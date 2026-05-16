namespace ComputerRepairService.Models.Enums
{
    /// <summary>Идентификаторы статусов заказа (таблица OrderStatuses).</summary>
    public static class OrderStatusIds
    {
        public const int New = 1;
        public const int Diagnostics = 2;
        public const int WaitingForParts = 3;
        public const int InRepair = 4;
        /// <summary>Готово к получению (после оплаты).</summary>
        public const int ReadyForPickup = 5;
        public const int Issued = 6;
        public const int Cancelled = 7;
        /// <summary>Ожидание оплаты (мастер завершил работу, указал стоимость).</summary>
        public const int AwaitingPayment = 8;

        public static readonly int[] ActiveWorkStatuses = { Diagnostics, WaitingForParts, InRepair };
    }
}
