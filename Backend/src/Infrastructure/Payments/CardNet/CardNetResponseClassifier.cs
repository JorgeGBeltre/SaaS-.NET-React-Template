namespace Infrastructure.Payments.CardNet
{
    public enum CardNetResponseCategory
    {
        Success,
        InsufficientFunds,
        CardExpiredOrInvalid,
        ConfigurationOrData,
        InfrastructureError,
        DeclinedGeneric
    }

    public static class CardNetResponseClassifier
    {
        public static CardNetResponseCategory Classify(string? responseCode, bool isSuccess)
        {
            if (isSuccess || responseCode == "00" || responseCode == "0")
            {
                return CardNetResponseCategory.Success;
            }

            var code = (responseCode ?? "").Trim().ToUpperInvariant();
            return code switch
            {
                "51" or "INSUFFICIENT_FUNDS" => CardNetResponseCategory.InsufficientFunds,
                "54" or "14" or "41" or "43" or "EXPIRED_CARD" or "INVALID_CARD" => CardNetResponseCategory.CardExpiredOrInvalid,
                "99" or "INVALID_CVV" or "CVV_ERROR" => CardNetResponseCategory.ConfigurationOrData,
                "91" or "COMMUNICATION_ERROR" or "TIMEOUT" or "PARSE_ERROR" or "SYSTEM_UNAVAILABLE" or "HOST_UNAVAILABLE" => CardNetResponseCategory.InfrastructureError,
                _ => CardNetResponseCategory.DeclinedGeneric
            };
        }

        public static bool IsInfrastructureError(string? responseCode, bool isSuccess)
        {
            return Classify(responseCode, isSuccess) == CardNetResponseCategory.InfrastructureError;
        }

        public static bool IsRetryableWithSameCard(string? responseCode, bool isSuccess)
        {
            var category = Classify(responseCode, isSuccess);
            return category is CardNetResponseCategory.InsufficientFunds or CardNetResponseCategory.InfrastructureError;
        }

        public static string GetUserFriendlyMessage(string? responseCode, bool isSuccess, string? fallbackMessage = null)
        {
            if (isSuccess || responseCode == "00" || responseCode == "0")
            {
                return "Transacción aprobada exitosamente.";
            }

            var category = Classify(responseCode, isSuccess);
            return category switch
            {
                CardNetResponseCategory.InsufficientFunds =>
                    "No pudimos procesar el pago porque el banco emisor rechazó la transacción por fondos insuficientes.",
                CardNetResponseCategory.CardExpiredOrInvalid =>
                    "La tarjeta ha expirado o no es válida. Por favor registre un nuevo método de pago.",
                CardNetResponseCategory.ConfigurationOrData =>
                    "Hubo un problema de verificación con los datos de seguridad de la tarjeta (CVV o expiración).",
                CardNetResponseCategory.InfrastructureError =>
                    "El procesador de pagos CardNET no estuvo disponible temporalmente para verificar la transacción. Por favor intente más tarde.",
                _ => !string.IsNullOrWhiteSpace(fallbackMessage) && !fallbackMessage.StartsWith("Error de comunicación", System.StringComparison.OrdinalIgnoreCase)
                    ? fallbackMessage
                    : "La transacción no pudo ser autorizada por el banco emisor. Por favor verifique con su banco o utilice otra tarjeta."
            };
        }
    }
}
