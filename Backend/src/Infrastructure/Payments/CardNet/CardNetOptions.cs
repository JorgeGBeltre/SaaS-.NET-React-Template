namespace Infrastructure.Payments.CardNet
{
    public sealed class CardNetOptions
    {
        public const string SectionName = "Payments:CardNet";

        public string Environment { get; set; } = "QA"; // "QA", "Production"
        public string BaseUrl { get; set; } = "https://labservicios.cardnet.com.do/servicios/tokens";
        public string TransactionalUrl { get; set; } = "https://labservicios.cardnet.com.do/servicios/transacciones";
        public string PublicAccountKey { get; set; } = string.Empty;
        public string PrivateAccountKey { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string TerminalId { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "214"; // 214 = DOP (Pesos Dominicanos)
        public string WebhookSecret { get; set; } = string.Empty;
        public bool EnableMockInNonProduction { get; set; } = true;
    }
}
