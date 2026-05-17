using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.ViewModels
{
    public class IssueOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string DeviceDescription { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal AmountDue { get; set; }
        
        [Display(Name = "Способ оплаты (если есть долг)")]
        public string PaymentMethod { get; set; } = "Наличные";
    }
}
