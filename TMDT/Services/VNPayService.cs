using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TMDT.Models;

namespace TMDT.Services
{
    public class VNPayService
    {
        // Sandbox VNPay test credentials
        private const string vnp_TmnCode = "CGXZLS0Z";
        private const string vnp_HashSecret = "XNBCJFAKAZQSGTARRLGCHVZWCIOIGSHN";
        private const string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private const string vnp_Returnurl = "https://tmdt.local/vnpay-return";

        public static string CreatePaymentUrl(Order order, string ipAddress = "127.0.0.1")
        {
            var vnp_Params = new SortedList<string, string>(new VNPayCompare());
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_Amount", ((long)((order.TotalAmount ?? 0) * 100)).ToString()); // in VND * 100
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_IpAddr", ipAddress);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}");
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
            vnp_Params.Add("vnp_TxnRef", order.OrderId.ToString() + "_" + DateTime.Now.Ticks.ToString()); // Add ticks to make it unique per try
            
            StringBuilder query = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            if (query.Length > 0)
            {
                query.Remove(query.Length - 1, 1);
            }

            string vnp_SecureHash = HmacSHA512(vnp_HashSecret, query.ToString());
            query.Append("&vnp_SecureHash=" + vnp_SecureHash);

            return vnp_Url + "?" + query.ToString();
        }

        public static bool ValidateSignature(Dictionary<string, string> responseData, out int orderId, out string txnRefOut)
        {
            orderId = 0;
            txnRefOut = "";
            string vnp_SecureHash = responseData.GetValueOrDefault("vnp_SecureHash") ?? "";
            if (string.IsNullOrEmpty(vnp_SecureHash)) return false;

            responseData.Remove("vnp_SecureHash");
            responseData.Remove("vnp_SecureHashType");

            var vnp_Params = new SortedList<string, string>(responseData, new VNPayCompare());

            StringBuilder hashData = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    hashData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            if(hashData.Length > 0)
                hashData.Remove(hashData.Length - 1, 1);

            string checkSum = HmacSHA512(vnp_HashSecret, hashData.ToString());

            if (checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase))
            {
                string txnRef = responseData.GetValueOrDefault("vnp_TxnRef") ?? "";
                txnRefOut = txnRef;
                if (!string.IsNullOrEmpty(txnRef))
                {
                    string[] parts = txnRef.Split('_');
                    if(parts.Length > 0 && int.TryParse(parts[0], out int id))
                    {
                        orderId = id;
                    }
                }
                return responseData.GetValueOrDefault("vnp_ResponseCode") == "00";
            }
            return false;
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        private class VNPayCompare : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var vnpCompare = string.Compare(x, y, StringComparison.Ordinal);
                if (vnpCompare != 0) return vnpCompare;
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }
}
