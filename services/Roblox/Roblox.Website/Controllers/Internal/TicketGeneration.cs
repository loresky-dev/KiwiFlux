using Roblox.Website.Controllers.Internal;

namespace Roblox.Website.Controllers
{
    public class TicketGeneration
    {
        /// <summary>
        /// Generate a ClientTicket that RCC verifies, this format is for V1 tickets
        /// </summary>
        public static string GenerateClientTicketV1(long userId, string username, string jobId, string dateTime, string characterAppearanceUrl)
        {
            string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignatureController.SignStringResponseForClientFromPrivateKey(ticket2);
            string ticket = $"{userId}\n{jobId}\n{dateTime}";
            string ticketSignature = SignatureController.SignStringResponseForClientFromPrivateKey(ticket);
            // Final ticket
            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature}";
            return finalTicket;
        }
        
        /// <summary>
        /// Generate a ClientTicket that RCC verifies, this format is for v2 tickets
        /// </summary>
        public static string GenerateClientTicketV2(long userId, string username, string jobId, string dateTime)
        {
            // the second userid is meant to be characterAppearanceId
            string ticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket2);
            string ticket = $"{userId}\n{jobId}\n{dateTime}";
            string ticketSignature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket);
            // Final ticket
            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};2";
            return finalTicket;
        }
        
        /// <summary>
        /// Generate a ClientTicket that RCC verifies, this format is for V3 tickets
        /// </summary>
        public static string GenerateClientTicketV3(long userId, string username, string jobId, string dateTime)
        {
            // the second userid is meant to be characterAppearanceId
            string ticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket2);
            string ticket = $"{userId}\n{jobId}\n{dateTime}";
            string ticketSignature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket);
            // Final ticket
            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};3";
            return finalTicket;
        }
        
        /// <summary>
        /// Generate a ClientTicket that RCC verifies, this format is for V4 tickets
        /// </summary>
        public static string GenerateClientTicketV4(long userId, string username, string jobId, string dateTime, string characterAppearanceUrl, long accountAge, long followUserId, string countryCode, string membershipType)
        {
            // 0ABFE7F4  0F1825F0  "12/27/2023 6:49:21 PM\nid\n1\n1\n0\n69\nf\n6\nROBLOX\n4\nNone\n2\nUS\n0\n\n6\nROBLOX"
            string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{dateTime}";
            string ticket2Signature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket2);
            // the second userid is meant to be characterAppearanceId
            // the second username.Length and username is meant to be display name
            string ticket = $"{dateTime}\n{jobId}\n{userId}\n{userId}\n{followUserId}\n{accountAge}\nf\n{username.Length}\n{username}\n{membershipType.Length}\n{membershipType}\n{countryCode.Length}\n{countryCode}\n0\n\n{username.Length}\n{username}";
            string ticketSignature = SignatureController.SignStringResponseForClientFromPrivateKey2048(ticket);
            // Final ticket
            string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};4";
            return finalTicket;
        }
    }
}