using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MarathonManager.Web   
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData =
            new(StringComparer.Ordinal);

        private readonly SortedList<string, string> _responseData =
            new(StringComparer.Ordinal);

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData[key] = value;
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.ContainsKey(key) ? _responseData[key] : string.Empty;
        }

        
        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var queryBuilder = new StringBuilder();
            var rawDataBuilder = new StringBuilder();

            var sorted = _requestData.OrderBy(x => x.Key).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var kv = sorted[i];
                if (string.IsNullOrEmpty(kv.Value)) continue;

                rawDataBuilder.Append(kv.Key)
                             .Append('=')
                             .Append(kv.Value);

                queryBuilder.Append(HttpUtility.UrlEncode(kv.Key, Encoding.UTF8))
                            .Append('=')
                            .Append(HttpUtility.UrlEncode(kv.Value, Encoding.UTF8));

                if (i < sorted.Count - 1)
                {
                    rawDataBuilder.Append('&');
                    queryBuilder.Append('&');
                }
            }

            var rawData = rawDataBuilder.ToString();
            var queryString = queryBuilder.ToString();

            string secureHash = GetHashData(rawData, hashSecret);

            return $"{baseUrl}?{queryString}&vnp_SecureHashType=HMACSHA512&vnp_SecureHash={secureHash}";
        }

       
        public bool ValidateSignature(string hashSecret)
        {
            var sorted = _responseData
                .Where(x => x.Key.StartsWith("vnp_")
                            && x.Key != "vnp_SecureHash"
                            && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .ToList();

            var rawDataBuilder = new StringBuilder();

            for (int i = 0; i < sorted.Count; i++)
            {
                var kv = sorted[i];
                if (string.IsNullOrEmpty(kv.Value)) continue;

                rawDataBuilder.Append(kv.Key)
                              .Append('=')
                              .Append(kv.Value);

                if (i < sorted.Count - 1)
                {
                    rawDataBuilder.Append('&');
                }
            }

            string rawData = rawDataBuilder.ToString();

            string receivedHash = _responseData.ContainsKey("vnp_SecureHash")
                ? _responseData["vnp_SecureHash"]
                : string.Empty;

            string calculatedHash = GetHashData(rawData, hashSecret);

            return string.Equals(receivedHash, calculatedHash, StringComparison.OrdinalIgnoreCase);
        }

        
        private string GetHashData(string inputData, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
        }
    }
}
