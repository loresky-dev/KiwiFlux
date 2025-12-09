using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Roblox.Website.Controllers.Internal
{
    public class SignatureController : ControllerBase
    {
        private static RSACryptoServiceProvider? _rsaCsp;
        private static RSACryptoServiceProvider? _rsaCsp2048;
        private static readonly string format = "--rbxsig%{0}%{1}";
        private static readonly string format2048 = "--rbxsig2%{0}%{1}";
        private static readonly string newLine = "\r\n";
        public static void Setup()
        {
            try
            {
                byte[] privateKeyBlob = Convert.FromBase64String(System.IO.File.ReadAllText("PrivateKeyBlob.txt"));
                byte[] privateKeyBlob2048 = Convert.FromBase64String(System.IO.File.ReadAllText("PrivateKeyBlob2048.txt"));
                _rsaCsp = new RSACryptoServiceProvider();
                _rsaCsp.ImportCspBlob(privateKeyBlob);
                _rsaCsp2048 = new RSACryptoServiceProvider();
                _rsaCsp2048.ImportCspBlob(privateKeyBlob2048);
            }
            catch (Exception ex)
            {
                throw new Exception("Error setting up SignatureController: " + ex.Message);
            }
        }

        public static string SignJsonResponseForClientFromPrivateKey(dynamic JSONToSign)
        {
            string script = newLine + JsonConvert.SerializeObject(JSONToSign);
            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format, Convert.ToBase64String(signature), script);
        }
        
        public static string SignJsonResponseForClientFromPrivateKey2048(dynamic JSONToSign)
        {
            string script = newLine + JsonConvert.SerializeObject(JSONToSign);
            byte[] signature = _rsaCsp2048!.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format2048, Convert.ToBase64String(signature), script);
        }

        public static string SignStringResponseForClientFromPrivateKey(string stringToSign, bool bUseRbxSig = false)
        {
            if (bUseRbxSig)
            {
                string script = newLine + stringToSign;
                byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

                return string.Format(format, Convert.ToBase64String(signature), script);
            }
            else
            {
                byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(stringToSign), SHA1.Create());
                return Convert.ToBase64String(signature);
            }
        }
        
        public static string SignStringResponseForClientFromPrivateKey2048(string stringToSign, bool bUseRbxSig = false)
        {
            if (bUseRbxSig)
            {
                string script = newLine + stringToSign;
                byte[] signature = _rsaCsp2048!.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

                return string.Format(format2048, Convert.ToBase64String(signature), script);
            }
            else
            {
                byte[] signature = _rsaCsp2048!.SignData(Encoding.Default.GetBytes(stringToSign), SHA1.Create());
                return Convert.ToBase64String(signature);
            }
        }
    }
}