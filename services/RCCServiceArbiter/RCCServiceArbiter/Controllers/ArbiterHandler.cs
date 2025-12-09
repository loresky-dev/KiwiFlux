using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RCCServiceArbiter.Controllers
{
    public class ArbiterHandler : ControllerBase
    {
        public static Random RandomComponent = new Random();
        public static string GetJsonTemplateForType(string type)
        {
            string path = Path.Combine(Settings.TemplatesPath, $"{type}.json");
            string fileContents = System.IO.File.ReadAllText(path);
            return fileContents;
        }
        
        public static string GetXmlTemplateForType(string type)
        {
            string path = Path.Combine(Settings.TemplatesPath, $"{type}.xml");
            string fileContents = System.IO.File.ReadAllText(path);
            return fileContents;
        }

        public static string GetRenderScriptForType(string type)
        {
            string path = Path.Combine(Settings.TemplatesPath, $"{type}.lua");
            string fileContents = System.IO.File.ReadAllText(path);
            return fileContents;
        }
        
        public static async Task<string?> PostToRcc(string URL, string XML, string SOAPAction, bool returnFailValue = false)
        {
            //Console.WriteLine($"[PostToRcc] Request with type {SOAPAction} sent to RCC.");
            using (HttpClient RccHttpClient = new HttpClient())
            {
                RccHttpClient.DefaultRequestHeaders.Add("SOAPAction", $"http://roblox.com/{SOAPAction}");
                HttpContent XMLContent = new StringContent(XML, Encoding.UTF8, "text/xml");

                try
                {
                    HttpResponseMessage RccHttpClientPost = await RccHttpClient.PostAsync(URL, XMLContent);
                    string RccHttpClientResponse = await RccHttpClientPost.Content.ReadAsStringAsync();
                    
                    if (!RccHttpClientPost.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[PostToRcc] RCC request returned status code (not OK): {RccHttpClientPost.StatusCode}, full response: {RccHttpClientResponse}");
                        // just incase, don't actually return the response and keep it within server logs
                        if (returnFailValue)
                        {
                            XDocument Docc = XDocument.Parse(RccHttpClientResponse);
                            XNamespace ns11 = "http://roblox.com/";
                            XElement Elementt = Docc.Descendants(ns11 + "value").FirstOrDefault()!;
                            string LuaValuee = Elementt.Value;
                            return LuaValuee;
                        }
                    }
                    
                    XDocument Doc = XDocument.Parse(RccHttpClientResponse);
                    XNamespace ns1 = "http://roblox.com/";
                    XElement Element = Doc.Descendants(ns1 + "value").FirstOrDefault()!;
                    string LuaValue = Element.Value;
                    return LuaValue;
                }
                catch (Exception PostToRccException)
                {
                    Console.WriteLine($"[PostToRcc] Critical error: {PostToRccException}");
                }
            }

            return null;
        }
        
        public static async Task PostToRccGS(string URL, string XML, string SOAPAction)
        {
            using (HttpClient RccHttpClient = new HttpClient())
            {
                RccHttpClient.DefaultRequestHeaders.Add("SOAPAction", $"http://roblox.com/{SOAPAction}");
                HttpContent XMLContent = new StringContent(XML, Encoding.UTF8, "text/xml");

                try
                {
                    HttpResponseMessage RccHttpClientPost = await RccHttpClient.PostAsync(URL, XMLContent);
                    string RccHttpClientResponse = await RccHttpClientPost.Content.ReadAsStringAsync();
                    
                    if (!RccHttpClientPost.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[PostToRcc] RCC request returned status code (not OK): {RccHttpClientPost.StatusCode}, full response: {RccHttpClientResponse}");
                        // just incase, don't actually return the response and keep it within server logs
                    }
                    
                    XDocument Doc = XDocument.Parse(RccHttpClientResponse);
                    XNamespace ns1 = "http://roblox.com/";
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[PostToRcc] Critical error: {e}");
                }
            }
        }
    }

    public class RccConfigurer : ControllerBase
    {
        public static void ConfigureRccProcess(Process process, int rccPort)
        {
            process.StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                //WindowStyle = ProcessWindowStyle.Hidden,
                FileName = $"{Settings.RCC2016EPath}RCCService.exe",
                Arguments = $@"-Console {rccPort}",
                RedirectStandardError = false,
                RedirectStandardOutput = false
            };
        }

        public static void ConfigureRccProcess2018(Process process, int rccPort)
        {
            process.StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                //WindowStyle = ProcessWindowStyle.Hidden,
                FileName = $"{Settings.RCC2018LPath}RCCService.exe",
                Arguments = $@"-Console {rccPort} -Verbose",
                RedirectStandardError = false,
                RedirectStandardOutput = false
            };
        }

        public static Process OpenRccProcess(int rccPort)
        {
            Process rccProcess = new Process();
            ConfigureRccProcess2018(rccProcess, rccPort);
            return rccProcess;
        }

        public static int GenerateRccPort()
        {
            return ArbiterHandler.RandomComponent.Next(10000, 25000);
        }
    }
}