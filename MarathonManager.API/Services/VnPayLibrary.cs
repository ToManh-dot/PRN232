// VnPayLibrary.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MarathonManager.API.Services
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(StringComparer.Ordinal);
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(StringComparer.Ordinal);

        // Thêm dữ liệu gửi đi
        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _requestData[key] = value;
        }

        // Thêm dữ liệu nhận về
        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _responseData[key] = value;
        }

        // Lấy giá trị
        public string GetResponseData(string key) => _responseData.ContainsKey(key) ? _responseData[key] : "";

        // Tạo URL thanh toán
        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append($"{kv.Key}={HttpUtility.UrlEncode(kv.Value, Encoding.UTF8)}&");
                }
            }

            // 2️⃣ Tạo chuỗi dữ liệu để hash (bỏ ký tự & cuối)
            string queryString = data.ToString().TrimEnd('&');

            // 3️⃣ Tạo chữ ký (HMAC-SHA512)
            string secureHash = GetHashData(queryString, hashSecret);

            // 4️⃣ Gắn SecureHash và SecureHashType vào URL cuối cùng
            string paymentUrl = $"{baseUrl}?{queryString}&vnp_SecureHashType=HMACSHA512&vnp_SecureHash={secureHash}";

            return paymentUrl;
        }

        public bool ValidateSignature(string hashSecret)
        {
            // Sắp xếp theo thứ tự alphabet key (theo tài liệu VNPAY)
            var sortedData = _responseData
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key)
                .ToList();

            var data = new StringBuilder();
            foreach (var kv in sortedData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append($"{kv.Key}={HttpUtility.UrlEncode(kv.Value, Encoding.UTF8)}&");
                }
            }

            string queryString = data.ToString().TrimEnd('&');
            string receivedHash = _responseData.ContainsKey("vnp_SecureHash") ? _responseData["vnp_SecureHash"] : "";

            string calculatedHash = GetHashData(queryString, hashSecret);

            return string.Equals(receivedHash, calculatedHash, StringComparison.OrdinalIgnoreCase);
        }

        private string GetHashData(string inputData, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }
    }
}