using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Roblox.Dto.Games;
using Roblox.Dto.Persistence;
using Roblox.Dto.Users;
using System.Text.RegularExpressions;
using Roblox.Dto.Admin;
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.Assets;
using Roblox.Libraries.FastFlag;
using Roblox.Libraries.RobloxApi;
using Roblox.Logging;
using Roblox.Services.Exceptions;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using Roblox.Models.Assets;
using Roblox.Models.GameServer;
using Roblox.Models.Users;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.Controllers.Internal;
using Roblox.Website.Filters;
using Roblox.Website.Middleware;
using Roblox.Website.WebsiteModels.Asset;
using Roblox.Website.WebsiteModels.Games;
using HttpGet = Roblox.Website.Controllers.HttpGetBypassAttribute;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using ServiceProvider = Roblox.Services.ServiceProvider;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class BypassController : ControllerBase
    {
        public static string[] ClientFilteredWords =
        {
            "fag", "faggot", "dick", "cocK", "coc", "lezis", "dox", "kys", "hitler", "dox", 
            "jew", "nigger", "nigga", "kike", "porn", "nlgga", "n1gga", "n1gg3r", "n4gg3r", "igna", "К", "njgga", "njgger", "к"
        };

        public static string FilterString(string Text)
        {
            string pattern = string.Join("|", ClientFilteredWords.Select(word => Regex.Escape(word)));
            Regex censoredRegex = new Regex(pattern, RegexOptions.IgnoreCase);
            
            // now tag the word
            string filteredMessage = censoredRegex.Replace(Text, match =>
            {
                string hashtag = new string('#', match.Length);
                return hashtag;
            });

            return filteredMessage;
        }

        [HttpGet("internal/release-metadata")]
        public dynamic GetReleaseMetaData([Required] string requester)
        {
            throw new RobloxException(RobloxException.BadRequest, 0, "BadRequest");
        }

        [HttpGet("asset/shader")]
        public async Task<MVC.FileResult> GetShaderAsset(long id)
        {
            var isMaterialOrShader = BypassControllerMetadata.materialAndShaderAssetIds.Contains(id);
            if (!isMaterialOrShader)
            {
                // Would redirect but that could lead to infinite loop.
                // Just throw instead
                throw new RobloxException(400, 0, "BadRequest");
            }

            var assetId = id;
            try
            {
                var ourId = await services.assets.GetAssetIdFromRobloxAssetId(assetId);
                assetId = ourId;
            }
            catch (RecordNotFoundException)
            {
                // Doesn't exist yet, so create it
                var migrationResult = await MigrateItem.MigrateItemFromRoblox(assetId.ToString(), false, null, default, new ProductDataResponse()
                {
                    Name = "ShaderConversion" + id,
                    AssetTypeId = Type.Special, // Image
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    Description = "ShaderConversion1.0",
                });
                assetId = migrationResult.assetId;
            }
            
            var latestVersion = await services.assets.GetLatestAssetVersion(assetId);
            if (latestVersion.contentUrl is null)
            {
                throw new RobloxException(403, 0, "Forbidden"); // ?
            }
            // These files are large, encourage clients to cache them
            HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(360),
            }.ToString();
            var assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
            return File(assetContent, "application/binary");
        }

        private bool IsRcc()
        {
            var rccAccessKey = Request.Headers.ContainsKey("accesskey") ? Request.Headers["accesskey"].ToString() : null;
            var isRcc = rccAccessKey == Configuration.RccAuthorization;
            return isRcc;
        }

        [HttpGet("asset")]
        [HttpGet("v1/asset")]
        public async Task<MVC.ActionResult> GetAssetById(long id = 0, long assetversionid = 0)
        {
            bool useAssetVersionId = assetversionid > 0;
            // TODO: This endpoint needs to be updated to return a URL to the asset, not the asset itself.
            // The reason for this is so that cloudflare can cache assets without caching the response of this endpoint, which might be different depending on the client making the request (e.g. under 18 user, over 18 user, rcc, etc).
            var is18OrOver = false;
            if (userSession != null)
            {
                is18OrOver = await services.users.Is18Plus(userSession.userId);
            }

            // TEMPORARY UNTIL AUTH WORKS ON STUDIO! REMEMBER TO REMOVE
            if (HttpContext.Request.Headers.ContainsKey("RbxTempBypassFor18PlusAssets"))
            {
                is18OrOver = true;
            }

            AssetsService assetsService = new AssetsService();
            long assetId = useAssetVersionId ? await assetsService.GetAssetIdByAssetVersionId(assetversionid) : id;
            var invalidIdKey = "InvalidAssetIdForConversionV1:" + assetId;
            // Opt
            if (Services.Cache.distributed.StringGetMemory(invalidIdKey) != null)
                throw new RobloxException(400, 0, "Asset is invalid or does not exist");
            
            var isBotRequest = Request.Headers["bot-auth"].ToString() == Roblox.Configuration.BotAuthorization;
            var isLoggedIn = userSession != null;
            var encryptionEnabled = !isBotRequest; // bots can't handle encryption :(
#if DEBUG == false
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault()?.ToLower();
            var requester = Request.Headers["Requester"].FirstOrDefault()?.ToLower();
            if (!isBotRequest && !isLoggedIn) {
                if (userAgent is null) throw new BadRequestException();
                if (requester is null) throw new BadRequestException();
                // Client = studio/client, Server = rcc
                if (requester != "client" && requester != "server")
                {
                    throw new BadRequestException();
                }

                if (!BypassControllerMetadata.allowedUserAgents.Contains(userAgent))
                {
                    throw new BadRequestException();
                }
            }
#endif

            var isMaterialOrShader = BypassControllerMetadata.materialAndShaderAssetIds.Contains(assetId);
            if (isMaterialOrShader)
            {
                return new MVC.RedirectResult("/asset/shader?id=" + assetId);
            }

            var isRcc = IsRcc();
            if (isRcc)
                encryptionEnabled = false;
#if DEBUG
            encryptionEnabled = false;
#endif
            MultiGetEntry details;
            try 
            {
                
                details = await services.assets.GetAssetCatalogInfo(assetId);
            } 
            catch (RecordNotFoundException) 
            {
                try
                {
                    var ourId = await services.assets.GetAssetIdFromRobloxAssetId(assetId);
                    assetId = ourId;
                }
                catch (RecordNotFoundException)
                {
                    if (await Services.Cache.distributed.StringGetAsync(invalidIdKey) != null)
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    
                    try
                    {
                        return Redirect($"https://assetdelivery.roblox.com/v1/asset/?id={assetId}");
                        // Doesn't exist yet, so create it
                        var migrationResult = await MigrateItem.MigrateItemFromRoblox(assetId.ToString(), false, null,
                            new List<Type>()
                            {
                                Type.Image,
                                Type.Audio,
                                Type.Mesh,
                                Type.Lua,
                                Type.Model,
                                Type.Decal,
                                Type.Animation,
                                Type.SolidModel,
                                Type.MeshPart,
                                Type.ClimbAnimation,
                                Type.DeathAnimation,
                                Type.FallAnimation,
                                Type.IdleAnimation,
                                Type.JumpAnimation,
                                Type.RunAnimation,
                                Type.SwimAnimation,
                                Type.WalkAnimation,
                                Type.PoseAnimation,
                            }, default, default, true);
                        assetId = migrationResult.assetId;
                    }
                    catch (AssetTypeNotAllowedException)
                    {
                        // TODO: permanently insert as invalid for AssetTypeNotAllowedException in a table
                        await Services.Cache.distributed.StringSetAsync(invalidIdKey,
                            "{}", TimeSpan.FromDays(7));
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    }
                    catch (Exception e)
                    {
                        // temporary failure? mark as invalid, but only temporarily
                        Writer.Info(LogGroup.AssetDelivery, "Failed to migrate asset " + assetId + " - " + e.Message + "\n" + e.StackTrace);
                        await Services.Cache.distributed.StringSetAsync(invalidIdKey,
                            "{}", TimeSpan.FromMinutes(1));
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    }
                }
                details = await services.assets.GetAssetCatalogInfo(assetId);
            }
            if (details.is18Plus && !isRcc && !isBotRequest && !is18OrOver)
                throw new RobloxException(400, 0, "AssetTemporarilyUnavailable");
            if (details.moderationStatus != ModerationStatus.ReviewApproved && !isRcc && !isBotRequest)
                throw new RobloxException(403, 0, "Asset not approved for requester");
            
            var latestVersion = useAssetVersionId ? await assetsService.GetAssetVersionId(assetversionid) : await services.assets.GetLatestAssetVersion(assetId);
            Stream? assetContent = null;
            switch (details.assetType)
            {
                // Special types
                case Roblox.Models.Assets.Type.TeeShirt:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetTeeShirt(latestVersion.contentId)), "application/binary");
                case Models.Assets.Type.Shirt:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetShirt(latestVersion.contentId)), "application/binary");
                case Models.Assets.Type.Pants:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetPants(latestVersion.contentId)), "application/binary");
                // Types that require no authentication and aren't encrypted
                case Models.Assets.Type.Image:
                case Models.Assets.Type.Special:
                    if (latestVersion.contentUrl != null)
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    // encryptionEnabled = false;
                    break;
                // Types that require no authentication
                case Models.Assets.Type.Audio:
                case Models.Assets.Type.Mesh:
                case Models.Assets.Type.Hat:
                case Models.Assets.Type.Model:
                case Models.Assets.Type.Decal:
                case Models.Assets.Type.Head:
                case Models.Assets.Type.Face:
                case Models.Assets.Type.Gear:
                case Models.Assets.Type.Badge:
                case Models.Assets.Type.Animation:
                case Models.Assets.Type.Torso:
                case Models.Assets.Type.RightArm:
                case Models.Assets.Type.LeftArm:
                case Models.Assets.Type.RightLeg:
                case Models.Assets.Type.LeftLeg:
                case Models.Assets.Type.Package:
                case Models.Assets.Type.GamePass:
                case Models.Assets.Type.Plugin: // TODO: do plugins need auth?
                case Models.Assets.Type.MeshPart:
                case Models.Assets.Type.HairAccessory:
                case Models.Assets.Type.FaceAccessory:
                case Models.Assets.Type.NeckAccessory:
                case Models.Assets.Type.ShoulderAccessory:
                case Models.Assets.Type.FrontAccessory:
                case Models.Assets.Type.BackAccessory:
                case Models.Assets.Type.WaistAccessory:
                case Models.Assets.Type.ClimbAnimation:
                case Models.Assets.Type.DeathAnimation:
                case Models.Assets.Type.FallAnimation:
                case Models.Assets.Type.IdleAnimation:
                case Models.Assets.Type.JumpAnimation:
                case Models.Assets.Type.RunAnimation:
                case Models.Assets.Type.SwimAnimation:
                case Models.Assets.Type.WalkAnimation:
                case Models.Assets.Type.PoseAnimation:
                case Models.Assets.Type.SolidModel:
                    if (latestVersion.contentUrl is null)
                        throw new RobloxException(400, 0, "BadRequest"); // todo: should we log this?
                    if (details.assetType == Models.Assets.Type.Audio)
                    {
                        // Convert to WAV file since that's what web client requires
                        assetContent = await services.assets.GetAudioContentAsWav(assetId, latestVersion.contentUrl);
                    }
                    else
                    {
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    }
                    break;
                default:
                    // anything else requires auth
                    var ok = false;
                    if (isRcc)
                    {
                        encryptionEnabled = false;
                        var placeIdHeader = Request.Headers["roblox-place-id"].ToString();
                        long placeId = 0;
                        if (!string.IsNullOrEmpty(placeIdHeader))
                        {
                            try
                            {
                                placeId = long.Parse(Request.Headers["roblox-place-id"].ToString());
                            }
                            catch (FormatException)
                            {
                                // Ignore
                            }
                        }
                        // if rcc is trying to access current place, allow through
                        ok = (placeId == assetId);
                        // If game server is trying to load a new place (current placeId is empty), then allow it
                        if (!ok && details.assetType == Models.Assets.Type.Place && placeId == 0)
                        {
                            // Game server is trying to load, so allow it
                            ok = true;
                        }
                        // If rcc is making the request, but it's not for a place, validate the request:
                        if (!ok)
                        {
                            // Check permissions
                            var placeDetails = await services.assets.GetAssetCatalogInfo(placeId);
                            if (placeDetails.creatorType == details.creatorType &&
                                placeDetails.creatorTargetId == details.creatorTargetId)
                            {
                                // We are authorized
                                ok = true;
                            }
                        }
                    }
                    else
                    {
                        // It's not RCC making the request. are we authorized?
                        ok = (details.creatorType == CreatorType.User && details.creatorTargetId == 1); // Allow access to ROBLOX's assets
                        if (userSession != null)
                        {
                            // Use current user as access check
                            ok = await services.assets.CanUserModifyItem(assetId, userSession.userId);
                            if (!ok)
                            {
                                // Note that all users have access to "Roblox"'s content for legacy reasons
                                ok = (details.creatorType == CreatorType.User && details.creatorTargetId == 1);
                            }
#if DEBUG
                            // If staff, allow access in debug builds
                            if (await services.users.IsUserStaff(userSession.userId))
                            {
                                ok = true;
                            }
#endif
                            // Don't encrypt assets being sent to authorized users - they could be trying to download their own place to give to a friend or something
                            if (ok)
                            {
                                encryptionEnabled = false;
                            }
                        }
                    }

                    if (ok && latestVersion.contentUrl != null)
                    {
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    }

                    break;
            }

            if (assetContent != null)
            {
                return File(assetContent, "application/binary");
            }

            Console.WriteLine("[info] got BadRequest on /asset/ endpoint");
            throw new BadRequestException();
        }

        [HttpGet("Game/GamePass/GamePassHandler.ashx")]
        public async Task<string> GamePassHandler(string Action, long UserID, long PassID)
        {
            if (Action == "HasPass")
            {
                var has = await services.users.GetUserAssets(UserID, PassID);
                return has.Any() ? "True" : "False";
            }

            throw new NotImplementedException();
        }

        [HttpGet("avatar-thumbnail/json")]
        public async Task<dynamic> AvThumbnailJson([Required] long userId)
        {
            string? thumbnailUrl = null;
            var authUser18Plus = userSession != null && await services.users.Is18Plus(userSession.userId);
            if (!authUser18Plus)
            {
                var avatar18Plus = await services.avatar.IsUserAvatar18Plus(userId);
                if (avatar18Plus)
                    thumbnailUrl = $"{Configuration.BaseUrl}/img/blocked.png";
            }

            var result = (await services.thumbnails.GetUserThumbnails(new[] {userId})).ToList();
            if (result.Count == 0)
                thumbnailUrl = $"{Configuration.BaseUrl}/img/placeholder.png";
        
            if (thumbnailUrl == null)
            {
                thumbnailUrl = Configuration.BaseUrl + result[0].imageUrl;
            }
            return new {
                Url = thumbnailUrl,
                Final = true,
                SubstitutionType = 4
            };
        }

        [HttpGet("Game/LuaWebService/HandleSocialRequest.ashx")]
        public async Task<string> LuaSocialRequest([Required, MVC.FromQuery] string method, long? playerid = null, long? groupid = null, long? userid = null)
        {
            // TODO: Implement these
            method = method.ToLower();
            if (method == "isingroup" && playerid != null && groupid != null)
            {
                bool isInGroup = false;
                try
                {
                    if (groupid == 1200769 && await StaffFilter.IsStaff(playerid ?? 0))
                    {
                        isInGroup = true;
                    }
                    var group = await services.groups.GetUserRoleInGroup((long) groupid, (long) playerid);
                    if (group.rank != 0)
                        isInGroup = true;
                }
                catch (Exception)
                {
                    
                }

                return "<Value Type=\"boolean\">"+(isInGroup ? "true" : "false")+"</Value>";
            }

            if (method == "getgrouprank" && playerid != null && groupid != null)
            {
                int rank = 0;
                try
                {
                    var group = await services.groups.GetUserRoleInGroup((long) groupid, (long) playerid);
                    rank = group.rank;
                }
                catch (Exception)
                {
                    
                }

                return "<Value Type=\"integer\">"+rank+"</Value>";
            }

            if (method == "getgrouprole" && playerid != null && groupid != null)
            {
                var groups = await services.groups.GetAllRolesForUser((long) playerid);
                foreach (var group in groups)
                {
                    if (group.groupId == groupid)
                    {
                        return group.name;
                    }
                }

                return "Guest";
            }

            if (method == "isfriendswith" && playerid != null && userid != null)
            {
                var status = (await services.friends.MultiGetFriendshipStatus((long) playerid, new[] {(long) userid})).FirstOrDefault();
                if (status != null && status.status == "Friends")
                {
                    return "<Value Type=\"boolean\">True</Value>";
                }
                return "<Value Type=\"boolean\">False</Value>";

            }

            if (method == "isbestfriendswith")
            {
                return "<Value Type\"boolean\">False</value>";
            }

            throw new NotImplementedException();
        }

        /*[HttpGetBypass("/users/{useridlol:long}/canmanage/{placeIdlol:long}")]
        public dynamic CanManageApiEndPoint(long useridlol, long placeIdlol)
        {
            long[] accessAllGames = {1, 4, 12};
            // todo: owner ids later
            dynamic canManageResponse = new
            {
                Success = true,
                CanManage = accessAllGames.Contains(useridlol)
            };
            string jsonString = JsonConvert.SerializeObject(canManageResponse);
            return Content(jsonString, "application/json");
        }
        */

        /// <summary>
        /// Negotiate client authentication
        /// </summary>
        [HttpGetBypass("Login/Negotiate.ashx")]
        [HttpPostBypass("Login/Negotiate.ashx")]
        public void NegotiateAuthentication(string suggest)
        {
            // for now just do this shit because we don't have game auth tickets yet
            HttpContext.Response.Cookies.Append(".ROBLOSECURITY", suggest, new CookieOptions
            {
                Domain = Request.GetDisplayUrl().Contains("localhost") ? "localhost" : ".economy-simulator.org",
                Secure = false,
                Expires = null,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });
            HttpContext.Response.Cookies.Append(".ROBLOSECURITY", suggest, new CookieOptions
            {
                Domain = Request.GetDisplayUrl().Contains("localhost") ? "localhost" : "economy-simulator.org",
                Secure = false,
                Expires = null,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });
        }

        [HttpGet("/auth/submit")]
        public MVC.RedirectResult SubmitAuth(string auth)
        {
            return new MVC.RedirectResult("/");
        }

        [HttpGetBypass("/game/PlaceLauncher.ashx")]
        [HttpPostBypass("/game/PlaceLauncher.ashx")]
        public async Task<dynamic> PlaceLaunch(long placeId)
        {
            if (userSession == null)
            {
                return BadRequest();
            }
            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);
            GameServerJwt details = new GameServerJwt
            {
                userId = userSession.userId,
                placeId = placeId,
                t = "GameJoinTicketV1.1",
                iat = DateTimeOffset.Now.ToUnixTimeSeconds(),
                ip = GetIP()
            };
            var result = await services.gameServer.GetServerForPlace(details.placeId);
            if (result.status == JoinStatus.Joining)
            {
                await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(details.placeId);
                return new
                {
                    jobId = result.job,
                    status = (int)result.status,
                    joinScriptUrl = $"https://www.economy-simulator.org/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                    authenticationUrl = "https://www.economy-simulator.org" + "/Login/Negotiate.ashx",
                    authenticationTicket = Request.Cookies[".ROBLOSECURITY"],
                    message = (string?)null,
                };
            }

            return new
            {
                jobId = (string?)null,
                status = (int)result.status,
                message = "Waiting for server",
            };
        }

        public static long startUserId {get;set;} = 30;
#if DEBUG
        [HttpGetBypass("/game/get-join-script-debug")]
        public async Task<dynamic> GetJoinScriptDebug(long placeId, long userId = 12)
        {
            //startUserId = 12;
            var result = services.gameServer.CreateTicket(startUserId, placeId, GetIP());
            startUserId++;
            return new
            {
                placeLauncher = $"{Configuration.BaseUrl}/placelauncher.ashx?ticket={HttpUtility.UrlEncode(result)}",
                authenticationTicket = result,
            };
        }
#endif

        [HttpGetBypass("game/join.ashx")]
        public async Task<dynamic> JoinGame(string jobId, long placeId)
        {
            string username = safeUserSession.username;
            long userId = safeUserSession.userId;
            Console.WriteLine(username);
            long test2018Place = 1294;
            bool is2018 = placeId == test2018Place;
            GamesService gamesService = new GamesService();
            PlaceEntry uni = (await gamesService.MultiGetPlaceDetails(new[] { placeId })).First();
            string membership;
            var membership2 = await services.users.GetUserMembership(userId);
            if (membership2  == null)
            {
                membership = "None";
            }
            else
            {
                membership = (int)membership2!.membershipType == 3 ? "OutrageousBuildersClub" : (int)membership2.membershipType == 2 ? "TurboBuildersClub" : (int)membership2.membershipType == 1 ? "BuildersClub" : "None";

            }
            var userInfo = await services.users.GetUserById(userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;
            string characterAppearanceUrl;
            if (!is2018)
            {
                characterAppearanceUrl = $"{Configuration.BaseUrl}/Asset/CharacterFetch.ashx?placeId={placeId}&userId={userId}";
            }
            else
            {
                characterAppearanceUrl = $"{Configuration.BaseUrl}/v1.1/avatar-fetch/?placeId={placeId}&userId={userId}";
            }
            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            string clientTicket;
            if (is2018)
            {
                clientTicket = TicketGeneration.GenerateClientTicketV2(userId, username, "Test", formattedDateTime);
            }
            else
            {
                clientTicket = TicketGeneration.GenerateClientTicketV1(userId, username, jobId, formattedDateTime, characterAppearanceUrl);
            }
            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);

            string ip = "vps.economy-simulator.org";
            int port = placeId == test2018Place ? 53640 : GameServerService.currentGameServerPorts[jobId];
            dynamic joinScript = new
            {
                ClientPort = 0,
                MachineAddress = ip,
                ServerPort = port,
                PingUrl = "",
                PingInterval = 120,
                UserName = username,
                SeleniumTestMode = false,
                UserId = userId,
                SuperSafeChat = false,
                CharacterAppearance =
                    characterAppearanceUrl,
                ClientTicket = clientTicket,
                NewClientTicket = clientTicket,
                GameId = jobId,
                PlaceId = placeId,
                MeasurementUrl = "",
                WaitingForCharacterGuid = Guid.NewGuid().ToString(),
                BaseUrl = Configuration.BaseUrl,
                ChatStyle = "ClassicAndBubble",
                VendorId = 0,
                ScreenShotInfo = "",
                VideoInfo = "",
                CreatorId = uni.builderId,
                CreatorTypeEnum = "User",
                MembershipType = membership,
                AccountAge = accountAgeDays,
                CookieStoreFirstTimePlayKey = "rbx_evt_ftp",
                CookieStoreFiveMinutePlayKey = "rbx_evt_fmp",
                CookieStoreEnabled = true,
                IsRobloxPlace = uni.builderId == 1,
                GenerateTeleportJoin = false,
                IsUnknownOrUnder13 = false,
                SessionId = "",
                DataCenterId = 0,
                UniverseId = 0,
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                FollowUserId = 0,
                characterAppearanceId = userId,
                CountryCode = "US"
            };
            string signature = is2018 ? SignatureController.SignJsonResponseForClientFromPrivateKey2048(joinScript) : SignatureController.SignJsonResponseForClientFromPrivateKey(joinScript);
            Console.WriteLine(signature);
            return signature;
        }

        [HttpGetBypass("Asset/CharacterFetch.ashx")]
        public async Task<string> CharacterFetch(long userId, long placeId = 0)
        {
            bool enableGears = StaffFilter.IsOwner(userId);
            AssetsService assetsService = new AssetsService();
            var assets = await services.avatar.GetWornAssets(userId);
            var filteredAssets = (await Task.WhenAll(assets.Select(async asset => new { Asset = asset, Type = await assetsService.GetAssetType(asset) })))
                .Where(x => x.Type != 19 || enableGears)
                .Select(x => x.Asset)
                .ToList();
            return $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?requestId={Guid.NewGuid().ToString()}&userId={userId};{string.Join(";", filteredAssets.Select(c => Configuration.BaseUrl + "/Asset/?id=" + c))}";
        }

        [HttpGetBypass("/v1.1/avatar-fetch")]
        public async Task<dynamic> AvatarFetchV1([Required] long userId, long placeId = 0)
        {
            bool enableGears = placeId == 0 || StaffFilter.IsOwner(userId);;
            var colors = await services.avatar.GetAvatar(userId);
            var scales = await services.avatar.GetAvatarScales(userId);
            IEnumerable<long> assets = await services.avatar.GetWornAssets(userId);
            AssetsService assetsService = new AssetsService();
            List<long> accessoryVersionIds = new List<long>();
            List<long> equippedGearVersionIds = new List<long>();
            List<long> backpackGearVersionIds = new List<long>();
            foreach (var asset in assets)
            {
                bool isGear = await assetsService.GetAssetType(asset) == 19; // 19 = gear
                if (!isGear || enableGears && placeId == 0)
                {
                    var latestVersion = await assetsService.GetLatestAssetVersion(asset);
                    accessoryVersionIds.Add(latestVersion.assetVersionId);
                }
                // if is gear add this here
                if (enableGears && isGear)
                {
                    var latestVersion = await assetsService.GetLatestAssetVersion(asset);
                    equippedGearVersionIds.Add(latestVersion.assetVersionId);
                    backpackGearVersionIds.Add(latestVersion.assetVersionId);
                }
            }
            dynamic response = new
            {
                resolvedAvatarType = await services.avatar.GetAvatarType(userId),
                accessoryVersionIds = accessoryVersionIds,
                equippedGearVersionIds = equippedGearVersionIds,
                backpackGearVersionIds = backpackGearVersionIds,
                bodyColors = new
                {
                    HeadColor = colors.headColorId,
                    LeftArmColor = colors.leftArmColorId,
                    LeftLegColor = colors.leftLegColorId,
                    RightArmColor = colors.rightArmColorId,
                    RightLegColor = colors.rightLegColorId,
                    TorsoColor = colors.torsoColorId
                },
                animations = new
                {
                    
                },
                scales = new
                {
                    Width = scales.scale_width,
                    Height = scales.scale_height,
                    Head = scales.scale_head,
                    Depth = scales.scale_depth,
                    Proportion = scales.scale_proportion,
                    BodyType = scales.scale_body_type
                }
            };
            return response;
        }
        
        /// <summary>
        /// gameserver script
        /// </summary>
        [HttpGet("Game/GameServer.ashx")]
        public string GameServerLua()
        {
            if (!IsRcc())
            {
                throw new RobloxException(401, 0, "Not allowed");
            }
            string script = System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Scripts", "GameServer.lua"));
            return SignatureController.SignStringResponseForClientFromPrivateKey(script, true);
        }
        
        [HttpGet("GetCurrentClientVersionUpload")]
        public string GetCurrentClientVersionUpload(string binaryType)
        {
            string version = @"""version-2018testversion""";
            return version;
        }

        [HttpGet("Game/Visit.ashx")]
        public string VisitLua()
        {
            string script = System.IO.File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Scripts", "Visit.lua"));
            return SignatureController.SignStringResponseForClientFromPrivateKey(script, true);
        }

        private void CheckServerAuth(string auth)
        {
            if (auth != Configuration.GameServerAuthorization)
            {
                Roblox.Metrics.GameMetrics.ReportRccAuthorizationFailure(HttpContext.Request.GetEncodedUrl(),
                    auth, GetRequesterIpRaw(HttpContext));
                throw new BadRequestException();
            }
        }

        [MVC.HttpPost("/gs/activity")]
        public async Task<dynamic> GetGsActivity([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            var result = await services.gameServer.GetLastServerPing(request.serverId);
            return new
            {
                isAlive = result >= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                updatedAt = result,
            };
        }

        [MVC.HttpPost("/gs/ping")]
        public async Task ReportServerActivity([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            await services.gameServer.SetServerPing(request.serverId);
        }

        [MVC.HttpPost("/gs/delete")]
        public async Task DeleteServer([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            await services.gameServer.DeleteGameServer(request.serverId);
        }

        [MVC.HttpPost("/gs/shutdown")]
        public async Task ShutDownServer([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            services.gameServer.ShutDownServer(request.serverId);
        }

        [MVC.HttpPost("/gs/players/report")]
        public async Task ReportPlayerActivity([Required, MVC.FromBody] ReportPlayerActivity request)
        {
            CheckServerAuth(request.authorization);
            if (request.eventType == "Leave")
            {
                await services.gameServer.OnPlayerLeave(request.userId, request.placeId, request.serverId);
            }
            else if (request.eventType == "Join")
            {
                await Roblox.Metrics.GameMetrics.ReportGameJoinSuccess(request.placeId);
                await services.gameServer.OnPlayerJoin(request.userId, request.placeId, request.serverId);
            }
            else
            {
                throw new Exception("Unexpected type " + request.eventType);
            }
        }
        
        /// <summary>
        /// used to report hackers
        /// </summary>
        [HttpGet("/gs/players/report-sys-stats")]
        public async Task<MVC.IActionResult> ReportSysStats([Required] string apiKey, [Required] long UserID, string Resolution, string Message)
        {
            long onRemoteSysStatsAccountId = 805;
            AdminApiController adminApiController = new AdminApiController();
            string webhookUrl =
                "https://discord.com/api/webhooks/1216909880837804173/i_1lJzhIb4x8Q_YOaO9IhHhjWME-KfGsmsJSRoargH3TqQUu5uaQ0WK3GsBUP_-H9K0w";
            if (apiKey != Configuration.RccAuthorization)
                return StatusCode(403, new { Message = "You are not authorized to use this endpoint." });
            // check if user should be banned
            bool wasBanned = false;
            if (Message == "suzanne")
            {
                await adminApiController.BanUser(new BanUserRequest
                {
                    userId = UserID,
                    reason = "x32dbg is fire huh?",
                    internalReason = "Tried attaching x32dbg (or another debugger) to the client (This is an anticheat ban)",
                    expires = null
                }, null, onRemoteSysStatsAccountId);
                wasBanned = true;
            }
            dynamic discordMessage = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "Hacker Report",
                        description = "Reported by onRemoteSysStats",
                        color = 2067276,
                        fields = new[]
                        {
                            new { name = "User ID", value = UserID.ToString(), inline = true },
                            new { name = "Resolution", value = Resolution, inline = true },
                            new { name = "Message", value = Message, inline = true },
                            new { name = "User Banned?", value = wasBanned.ToString(), inline = true }
                        }
                    }
                }
            };
            StringContent reportContent = new StringContent(JsonConvert.SerializeObject(discordMessage), Encoding.UTF8, "application/json");
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.PostAsync(webhookUrl, reportContent);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ReportSysStats] Successfully reported cheater to discord! Information: UserId: {UserID}, Resolution: {Resolution}, Message: {Message}");
                }
                else
                {
                    Console.WriteLine($"[ReportSysStats] Failed to report cheater to discord. Response code: {response.StatusCode}, response message: {response.Content.ReadAsStringAsync()}");
                }
            }
            return Ok();
        }

        [MVC.HttpPost("/gs/a")]
        public void ReportGS()
        {
            // Doesn't do anything yet. See: services/api/src/controllers/bypass.ts:1473
            return;
        }

        [MVC.HttpPost("/Game/ValidateTicket.ashx")]
        public async Task<string> ValidateClientTicketRcc([Required, MVC.FromBody] ValidateTicketRequest request)
        {
#if DEBUG
            return "true";
#endif
            
            try
            {
                // Below is intentionally caught by local try/catch. RCC could crash if we give a 500 error.
                FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);
                var ticketData = services.gameServer.DecodeTicket(request.ticket, null);
                if (ticketData.userId != request.expectedUserId)
                {
                    // Either bug or someone broke into RCC
                    Roblox.Metrics.GameMetrics.ReportTicketErrorUserIdNotMatchingTicket(request.ticket,
                        ticketData.userId, request.expectedUserId);
                    throw new Exception("Ticket userId does not match expected userId");
                }
                // From TS: it is possible for a client to spoof username or appearance to be empty string, 
                // so make sure you don't do much validation on those params (aside from assertion that it's a string)
                if (request.expectedUsername != null)
                {
                    var userInfo = await services.users.GetUserById(ticketData.userId);
                    if (userInfo.username != request.expectedUsername)
                    {
                        throw new Exception("Ticket username does not match expected username");
                    }
                }

                if (request.expectedAppearanceUrl != null)
                {
                    // will always be format of "http://localhost/Asset/CharacterFetch.ashx?userId=12", NO EXCEPTIONS!
                    var expectedUrl =
                        $"{Roblox.Configuration.BaseUrl}/Asset/CharacterFetch.ashx?userId={ticketData.userId}";
                    if (request.expectedAppearanceUrl != expectedUrl)
                    {
                        throw new Exception("Character URL is bad");
                    }
                }
                
                // Confirm user isn't already in a game
                var gameStatus = (await services.users.MultiGetPresence(new [] {ticketData.userId})).First();
                if (gameStatus.placeId != null && gameStatus.userPresenceType == PresenceType.InGame)
                {
                    // Make sure that the only game they are playing is the one they are trying to join.
                    var playingGames = await services.gameServer.GetGamesUserIsPlaying(ticketData.userId);
                    foreach (var game in playingGames)
                    {
                        if (game.id != request.gameJobId)
                            throw new Exception("User is already playing another game");
                    }
                }

                return "true";
            }
            catch (Exception e)
            {
                Console.WriteLine("[error] Verify ticket failed. Error = {0}\n{1}", e.Message, e.StackTrace);
                return "false";
            }
        }

        [MVC.HttpPost("/game/validate-machine")]
        public dynamic ValidateMachine()
        {
            return new
            {
                success = true,
                message = "",
            };
        }

        [HttpGetBypass("Users/ListStaff.ashx")]
        public async Task<IEnumerable<long>> GetStaffList()
        {
            return (await StaffFilter.GetStaff()).Where(c => c != 36 /* this was id 12 lol in og source */);
        }

        [HttpGetBypass("Users/GetBanStatus.ashx")]
        public async Task<IEnumerable<dynamic>> MultiGetBanStatus(string userIds)
        {

            var ids = userIds.Split(",").Select(long.Parse).Distinct();
            var result = new List<dynamic>();
#if DEBUG
            return ids.Select(c => new
            {
                userId = c,
                isBanned = false,
            });
#else
            var multiGetResult = await services.users.MultiGetAccountStatus(ids);
            foreach (var user in multiGetResult)
            {
                result.Add(new
                {
                    userId = user.userId,
                    isBanned = user.accountStatus != AccountStatus.Ok,
                });
            }

            return result;
#endif
        }

        [HttpGetBypass("Asset/BodyColors.ashx")]
        public async Task<string> GetBodyColors(long userId)
        {
            var colors = await services.avatar.GetAvatar(userId);

            var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

            var robloxRoot = new XElement("roblox",
                new XAttribute(XNamespace.Xmlns + "xmime", "http://www.w3.org/2005/05/xmlmime"),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(xsi + "noNamespaceSchemaLocation", "http://www.roblox.com/roblox.xsd"),
                new XAttribute("version", 4)
            );
            robloxRoot.Add(new XElement("External", "null"));
            robloxRoot.Add(new XElement("External", "nil"));
            var items = new XElement("Item", new XAttribute("class", "BodyColors"));
            var properties = new XElement("Properties");
            // set colors
            properties.Add(new XElement("int", new XAttribute("name", "HeadColor"), colors.headColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "LeftArmColor"), colors.leftArmColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "LeftLegColor"), colors.leftLegColorId.ToString()));
            properties.Add(new XElement("string", new XAttribute("name", "Name"), "Body Colors"));
            properties.Add(new XElement("int", new XAttribute("name", "RightArmColor"), colors.rightArmColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "RightLegColor"), colors.rightLegColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "TorsoColor"), colors.torsoColorId.ToString()));
            properties.Add(new XElement("bool", new XAttribute("name", "archivable"), "true"));
            // add
            items.Add(properties);
            robloxRoot.Add(items);
            // return as string
            return new XDocument(robloxRoot).ToString();
        }

        [MVC.HttpPost("/apisite/api/moderation/filtertext/")]
        [MVC.HttpPost("/api/moderation/filtertext/")]
        [MVC.HttpPost("/moderation/filtertext/")]
        public async Task<dynamic> GetModerationText([MVC.FromForm] string text, [MVC.FromForm] long userId)
        {
            if (!IsRcc())
            {
                return Unauthorized();
            }
            string webhook = "https://discord.com/api/webhooks/1216576629451784314/h06PaGniTl2eRDpF_1isF2XNTQc5ixZV6TUVjjj_iMcFAJTpgWpwnzbAGN-E8UOHb5Tc";
            string filteredText = FilterString(text);
            dynamic discordMessage = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "Chat Message",
                        description = "Chat Message sent in 2016E",
                        color = 65280,
                        fields = new[]
                        {
                            new { name = "User ID", value = userId.ToString(), inline = true },
                            new { name = "Message", value = text, inline = true },
                            new { name = "Filtered Message", value = filteredText, inline = true }
                        }
                    }
                }
            };
            StringContent reportContent = new StringContent(JsonConvert.SerializeObject(discordMessage), Encoding.UTF8, "application/json");
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.PostAsync(webhook, reportContent);
            }
            return new
            {
                data = new
                {
                    white = filteredText,
                    black = filteredText,
                },
            };
        }
        
        [MVC.HttpPost("/moderation/v2/filtertext")]
        public async Task<dynamic> GetModerationTextV2([MVC.FromForm] string text, [MVC.FromForm] long userId)
        {
            if (!IsRcc())
            {
                return Unauthorized();
            }
            string webhook = "https://discord.com/api/webhooks/1216576629451784314/h06PaGniTl2eRDpF_1isF2XNTQc5ixZV6TUVjjj_iMcFAJTpgWpwnzbAGN-E8UOHb5Tc";
            string filteredText = FilterString(text);
            dynamic discordMessage = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "Chat Message",
                        description = "Chat Message sent in 2018L",
                        color = 65280,
                        fields = new[]
                        {
                            new { name = "User ID", value = userId.ToString(), inline = true },
                            new { name = "Message", value = text, inline = true },
                            new { name = "Filtered Message", value = filteredText, inline = true }
                        }
                    }
                }
            };
            StringContent reportContent = new StringContent(JsonConvert.SerializeObject(discordMessage), Encoding.UTF8, "application/json");
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.PostAsync(webhook, reportContent);
            }
            return new
            {
                success = true,
                data = new
                {
                    AgeUnder13 = filteredText,
                    Age13OrOver = filteredText,
                },
            };
        }

        private void ValidateBotAuthorization()
        {
#if DEBUG == false
	        if (Request.Headers["bot-auth"].ToString() != Roblox.Configuration.BotAuthorization)
	        {
		        throw new Exception("Internal");
	        }
#endif
        }

        [HttpGetBypass("botapi/migrate-alltypes")]
        public async Task<dynamic> MigrateAllItemsBot([Required, MVC.FromQuery] string url)
        {
            ValidateBotAuthorization();
            return await MigrateItem.MigrateItemFromRoblox(url, false, null, new List<Type>()
            {
                Type.Image,
                Type.Audio,
                Type.Mesh,
                Type.Lua,
                Type.Model,
                Type.Decal,
                Type.Animation,
                Type.SolidModel,
                Type.MeshPart,
                Type.ClimbAnimation,
                Type.DeathAnimation,
                Type.FallAnimation,
                Type.IdleAnimation,
                Type.JumpAnimation,
                Type.RunAnimation,
                Type.SwimAnimation,
                Type.WalkAnimation,
                Type.PoseAnimation,
            }, default, false);
        }

        [HttpGetBypass("botapi/migrate-clothing")]
        public async Task<dynamic> MigrateClothingBot([Required] string assetId)
        {
            ValidateBotAuthorization();
            return await MigrateItem.MigrateItemFromRoblox(assetId, true, 5, new List<Models.Assets.Type>() { Models.Assets.Type.TeeShirt, Models.Assets.Type.Shirt, Models.Assets.Type.Pants });
        }

        [HttpGet("/apisite/versioncompatibility/GetAllowedMD5Hashes")]
        [HttpGet("/GetAllowedMD5Hashes")]
        public dynamic Md5Hashes()
        {
            List<string> allowedList = new List<string>()
            {
                "7c2352479ce950ef5c2aec63df6e1231", // 2016E
                "9387485f60eebc63f58f14d9cb8e58f5" // 2018L
            };
            return new { data = allowedList };
        }
        
        [HttpGet("/apisite/versioncompatibility/GetAllowedSecurityVersions")]
        [HttpGet("/GetAllowedSecurityVersions")]
        [HttpGet("/apisite/versioncompatibility/GetAllowedSecurityKeys")]
        [HttpGet("/GetAllowedSecurityKeys")]
        public dynamic SecurityVersions()
        {
            List<string> allowedList = new List<string>()
            {
                "0.1.0espcplayer",
                "0.360.0pcplayer"
            };
            return new { data = allowedList };
        }

        [HttpGetBypass("/apisite/clientsettings/Setting/QuietGet/{type}")]
        [HttpGetBypass("/Setting/QuietGet/{type}")]
        public MVC.ActionResult<dynamic> GetAppSettings(string type)
        {
            try
            {
                string jsonFilePath = Path.Combine(Environment.CurrentDirectory, "Flags", type + ".json");
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                dynamic? clientAppSettingsData = JsonConvert.DeserializeObject<ExpandoObject>(jsonContent);

                return clientAppSettingsData ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RetrieveClientFFlags] Error while retrieving FFlags: {ex.Message}");
                return new { };
            }
        }
        
        [HttpGetBypass("/Set2018/QuietGet/{type}")]
        public MVC.ActionResult<dynamic> GetAppSettings2018(string type)
        {
            try
            {
                string jsonFilePath = Path.Combine(Environment.CurrentDirectory, "Flags/2018L", type + ".json");
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                dynamic? clientAppSettingsData = JsonConvert.DeserializeObject<ExpandoObject>(jsonContent);

                return clientAppSettingsData ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RetrieveClientFFlags] Error while retrieving FFlags: {ex.Message}");
                return new { };
            }
        }

        [HttpGetBypass("BuildersClub/Upgrade.ashx")]
        public MVC.IActionResult UpgradeNow()
        {
            return new MVC.RedirectResult("/internal/membership");
        }

        [HttpGetBypass("abusereport/UserProfile"), HttpGetBypass("abusereport/asset"), HttpGetBypass("abusereport/user"), HttpGetBypass("abusereport/users")]
        public MVC.IActionResult ReportAbuseRedirect()
        {
            return new MVC.RedirectResult("/internal/report-abuse");
        }

        [HttpGetBypass("/info/blog")]
        public MVC.IActionResult RedirectToUpdates()
        {
            return new MVC.RedirectResult("/internal/updates");
        }

        [HttpGetBypass("/my/economy-status")]
        public dynamic GetEconomyStatus()
        {
            return new
            {
                isMarketplaceEnabled = true,
                isMarketplaceEnabledForAuthenticatedUser = true,
                isMarketplaceEnabledForUser = true,
                isMarketplaceEnabledForGroup = true,
            };
        }
        
        [HttpGet("users/get-by-username")]
        public async Task<dynamic> GetUserByUsername(string username)
        {
            var result = (await services.users.MultiGetUsersByUsername(new[] { username })).ToList();
            if (result.Count == 0) return new { success = false, errorMessage = "User not found" };
            var user = result[0];
            return new
            {
                Id = user.id,
                Username = user.name,
                AvatarUri = (string?)null,
                AvatarFinal = false,
                IsOnline = false,
            };
        }

        [HttpGet("v1/countries/phone-prefix-list")]
        public dynamic GetCountries()
        {
            return new List<dynamic>()
            {
                new
                {
                    name = "United States",
                    code = "US",
                    prefix = "1",
                    localizedName = "United States",
                },
                // from services/api/src/controllers/proxy/v1/Api.ts:38
                new
                {
                    name = "Your Mom",
                    code = "YM",
                    prefix = "69",
                    localizedName = "Your Mom",
                }
            };
        }

        [HttpGet("marketplace/productinfo")]
        public async Task<dynamic> GetProductInfo(long assetId)
        {
            var details = await services.assets.GetAssetCatalogInfo(assetId);
            return new
            {
                TargetId = details.id,
                AssetId = details.id,
                ProductId = details.id,
                Name = details.name,
                Description = details.description,
                AssetTypeId = (int)details.assetType,
                IsForSale = details.isForSale,
                IsPublicDomain = details.isForSale && details.price == 0,
                Creator = new
                {
                    Id = details.creatorTargetId,
                    Name = services.users.GetUserById(details.creatorTargetId).Result.username,
                },
            };
        }

        /// <summary>
        /// Chat Filter Info
        /// </summary>
        [HttpGetBypass("/apisite/api/game/players/{id:long}")]
        [HttpGetBypass("/game/players/{id:long}")]
        public MVC.ActionResult<dynamic> RetrieveChatFilterStatusForPlayer(long id)
        {
            var ChatFilterData = new
            {
                ChatFilter = "blacklist"
            };
            string jsonString = JsonConvert.SerializeObject(ChatFilterData);
            return Content(jsonString, "application/json");
        }

        [HttpGetBypass("/currency/balance")]
        public async Task<dynamic> GetBalance()
        {
            return await services.economy.GetBalance(CreatorType.User, safeUserSession.userId);
        }

        [HttpGetBypass("/ownership/hasasset")]
        public async Task<string> DoesOwnAsset(long userId, long assetId)
        {
            return (await services.users.GetUserAssets(userId, assetId)).Any() ? "true" : "false";
        }

        [HttpPostBypass("persistence/increment")]
        public async Task<dynamic> IncrementPersistence(long placeId, string key, string type, string scope, string target, int value)
        {
            // increment?placeId=%i&key=%s&type=%s&scope=%s&target=&value=%i
            
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            
            return new
            {
                data = (object?) null,
            };
        }

        [HttpPostBypass("persistence/getSortedValues")]
        public async Task<dynamic> GetSortedPersistenceValues(long placeId, string type, string scope, string key, int pageSize, bool ascending, int inclusiveMinValue = 0, int inclusiveMaxValue = 0)
        {
            // persistence/getSortedValues?placeId=0&type=sorted&scope=global&key=Level%5FHighscores20&pageSize=10&ascending=False"
            // persistence/set?placeId=124921244&key=BF2%5Fds%5Ftest&&type=standard&scope=global&target=BF2%5Fds%5Fkey%5Ftmp&valueLength=31
            
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            
            return new
            {
                data = new
                {
                    Entries = ArraySegment<int>.Empty,
                    ExclusiveStartKey = (string?)null,
                },
            };
        }

        [HttpPostBypass("persistence/getv2")]
        public async Task<dynamic> GetPersistenceV2(long placeId, string type, string scope)
        {
            var rawBody = await new StreamReader(Request.Body).ReadToEndAsync();
            if (rawBody.StartsWith("&"))
            {
                rawBody = rawBody.Substring(1);
            }
            // getV2?placeId=%i&type=%s&scope=%s
            // Expected format is:
            //	{ "data" : 
            //		[
            //			{	"Value" : value,
            //				"Scope" : scope,							
            //				"Key" : key,
            //				"Target" : target
            //			}
            //		]
            //	}
            // or for non-existing key:
            // { "data": [] }
            
            // for no sub key:
            // Expected format is:
            //	{ "data" : value }
            Console.WriteLine("Request = {0}", rawBody);
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            var requests = rawBody.Split("\n").Where(c => !string.IsNullOrWhiteSpace(c)).Distinct();
            
            var result = new List<GetKeyEntry>();
            foreach (var request in requests)
            {
                var des = JsonSerializer.Deserialize<GetKeyScope>(request);
                
                var res = await ds.Get(placeId, type, des.scope ?? scope, des.key, des.target);
                if (!string.IsNullOrWhiteSpace(res))
                    result.Add(new GetKeyEntry()
                    {
                        Key = des.key,
                        Scope = des.scope ?? scope,
                        Target =des.target,
                        Value = res,
                    });
            }

            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            
            return new
            {
                data = result,
            };
        }

        [HttpPostBypass("persistence/set")]
        public async Task<dynamic> Set(long placeId, string key, string type, string scope, string target, int valueLength, [Required, MVC.FromBody] SetRequest request)
        {
            // { "data" : value }
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            await ServiceProvider.GetOrCreate<DataStoreService>()
                .Set(placeId, target, type, scope, key, valueLength, request.data);
            
            return new
            {
                data = request.data,
            };
        }

#if DEBUG
        [HttpGetBypass("integration-test/create-account-and-set-cookie")]
        public async Task<string> CreateAccountAndSetCookie()
        {
            var name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 14);
            var result = await services.users.CreateUser(name, "AmogusDrip69", Gender.Male);
            await services.users.InsertOrUpdateMembership(result.userId, MembershipType.BuildersClub);
            var id = await services.users.CreateApplication(new CreateUserApplicationRequest()
            {
                about = "Integration test",
                socialPresence = "",
                isVerified = true,
                verifiedUrl = "https://economy-simulator.org/",
                verificationPhrase = "Integration test",
                verifiedId = "1",
            });
            var joinId = await services.users.ProcessApplication(id, 1, UserApplicationStatus.Approved);
            await services.users.SetApplicationUserIdByJoinId(joinId, result.userId);
            
            var sess = await services.users.CreateSession(result.userId);
            var sessionCookie = Roblox.Website.Middleware.SessionMiddleware.CreateJwt(new Middleware.JwtEntry()
            {
                sessionId = sess,
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
            });
            Response.Cookies.Append(SessionMiddleware.CookieName, sessionCookie, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.Now.AddDays(1),
                Path = "/",
            });
            return "Created user " + name + "...\nOK";
        }
#endif
    }
}
