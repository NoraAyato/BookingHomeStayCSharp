namespace DoAnCs.Models.Momo
{
    public class MoMoPaymentResponse
    {
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public long Amount { get; set; }
        public string PayUrl { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }
        public string Signature { get; set; }
    }
}