using System;
using System.Security.Cryptography;
using System.Text;

namespace DoAnCs.Models.Momo
{
    public class MoMoPaymentRequest
    {
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public long Amount { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string RedirectUrl { get; set; }
        public string IpnUrl { get; set; }
        public string RequestType { get; set; }
        public string ExtraData { get; set; }
        public string Signature { get; set; }
        public string Lang { get; set; }

        public MoMoPaymentRequest(
            string partnerCode,
            string requestId,
            long amount,
            string orderId,
            string orderInfo,
            string redirectUrl,
            string ipnUrl,
            string requestType,
            string extraData,
            string lang)
        {
            PartnerCode = partnerCode;
            RequestId = requestId;
            Amount = amount;
            OrderId = orderId;
            OrderInfo = orderInfo;
            RedirectUrl = redirectUrl;
            IpnUrl = ipnUrl;
            RequestType = requestType;
            ExtraData = extraData;
            Lang = lang;
        }

        public void GenerateSignature(string accessKey, string secretKey)
        {
            var rawData = $"accessKey={accessKey}&amount={Amount}&extraData={ExtraData}&ipnUrl={IpnUrl}" +
                          $"&orderId={OrderId}&orderInfo={OrderInfo}&partnerCode={PartnerCode}" +
                          $"&redirectUrl={RedirectUrl}&requestId={RequestId}&requestType={RequestType}";
            Signature = ComputeHmacSha256(rawData, secretKey);
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                var hash = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}