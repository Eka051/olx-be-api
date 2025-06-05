using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.DTO;
using olx_be_api.Helpers;
using olx_be_api.Models;

namespace olx_be_api.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Random _random = new Random();
        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<long> GenerateProductId()
        {
            long newId;
            bool exists;

            do
            {
                newId = _random.NextInt64(100_000_000L, 1_000_000_000L);
                exists = await _context.Products.AnyAsync(p => p.Id == newId);
            } while (exists);
            return newId;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAllProducts()
        {
            var products = _context.Products.ToList();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO productDTO)
        {
            try
            {
                if (productDTO == null)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Data iklan harus diisi!",
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new ApiErrorResponse
                    {
                        success = false,
                        message = "Invalid data",
                        errors = errors
                    });
                }

                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Title == productDTO.Title && p.CategoryId == productDTO.CategoryId);
                if (existingProduct != null)
                {
                    return Conflict(new ApiErrorResponse
                    {
                        success = false,
                        message = $"Iklan {existingProduct.Title} sudah ada. Ganti dengan judul iklan yang lain! "
                    });
                }

                //var newProduct = new Product
                //{
                //    Id = await GenerateProductId(),
                //    Title = productDTO.Title,
                //    Description = productDTO.Description,
                //    Price = productDTO.Price,
                //    CategoryId = productDTO.CategoryId,
                //    LocationId = productDTO.LocationId,
                //    Images = productDTO.Images ?? new List<string>(),
                //    CreatedAt = DateTime.UtcNow
                //};


            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
                {
                    success = false,
                    message = "An error occurred while creating the product",
                    errors = ex.Message
                });
            }
        }
    }
}
