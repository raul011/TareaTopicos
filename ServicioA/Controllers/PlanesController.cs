using Microsoft.AspNetCore.Mvc;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Dtos;
namespace TAREATOPICOS.ServicioA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                .Select(p => new PlanEstudioDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Anio = p.Anio,
                    Materias = p.PlanMaterias.Select(pm => new MateriaDto
                    {
                        Nombre = pm.Materia.Nombre,
                        Creditos = pm.Materia.Creditos,
                        Semestre = pm.Semestre
                    }).ToList()
                })
                .ToListAsync();

            return Ok(planes);
        }

  
        
       
       

        
    }
}
