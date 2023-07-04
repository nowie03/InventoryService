using InventoryService.Context;
using InventoryService.MessageBroker;
using InventoryService.Models;
using InventoryService.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace InventoryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ServiceContext _context;
        private readonly IMessageBrokerClient _rabbitMQClient;

        public ProductsController(ServiceContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _rabbitMQClient = serviceProvider.GetRequiredService<IMessageBrokerClient>();
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductGetResponse>>> GetProducts(int limit, int skip)
        {
            if (_context.Products == null)
            {
                return NotFound();
            }
            try
            {

                var products = await _context.Products
                        .Skip((skip - 1) * limit)
                        .Take(limit)
                        .AsNoTracking()
                        .ToListAsync();

                List<ProductGetResponse> response = new();

                foreach (Product product in products)
                {
                    Category? category = await _context.Categories.FindAsync(product.CategoryId) ?? throw new Exception($"unable to find category for product {product.Id}");
                    IEnumerable<ProductImage>? productImages = _context.ProductImages.Where(image => image.ProductId == product.Id) ?? throw new Exception($"unable to get images for products {product.Id}");

                    response.Add(new ProductGetResponse(product.Id, category, product.Price, product.Description, product.Address, productImages));
                }
                return Ok(response);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            if (_context.Products == null)
            {
                return NotFound();
            }
            Product? product = await _context.Products.FindAsync(id);

            if (product == null) return NotFound();

            try
            {

                Category? category = await _context.Categories.FindAsync(product.CategoryId) ?? throw new Exception($"unable to find category for product {product.Id}");
                IEnumerable<ProductImage>? productImages = _context.ProductImages.AsNoTracking().Where(image => image.ProductId == product.Id) ?? throw new Exception($"unable to get images for products {product.Id}");

                return Ok(new ProductGetResponse(product.Id, category, product.Price, product.Description, product.Address, productImages));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        [Route("category")]
        public async Task<ActionResult<IEnumerable<Category>>> getCategories()
        {
            if (_context.Categories == null)
            {
                return NotFound();
            }

            return Ok(_context.Categories.AsNoTracking());
        }

        [HttpGet]
        [Route("image")]
        public async Task<ActionResult<IEnumerable<ProductImage>>> getImages(int productId)
        {
            if (_context.ProductImages == null)
            {
                return NotFound();
            }

            return Ok(_context.ProductImages.AsNoTracking().Where(image => image.ProductId == productId));
        }





        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Route("category")]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            if (_context.Categories == null)
                return NoContent();

            try
            {

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();
                return Ok(category);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("image")]
        public async Task<ActionResult<Category>> PostProductImage(ProductImage productImage)
        {
            if (_context.ProductImages == null)
                return NoContent();

            Product? product = await _context.Products
                .FindAsync(productImage.ProductId);

            if (product == null) return BadRequest();

            try
            {

                await _context.ProductImages.AddAsync(productImage);
                await _context.SaveChangesAsync();
                return Ok(productImage);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'ServiceContext.Products'  is null.");
            }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        [HttpDelete]
        [Route("image")]
        public async Task<ActionResult> DeleteProductImage(int id)
        {
            if (_context.ProductImages == null)
                return NoContent();

            ProductImage? productImage = await _context.ProductImages.FindAsync(id);

            if (productImage == null)
                return BadRequest();

            _context.ProductImages.Remove(productImage);
            await _context.SaveChangesAsync();

            return NoContent();

        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (_context.Products == null)
            {
                return NotFound();
            }
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Products.Remove(product);
                ProductImage[] images = _context.ProductImages.Where(image => image.ProductId == id).ToArray();
                _context.ProductImages.RemoveRange(images);
                await _context.SaveChangesAsync();

                //send message to outbox
                string serializedProduct = JsonConvert.SerializeObject(product);
                ulong nextSequenceNumber = _rabbitMQClient.GetNextSequenceNumber();

                Message message = new(Constants.EventTypes.PRODUCT_DELETED, serializedProduct, nextSequenceNumber, Constants.EventStates.EVENT_ACK_PENDING);

                await _context.AddAsync(message);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {

                await transaction.RollbackAsync();
                return Problem(ex.Message);
            }



        }

        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
