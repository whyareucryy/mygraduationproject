namespace ComputerRepairService.Models.Enums
{
    public static class PaymentStatusCodes
    {
        public const string Completed = "Completed";
        public const string Pending = "Pending";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";

        public static readonly string[] All = { Completed, Pending, Failed, Refunded };
    }
}
