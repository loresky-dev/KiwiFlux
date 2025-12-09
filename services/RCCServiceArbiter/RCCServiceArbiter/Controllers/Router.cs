using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RCCServiceArbiter;
using RCCServiceArbiter.Controllers;

namespace Fossci.RccServiceArbiter.Controllers
{
    [ApiController]
    [Route("/")]
    public class ArbiterRouter : ControllerBase
    {
        // TODO: Add a better settings system for this as the JSON replacements for thumbnails is REALLY ugly.
        [HttpGet("/")]
        public string OkMessage()
        {
            return "ARBITER OK!!!";
        }

        [HttpGet("/get-json-template")]
        public string GetJsonTemplate(string type)
        {
            return ArbiterHandler.GetJsonTemplateForType(type);
        }
        
        [HttpGet("/get-xml-template")]
        public string GetXmlTemplate(string type)
        {
            return ArbiterHandler.GetXmlTemplateForType(type);
        }

        [HttpPost("request-thumbnail-avatar")]
        public async Task<string> RequestThumbnailAvatar(string characterAppearanceUrl, bool R15)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments;
            if (R15)
            {
                arguments = $@"""{Settings.BaseUrl}""" + ",\n" + $@"""{characterAppearanceUrl}""" + ",\n" + @"""Png""" + ",\n840,\n840"; // 840x840 resolution in ThumbnailGenerator
            }
            else
            {
                arguments = $@"""{characterAppearanceUrl}""" + ",\n" + $@"""{Settings.BaseUrl}""" + ",\n" + @"""Png""" + ",\n840,\n840"; // 840x840 resolution in ThumbnailGenerator
            }
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", R15 ? "Avatar_R15_Action" : "Avatar")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-closeup")]
        public async Task<string> RequestThumbnailCloseup(string characterAppearanceUrl)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}""" + ",\n" + $@"""{characterAppearanceUrl}""" + ",\n" + @"""png""" + ",\n720,\n720,\ntrue,\n30,\n100,\n0,\n0"; // 30 and 100 basehatzoom and other settings etc
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Closeup")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-hat")]
        public async Task<string> RequestThumbnailHat(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}&guid={Guid.NewGuid().ToString()}""" + ",\n" + @"""png""" + ",\n1680,\n1680,\n" + $@"""{Settings.BaseUrl}/""";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Hat")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-head")]
        public async Task<string> RequestThumbnailHead(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" +
                               ",\n1680,\n1680,\n" + $@"""{Settings.BaseUrl}/""" + ",\n1785197";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Head")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-place")]
        public async Task<string> RequestThumbnailPlace(long assetId, int x, int y)
        {
            // todo: add universeids
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" +
                               $",\n{x.ToString()},\n{y.ToString()},\n" + $@"""{Settings.BaseUrl}/""" + $",\n{assetId}";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Place")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-shirt")]
        public async Task<string> RequestThumbnailClothing(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" +
                               ",\n1680,\n1680,\n" + $@"""{Settings.BaseUrl}/""" + ",\n1785197";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Shirt")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }

        [HttpPost("request-thumbnail-pants")]
        public async Task<string> RequestThumbnailPants(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" +
                               ",\n1680,\n1680,\n" + $@"""{Settings.BaseUrl}/""" + ",\n1785197";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Pants")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-gear")]
        public async Task<string> RequestThumbnailGear(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" + ",\n1680,\n1680,\n" + $@"""{Settings.BaseUrl}/""";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Gear")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-mesh")]
        public async Task<string> RequestThumbnailMesh(long assetId)
        {
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" + ",\n1260,\n1260,\n" + $@"""{Settings.BaseUrl}/""";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Mesh")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }

        [HttpPost("request-thumbnail-package")]
        public async Task<string> RequestThumbnailPackage([FromQuery] IEnumerable<long> assetIds)
        {
            string mannequinUrl = $"{Settings.BaseUrl}/asset/?id=1785197";
            string assetUrls = "";
            string customUrls = $"{Settings.BaseUrl}/asset/?id=27113661;{Settings.BaseUrl}/asset/?id=25251154";
            foreach (long assetId in assetIds)
            {
                assetUrls = assetUrls + $"{Settings.BaseUrl}/asset/?id={assetId}&guid={Guid.NewGuid().ToString()};";
            }
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{assetUrls}""" + ",\n" + $@"""{Settings.BaseUrl}""" + ",\n" + $@"""png""" + ",\n840,\n840,\n" + $@"""{mannequinUrl}""" + ",\n" + $@"""{customUrls}""";  
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Package")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-bodypart")]
        public async Task<string> RequestThumbnailBodyPart(long assetId, string BodyPartType)
        {
            string customUrl;
            switch (BodyPartType)
            {
                case "Torso":
                    customUrl = $"{Settings.BaseUrl}/asset/?id=25251062&guid={Guid.NewGuid().ToString()}";
                    break;
                case "LeftArm":
                    customUrl = $"{Settings.BaseUrl}/asset/?id=25251081&guid={Guid.NewGuid().ToString()}";
                    break;
                case "RightArm":
                    customUrl = $"{Settings.BaseUrl}/asset/?id=25251071&guid={Guid.NewGuid().ToString()}";
                    break;
                case "LeftLeg":
                    customUrl = $"{Settings.BaseUrl}/asset/?id=25251138&guid={Guid.NewGuid().ToString()}";
                    break;
                case "RightLeg":
                    customUrl = $"{Settings.BaseUrl}/asset/?id=25251144&guid={Guid.NewGuid().ToString()}";
                    break;
                default:
                    return "BAD";
            }
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}&guid={Guid.NewGuid().ToString()}""" + ",\n" + $@"""{Settings.BaseUrl}/""" + ",\n" + @"""png""" +
                               ",\n1680,\n1680,\n" +
                               $@"""{Settings.BaseUrl}/asset/?id=1785197""" + ",\n" + $@"""{customUrl}""";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "BodyPart")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }
        
        [HttpPost("request-thumbnail-image")]
        public async Task<string> RequestThumbnailImage(long assetId, bool isFace)
        {
            int xRes = isFace ? 1680 : 600;
            int yRes = isFace ? 1680 : 600;
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + @"""png""" + $",\n{xRes},\n{yRes},\n" + $@"""{Settings.BaseUrl}/""";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "Decal")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx") ?? "BAD";
            rccProcess.Kill();
            return result;
        }

        [HttpPost("request-thumbnail-texture")]
        public async Task<string> RequestThumbnailTexture(long assetId)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Requester", "Server");
            client.DefaultRequestHeaders.Add("User-Agent", "Roblox/WinInet");
            client.DefaultRequestHeaders.Add("accessKey", "qr13q9rqeiofjlfadefjlaedfjoiaqoipjfaoijg");
            string requestUrl = $"{Settings.BaseUrl}/asset/?id={assetId}&guid={Guid.NewGuid().ToString()}";
            HttpResponseMessage result =
                await client.GetAsync(requestUrl);
            byte[] asString = await result.Content.ReadAsByteArrayAsync();
            Console.WriteLine(await result.Content.ReadAsStringAsync());
            string base64 = Convert.ToBase64String(asString);
            return base64;
        }
        
        
        [HttpPost("validate-place")]
        public async Task<string> ValidatePlace(long assetId)
        {
            // todo: add universeids
            int port = RccConfigurer.GenerateRccPort();
            Process rccProcess = RccConfigurer.OpenRccProcess(port);
            rccProcess.Start(); // start while we set it up to not lose time
            // Setup JSON
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("Thumbnail");
            string arguments = $@"""{Settings.BaseUrl}/asset/?id={assetId}""" + ",\n" + $@"""{Settings.BaseUrl}""" +
                               ",\n" + $"{assetId}";
            string finalJson = JSONTemplate.Replace("%THUMBNAIL_TYPE%", "ValidatePlace")
                .Replace("%ARGUMENTS%", arguments);
            // Setup XML
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("BatchJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", Guid.NewGuid().ToString()).Replace("%expiration%", "15").Replace("%script%", finalJson); // 15 second job expiration
            //Console.WriteLine(finalXML);
            string result = await ArbiterHandler.PostToRcc($"http://127.0.0.1:{port}", finalXML, "BatchJobEx", true) ?? "BAD";
            rccProcess.Kill();
            return result;
        }

        [HttpPost("evict-player")]
        public async Task KickPlayerFromServer(string jobId, int port, long userId)
        {
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("EvictPlayer");
            string finalJson = JSONTemplate.Replace("%PLAYER_ID%", userId.ToString());
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("ExecuteEx");
            string finalXML = XMLTemplate.Replace("%jobid%", jobId).Replace("%script%", finalJson); // 15 second job expiration
            await ArbiterHandler.PostToRccGS($"http://127.0.0.1:{port}", finalXML, "ExecuteEx");
        }
        
        [HttpPost("kill-game-server")]
        public void KillGameServer(int processId)
        {
            Process process = Process.GetProcessById(processId);
            process.Kill();
        }
        
        // TODO: Add maxplayers properly, and UniverseId (creator type too)
        [HttpPost("start-game-server")]
        public async Task<int> StartGameServer(
            int mainRCCPort, int networkServerPort, long placeId, int jobExpiration, string jobId, long creatorId, string apiKey, string placeVisitAccessKey, int MaxPlayers = 30
            )
        {
            // Start up RCC
            Process serverRcc = new Process();
            RccConfigurer.ConfigureRccProcess2018(serverRcc, mainRCCPort);
            serverRcc.Start();
            string JSONTemplate = ArbiterHandler.GetJsonTemplateForType("GameServer");
            string finalJson = JSONTemplate.Replace(
                "%PLACE_ID%", $"{placeId}").Replace(
                "%CREATOR_ID%", $"{creatorId}").Replace(
                "%JOB_ID", jobId).Replace(
                "%MACHINE_ADDRESS%", "127.0.0.1").Replace(
                "%MAX_PLAYERS%", $"{MaxPlayers}").Replace(
                "%UNIVERSE_ID%", $"{placeId}").Replace( //universeID todo
                "%BASE_URL%", Settings.BaseUrl.Replace("https://www.", "").Replace("http://www.", "")).Replace(
                "%PLACE_FETCH_URL%", $"{Settings.BaseUrl}/asset/?id={placeId}").Replace(
                "%MATCHMAKING_CONTEXT_ID%", "1").Replace(
                "%CREATOR_TYPE%", "User").Replace(
                "%PLACE_VERSION%", "1").Replace(
                "%PORT%", $"{networkServerPort}").Replace(
                "%API_KEY%", apiKey).Replace(
                "%PLACE_VISIT_ACCESS_KEY%", placeVisitAccessKey);
            string XMLTemplate = ArbiterHandler.GetXmlTemplateForType("OpenJobEx");
            string finalXML = XMLTemplate.Replace("%jobid%", jobId).Replace("%expiration%", $"{jobExpiration}").Replace("%script%", finalJson);
            await ArbiterHandler.PostToRccGS($"http://127.0.0.1:{mainRCCPort}", finalXML, "OpenJobEx");
            return serverRcc.Id;
        }
    }
}