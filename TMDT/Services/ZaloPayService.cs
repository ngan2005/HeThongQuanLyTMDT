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
    public class ZaloPayService
    {
        // Hãy thay thế các thông tin này bằng Sandbox Key của ZaloPay
        private const string AppId = "2553";
        private const string Key1 = "PcY4iZIKFCIdgZvA6ueMcMHHUbRLYjPL";
        private const string Endpoint = "https://sb-openapi.zalopay.vn/v2/create";

        public static async Task<string?> CreatePaymentUrlAsync(Order order)
        {
            var random = new Random();
            long transId = random.Next(1000000);
            string app_trans_id = DateTime.Now.ToString("yyMMdd") + "_" + transId;
            long amount = (long)(order.TotalAmount ?? 0);
            string app_user = "user_" + order.BuyerId;
            long app_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            string item = "[]";
            string embed_data = "{}";
            string description = $"Thanh toan don hang {order.OrderCode}";
            string bank_code = "";

            string macData = $"{AppId}|{app_trans_id}|{app_user}|{amount}|{app_time}|{embed_data}|{item}";
            string mac = SignHmacSHA256(macData, Key1);

            var requestData = new Dictionary<string, string>
            {
                { "app_id", AppId },
                { "app_user", app_user },
                { "app_time", app_time.ToString() },
                { "amount", amount.ToString() },
                { "app_trans_id", app_trans_id },
                { "embed_data", embed_data },
                { "item", item },
                { "description", description },
                { "bank_code", bank_code },
                { "mac", mac }
            };

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(requestData);
                var response = await client.PostAsync(Endpoint, content);
                string responseString = await response.Content.ReadAsStringAsync();
                
                try
                {
                    dynamic? jsonResponse = JsonConvert.DeserializeObject(responseString);
                    if (jsonResponse != null && jsonResponse.return_code == 1)
                    {
                        return jsonResponse.order_url;
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
