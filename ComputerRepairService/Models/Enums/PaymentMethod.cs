namespace ComputerRepairService.Models.Enums
{
    /// <summary>Значения поля Payment.PaymentMethod (совпадают с опциями в представлениях и БД).</summary>
    public static class PaymentMethodCodes
    {
        public const string Cash = "Cash";
        public const string Card = "Card";
        public const string BankTransfer = "Bank Transfer";
        public const string Online = "Online";

        public static readonly string[] All = { Cash, Card, BankTransfer, Online };
    }
}
