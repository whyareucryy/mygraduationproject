using ComputerRepairService.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities 
{
    public class Inventory
    {
        [Key]
        public int PartId { get; set; }

        [Required]
        [StringLength(100)]
        public string PartName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int CategoryId { get; set; }

        public int QuantityInStock { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        public int ReorderLevel { get; set; } = 5;

        [StringLength(200)]
        public string? SupplierInfo { get; set; } // Изменили на nullable

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual PartCategory PartCategory { get; set; } = null!;

        public virtual ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
    }
}