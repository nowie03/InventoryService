using InventoryService.Models;

namespace InventoryService.ResponseModels
{
    public class ProductGetResponse
    {
        public int Id { get; set; }


        public Category Category { get; set; }


        public double Price { get; set; }


        public string Description { get; set; }

        public string Address { get; set; }

        public IEnumerable<ProductImage> Images { get; set; }


        public ProductGetResponse(int id, Category category, double price, string description, string address, IEnumerable<ProductImage> images)
        {
            Id = id;
            Category = category;
            Price = price;
            Description = description;
            Address = address;
            Images = images;
        }
    }
}
