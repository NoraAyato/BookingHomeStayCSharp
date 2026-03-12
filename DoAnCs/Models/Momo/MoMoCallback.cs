using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DoAnCs.Models.Momo
{
    public class MoMoCallback
    {
        public string PartnerCode { get; set; }
        public string OrderId { get; set; }
        public string RequestId { get; set; }
        public long Amount { get; set; }
        public string OrderInfo { get; set; }
        public string OrderType { get; set; }
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string PayType { get; set; }
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; }
        public string Signature { get; set; }

        public bool IsValidSignature(string accessKey, string secretKey, ILogger logger)
        {
            // Chuẩn hóa dữ liệu
            var safeOrderInfo = OrderInfo ?? "";
            var safeMessage = Message != null ? Regex.Replace(Message, @"\.+$", "") : ""; // Loại bỏ dấu chấm cuối
            var safeExtraData = ExtraData ?? "";

            // Biến thể 1: rawData chuẩn theo MoMo
            var rawData = $"partnerCode={PartnerCode ?? ""}&accessKey={accessKey ?? ""}&requestId={RequestId ?? ""}" +
                          $"&amount={Amount}&orderId={OrderId ?? ""}&orderInfo={safeOrderInfo}" +
                          $"&orderType={OrderType ?? ""}&transId={TransId}&resultCode={ResultCode}" +
                          $"&message={safeMessage}&payType={PayType ?? ""}&responseTime={ResponseTime}" +
                          $"&extraData={safeExtraData}";
            var computedSignature = ComputeHmacSha256(rawData, secretKey);

            // Biến thể 2: OrderInfo không dấu
            var orderInfoNoAccent = RemoveAccents(safeOrderInfo);
            var rawDataNoAccent = $"partnerCode={PartnerCode ?? ""}&accessKey={accessKey ?? ""}&requestId={RequestId ?? ""}" +
                                  $"&amount={Amount}&orderId={OrderId ?? ""}&orderInfo={orderInfoNoAccent}" +
                                  $"&orderType={OrderType ?? ""}&transId={TransId}&resultCode={ResultCode}" +
                                  $"&message={safeMessage}&payType={PayType ?? ""}&responseTime={ResponseTime}" +
                                  $"&extraData={safeExtraData}";
            var computedSignatureNoAccent = ComputeHmacSha256(rawDataNoAccent, secretKey);

            // Ghi log để debug
            logger.LogInformation("Signature validation - RawData: {RawData}", rawData);
            logger.LogInformation("Signature validation - ComputedSignature: {ComputedSignature}", computedSignature);
            logger.LogInformation("Signature validation - RawDataNoAccent: {RawDataNoAccent}", rawDataNoAccent);
            logger.LogInformation("Signature validation - ComputedSignatureNoAccent: {ComputedSignatureNoAccent}", computedSignatureNoAccent);
            logger.LogInformation("Signature validation - ReceivedSignature: {ReceivedSignature}", Signature);

            // Kiểm tra cả hai chữ ký
            return computedSignature.Equals(Signature, StringComparison.OrdinalIgnoreCase) ||
                   computedSignatureNoAccent.Equals(Signature, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using var hmacsha256 = new HMACSHA256(keyBytes);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var hash = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string RemoveAccents(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var normalized = input.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}