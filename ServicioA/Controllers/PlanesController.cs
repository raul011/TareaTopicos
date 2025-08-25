using Microsoft.AspNetCore.Mvc;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //nuevo
    [Authorize]
    public class PlanesController : ControllerBase
    {
        private readonly ServicioAContext _context;

        public PlanesController(ServicioAContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlanes()
        {
            var planes = await _context.PlanesEstudio
                .Include(p => p.PlanMaterias)
                .ThenInclude(pm => pm.Materia)
                .ToListAsync();
            return Ok(planes);
        }
        //nuevo
        [HttpGet("publico")]
        [AllowAnonymous]
        public IActionResult GetPublico()
        {
            return Ok("Este endpoint es público y no requiere token ✅");
        }
    }
}
