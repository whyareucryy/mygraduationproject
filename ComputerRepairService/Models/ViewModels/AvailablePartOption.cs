namespace ComputerRepairService.Models.ViewModels
{
    public class AvailablePartOption
    {
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }
        public decimal UnitPrice { get; set; }
        public int ReorderLevel { get; set; }
    }
}
