using System.ComponentModel.DataAnnotations;

namespace InventoryService.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public double Price { get; set; }

        public string Description { get; set; }

        [Required]
        public string Address { get; set; }

        public DateTime CreatedAt { get; set; }


    }
}
