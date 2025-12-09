using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Web;
using Roblox.Logging;

namespace Roblox.Rendering
{
    public static class CommandHandler
    {
        private static string arbiterBaseUrl { get; set; } = "";
        private static HttpClient httpClient { get; set; } = new HttpClient();

        public static void Configure(string baseUrl)
        {
            arbiterBaseUrl = baseUrl;
        }
        private static async Task<RenderResponse<Stream>> SendCommand(string command, IEnumerable<dynamic> arguments, CancellationToken? cancellationToken)
        {
            switch (command)
            {
                case "GenerateThumbnail":
                    dynamic resultThumbnail = arguments.FirstOrDefault()!;
                    long userIdThumbnail = resultThumbnail.userId;
                    bool R15 = resultThumbnail.avatarType == "R15";
                    string characterAppearanceUrlThumbnail =
                        $"http://www.economy-simulator.org/v1.1/avatar-fetch/?userId={userIdThumbnail}";
                    string thumbnailRequest =
                        $"{arbiterBaseUrl}/request-thumbnail-avatar?characterAppearanceUrl={characterAppearanceUrlThumbnail}&R15={(R15.ToString().ToString())}";
                    HttpResponseMessage httpResponseMessageThumbnail = await httpClient.PostAsync(thumbnailRequest, null);
                    string responseThumbnail = await httpResponseMessageThumbnail.Content.ReadAsStringAsync();
                    byte[] pngBytesThumbnail = Convert.FromBase64String(responseThumbnail);
                    MemoryStream streamThumbnail = new MemoryStream(pngBytesThumbnail);
                    return new RenderResponse<Stream>()
                    {
                        data = streamThumbnail,
                        id = Guid.NewGuid().ToString(),
                        status = 200
                    };
                case "GenerateThumbnailHeadshot":
                    dynamic resultHeadshot = arguments.FirstOrDefault()!;
                    long userIdHeadshot = resultHeadshot.userId;
                    string characterAppearanceUrlHeadshot =
                        $"http://www.economy-simulator.org/v1.1/avatar-fetch/?userId={userIdHeadshot}";
                    string headshotRequest =
                        $"{arbiterBaseUrl}/request-thumbnail-closeup?characterAppearanceUrl={characterAppearanceUrlHeadshot}";
                    HttpResponseMessage httpResponseMessageHeadshot = await httpClient.PostAsync(headshotRequest, null);
                    string responseHeadshot = await httpResponseMessageHeadshot.Content.ReadAsStringAsync();
                    byte[] pngBytesHeadshot = Convert.FromBase64String(responseHeadshot);
                    MemoryStream streamHeadshot = new MemoryStream(pngBytesHeadshot);
                    return new RenderResponse<Stream>()
                    {
                        data = streamHeadshot,
                        id = Guid.NewGuid().ToString(),
                        status = 200
                    };
                case "GenerateThumbnailAsset":
                    dynamic resultAsset = arguments.FirstOrDefault()!;
                    if (resultAsset is long userIdAsset)
                    {
                        string assetRequest = $"{arbiterBaseUrl}/request-thumbnail-hat?assetId={userIdAsset}";
                        HttpResponseMessage httpResponseMessageAsset = await httpClient.PostAsync(assetRequest, null);
                        string responseAsset = await httpResponseMessageAsset.Content.ReadAsStringAsync();
                        byte[] pngBytesAsset = Convert.FromBase64String(responseAsset);
                        MemoryStream streamAsset = new MemoryStream(pngBytesAsset);
                        return new RenderResponse<Stream>()
                        {
                            data = streamAsset,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailClothing":
                    dynamic resultClothing = arguments.FirstOrDefault()!;
                    if (resultClothing is long clothingId)
                    {
                        List<dynamic> argumentList = arguments.ToList();
                        string type = argumentList.Count > 1 ? argumentList[1] : "shirt";
                        string assetRequest = $"{arbiterBaseUrl}/request-thumbnail-{type}?assetId={clothingId}";
                        HttpResponseMessage httpResponseMessageAsset = await httpClient.PostAsync(assetRequest, null);
                        string responseAsset = await httpResponseMessageAsset.Content.ReadAsStringAsync();
                        byte[] pngBytesAsset = Convert.FromBase64String(responseAsset);
                        MemoryStream streamAsset = new MemoryStream(pngBytesAsset);
                        return new RenderResponse<Stream>()
                        {
                            data = streamAsset,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailHead":
                    dynamic resultHead = arguments.FirstOrDefault()!;
                    if (resultHead is long HeadId)
                    {
                        string HeadRequest = $"{arbiterBaseUrl}/request-thumbnail-head?assetId={HeadId}";
                        HttpResponseMessage httpResponseMessageHead = await httpClient.PostAsync(HeadRequest, null);
                        string responseHead = await httpResponseMessageHead.Content.ReadAsStringAsync();
                        byte[] pngBytesHead = Convert.FromBase64String(responseHead);
                        MemoryStream streamHead = new MemoryStream(pngBytesHead);
                        return new RenderResponse<Stream>()
                        {
                            data = streamHead,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailBodyPart":
                    dynamic resultBodyPart = arguments.FirstOrDefault()!;
                    if (resultBodyPart is long BodyPartId)
                    {
                        List<dynamic> argumentList = arguments.ToList();
                        string type = argumentList.Count > 1 ? argumentList[1] : "Torso";
                        string BodyPartIdRequest = $"{arbiterBaseUrl}/request-thumbnail-bodypart?assetId={BodyPartId}&BodyPartType={type}";
                        HttpResponseMessage httpResponseMessageBodyPartId = await httpClient.PostAsync(BodyPartIdRequest, null);
                        string responseBodyPartId = await httpResponseMessageBodyPartId.Content.ReadAsStringAsync();
                        byte[] pngBytesBodyPartId = Convert.FromBase64String(responseBodyPartId);
                        MemoryStream streamBodyPartId = new MemoryStream(pngBytesBodyPartId);
                        return new RenderResponse<Stream>()
                        {
                            data = streamBodyPartId,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailPackage":
                    dynamic resultPackage = arguments.FirstOrDefault()!;
                    if (resultPackage is List<long> packageIds)
                    {
                        string query = "?";
                        foreach (long assetId in packageIds)
                        {
                            query = query + $"&assetIds={assetId}";
                        }
                        string packageRequest = $"{arbiterBaseUrl}/request-thumbnail-package{query}";
                        HttpResponseMessage httpResponseMessagePackage = await httpClient.PostAsync(packageRequest, null);
                        string responsePackage = await httpResponseMessagePackage.Content.ReadAsStringAsync();
                        byte[] pngBytesPackage = Convert.FromBase64String(responsePackage);
                        MemoryStream streamPackage = new MemoryStream(pngBytesPackage);
                        return new RenderResponse<Stream>()
                        {
                            data = streamPackage,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailTexture":
                    dynamic resultTexture = arguments.FirstOrDefault()!;
                    if (resultTexture is long textureId)
                    {
                        string assetRequest = $"{arbiterBaseUrl}/request-thumbnail-texture?assetId={textureId}&isFace=false";
                        HttpResponseMessage httpResponseMessageAsset = await httpClient.PostAsync(assetRequest, null);
                        string responseAsset = await httpResponseMessageAsset.Content.ReadAsStringAsync();
                        byte[] pngBytesAsset = Convert.FromBase64String(responseAsset);
                        MemoryStream streamAsset = new MemoryStream(pngBytesAsset);
                        return new RenderResponse<Stream>()
                        {
                            data = streamAsset,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailFace":
                    dynamic resultFace = arguments.FirstOrDefault()!;
                    if (resultFace is long faceId)
                    {
                        string assetRequest = $"{arbiterBaseUrl}/request-thumbnail-image?assetId={faceId}&isFace=true";
                        HttpResponseMessage httpResponseMessageAsset = await httpClient.PostAsync(assetRequest, null);
                        string responseAsset = await httpResponseMessageAsset.Content.ReadAsStringAsync();
                        byte[] pngBytesAsset = Convert.FromBase64String(responseAsset);
                        MemoryStream streamAsset = new MemoryStream(pngBytesAsset);
                        return new RenderResponse<Stream>()
                        {
                            data = streamAsset,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                case "GenerateThumbnailGame":
                    dynamic resultGame = arguments.FirstOrDefault()!;
                    if (resultGame is long gameId)
                    {
                        List<dynamic> argumentList = arguments.ToList();
                        int x = argumentList.Count > 1 ? argumentList[1] : 0;
                        int y = argumentList.Count > 2 ? argumentList[2] : 0;
                        string PlaceRequest = $"{arbiterBaseUrl}/request-thumbnail-place?assetId={gameId}&x={x}&y={y}";
                        HttpResponseMessage httpResponseMessagePlace = await httpClient.PostAsync(PlaceRequest, null);
                        string responsePlace = await httpResponseMessagePlace.Content.ReadAsStringAsync();
                        byte[] pngBytesPlace = Convert.FromBase64String(responsePlace);
                        MemoryStream streamPlace = new MemoryStream(pngBytesPlace);
                        return new RenderResponse<Stream>()
                        {
                            data = streamPlace,
                            id = Guid.NewGuid().ToString(),
                            status = 200
                        };
                    }
                    throw new Exception();
                default:
                    throw new Exception();
            }
        }
        
        
        private static async Task<Stream> SendCmdWithErrHandlingAsync(string cmd, IEnumerable<dynamic> arguments, CancellationToken? cancellationToken = null)
        {
            var result = await SendCommand(cmd, arguments, cancellationToken);
            if (result.status != 200) throw new Exception("Render failed with status = " + result.status);
            if (result.data == null) throw new Exception("Null stream returned from SendCommand");
            return result.data;
        }

        public static async Task<Stream> RequestPlayerThumbnail(AvatarData data, CancellationToken? cancellationToken = null)
        {
            if (data.playerAvatarType != "R6")
                throw new Exception("Invalid PlayerAvatarType");
            
            // todo: do we need to get assetTypeId here, or can we just expect caller to get it for us?
            var w = new Stopwatch();
            w.Start();
            
            var result = await SendCommand("GenerateThumbnail",
                new List<dynamic> {data}, cancellationToken);
            w.Stop();
            if (result.status != 200 || result.data == null)
            {
                if (result.data == null && result.status == 200)
                    Roblox.Metrics.RenderMetrics.ReportRenderAvatarThumbnailFailureDueToNullBody(data.userId);
                Roblox.Metrics.RenderMetrics.ReportRenderAvatarThumbnailFailure(data.userId);
                throw new Exception("Render failed with status = " + result.status);
            }
            Metrics.RenderMetrics.ReportRenderAvatarThumbnailTime(data.userId, w.ElapsedMilliseconds);
            return result.data;
        }

        public static async Task<Stream> RequestPlayerHeadshot(AvatarData data, CancellationToken? cancellationToken = null)
        {
            if (data.playerAvatarType != "R6")
                throw new Exception("Invalid PlayerAvatarType");
            
            // todo: do we need to get assetTypeId here, or can we just expect caller to get it for us?
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailHeadshot", new List<dynamic> {data}, cancellationToken);
        }

        public static async Task<Stream> RequestTextureThumbnail(long assetId, int assetTypeId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailTexture", new List<dynamic>
            {
                assetId, 
                assetTypeId
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestPackageThumbnail(List<long> assetIds, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailPackage", new List<dynamic>
            {
                assetIds, 
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestBodyPartThumbnail(long assetIds, string type, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailBodyPart", new List<dynamic>
            {
                assetIds, 
                type
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestFaceThumbnail(long assetId, int assetTypeId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailFace", new List<dynamic>
            {
                assetId, 
                assetTypeId
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestAssetThumbnail(long assetId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailAsset", new List<dynamic>
            {
                assetId, 
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestClothingThumbnail(long assetId, string clothingtype, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailClothing", new List<dynamic>
            {
                assetId,
                clothingtype
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestHeadThumbnail(long assetId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailHead", new List<dynamic>
            {
                assetId, 
            }, cancellationToken);
        }
        
        public static async Task<Stream> RequestAssetMesh(long assetId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailMesh", new List<dynamic>
            {
                assetId, 
            }, cancellationToken);
        }

        public static async Task<Stream> RequestPlaceConversion(string base64EncodedPlace, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("ConvertRobloxPlace", new List<dynamic>
            {
                base64EncodedPlace, 
            }, cancellationToken);
        }

        public static async Task<Stream> RequestHatConversion(string base64EncodedHat,
            CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("ConvertHat", new List<dynamic>()
            {
                base64EncodedHat,
            });
        }
        
        public static async Task<Stream> RequestAssetGame(long assetId, int x, int y, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailGame", new List<dynamic>
            {
                assetId,
                x,
                y,
            }, cancellationToken);
        }

        public static async Task<Stream> RequestAssetTeeShirt(long assetId, long contentId, CancellationToken? cancellationToken = null)
        {
            return await SendCmdWithErrHandlingAsync("GenerateThumbnailTeeShirt", new List<dynamic>
            {
                assetId,
                contentId,
            }, cancellationToken);
        }
    }
}

