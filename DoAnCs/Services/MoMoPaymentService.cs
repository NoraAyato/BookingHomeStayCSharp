using DoAnCs.Models.Momo;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace DoAnCs.Services
{
    public class MoMoPaymentService : IMoMoPaymentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MoMoSettings _moMoSettings;
        private readonly ILogger<MoMoPaymentService> _logger;

        public MoMoPaymentService(
            IHttpClientFactory httpClientFactory,
            IOptions<MoMoSettings> moMoSettings,
            ILogger<MoMoPaymentService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _moMoSettings = moMoSettings?.Value ?? throw new ArgumentNullException(nameof(moMoSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MoMoPaymentResponse> CreatePaymentAsync(string orderId, long amount, string orderInfo)
        {
            // Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogError("OrderId không được để trống.");
                throw new ArgumentException("OrderId không được để trống.", nameof(orderId));
            }
            if (string.IsNullOrWhiteSpace(orderInfo))
            {
                _logger.LogError("OrderInfo không được để trống.");
                throw new ArgumentException("OrderInfo không được để trống.", nameof(orderInfo));
            }
            if (amount <= 0)
            {
                _logger.LogError("Số tiền phải lớn hơn 0.");
                throw new ArgumentException("Số tiền phải lớn hơn 0.", nameof(amount));
            }

            // Kiểm tra cấu hình MoMoSettings
            if (string.IsNullOrWhiteSpace(_moMoSettings.PartnerCode) ||
                string.IsNullOrWhiteSpace(_moMoSettings.AccessKey) ||
                string.IsNullOrWhiteSpace(_moMoSettings.SecretKey) ||
                string.IsNullOrWhiteSpace(_moMoSettings.ApiUrl) ||
                string.IsNullOrWhiteSpace(_moMoSettings.ReturnUrl) ||
                string.IsNullOrWhiteSpace(_moMoSettings.NotifyUrl))
            {
                _logger.LogError("Cấu hình MoMoSettings không đầy đủ: PartnerCode={PartnerCode}, ApiUrl={ApiUrl}",
                    _moMoSettings.PartnerCode, _moMoSettings.ApiUrl);
                throw new InvalidOperationException("Cấu hình MoMoSettings không đầy đủ.");
            }

            // Ghi log cấu hình
            _logger.LogInformation("MoMoSettings: PartnerCode={PartnerCode}, ApiUrl={ApiUrl}, ReturnUrl={ReturnUrl}, NotifyUrl={NotifyUrl}",
                _moMoSettings.PartnerCode, _moMoSettings.ApiUrl, _moMoSettings.ReturnUrl, _moMoSettings.NotifyUrl);

            // Tạo MoMoPaymentRequest
            var requestId = Guid.NewGuid().ToString();
            var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
            var request = new MoMoPaymentRequest(
                partnerCode: _moMoSettings.PartnerCode,
                requestId: requestId,
                amount: amount,
                orderId: orderId,
                orderInfo: orderInfo,
                redirectUrl: _moMoSettings.ReturnUrl,
                ipnUrl: _moMoSettings.NotifyUrl,
                requestType: "captureWallet",
                extraData: extraData,
                lang: "vi"
            );

            // Tạo chữ ký
            request.GenerateSignature(_moMoSettings.AccessKey, _moMoSettings.SecretKey);

            // Ghi log yêu cầu
            _logger.LogInformation("Yêu cầu MoMo: OrderId={OrderId}, RequestId={RequestId}, Amount={Amount}, Signature={Signature}",
                request.OrderId, request.RequestId, request.Amount, request.Signature);

            // Chuyển đổi sang JSON
            var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Gửi yêu cầu
            var httpClient = _httpClientFactory.CreateClient();
            _logger.LogInformation("Gửi yêu cầu đến MoMo API: Url={ApiUrl}, Body={JsonRequest}", _moMoSettings.ApiUrl, jsonRequest);
            var response = await httpClient.PostAsync(_moMoSettings.ApiUrl, content);

            // Đọc phản hồi
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Phản hồi MoMo: StatusCode={StatusCode}, Body={ResponseBody}", response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"MoMo API trả về lỗi: StatusCode={response.StatusCode}, Body={responseBody}");
            }

            // Giải mã phản hồi
            var paymentResponse = JsonSerializer.Deserialize<MoMoPaymentResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return paymentResponse;
        }
    }
}