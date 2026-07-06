using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMDT.Models;

namespace TMDT.Services
{
    public class MoMoService
    {
        // Hãy thay thế các thông tin này bằng thông tin lấy từ business.momo.vn
        private const string PartnerCode = "MOMOBKUN20180529";
        private const string AccessKey = "klm05TvNCzjOaHU1";
        private const string SecretKey = "at67qH6mk8w5Y1nAwMoVaT8h7H8o6K";
        private const string Endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
        private const string ReturnUrl = "https://tmdt.local/momo-return";
        private const string NotifyUrl = "https://tmdt.local/momo-notify";

        public static async Task<string?> CreatePaymentUrlAsync(Order order)
        {
            string orderId = order.OrderId.ToString() + "_" + DateTime.Now.Ticks.ToString();
            string requestId = Guid.NewGuid().ToString();
            string amount = ((long)(order.TotalAmount ?? 0)).ToString();
            string orderInfo = $"Thanh toan don hang {order.OrderCode}";
            string requestType = "captureWallet";
            string extraData = "";

            string rawSignature = $"accessKey={AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={NotifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={PartnerCode}&redirectUrl={ReturnUrl}&requestId={requestId}&requestType={requestType}";
            
            string signature = SignHmacSHA256(rawSignature, SecretKey);

            var requestData = new
            {
                partnerCode = PartnerCode,
                partnerName = "TMDT Shop",
                storeId = "MomoTestStore",
                requestId = requestId,
                amount = (long)(order.TotalAmount ?? 0),
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = ReturnUrl,
                ipnUrl = NotifyUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(Endpoint, content);
                string responseString = await response.Content.ReadAsStringAsync();
                
                try
                {
                    dynamic? jsonResponse = JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse != null && jsonResponse.resultCode == 0)
                    {
                        return jsonResponse.payUrl;
                    }
                }
                catch { }
            }
            return null;
        }

        private static string SignHmacSHA256(string message, string secretKey)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                string hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;
            }
        }
    }
}
