using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using olx_be_api.Data;
using olx_be_api.Helpers;

namespace olx_be_api.Controllers
{
    [Route("api/premium")]
    [ApiController]
    public class PremiumController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PremiumController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPremiumPackages()
        {
            var package = await _context.PremiumPackages.Where(p => p.IsActive).ToListAsync();

            return Ok(new ApiResponse<object> { success = true, data = package });
        }
    }
}
