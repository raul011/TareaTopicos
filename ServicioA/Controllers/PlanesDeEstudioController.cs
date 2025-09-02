using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanesDeEstudioController : ControllerBase
{
    private readonly ServicioAContext _context;
    public PlanesDeEstudioController(ServicioAContext context) => _context = context;

    // GET api/planesdeestudio
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanDeEstudioDto>>> Get(CancellationToken ct = default)
    {
        var items = await _context.PlanesEstudio
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    // GET api/planesdeestudio/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanDeEstudioDto>> GetById(int id, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return plan is null ? NotFound() : Ok(ToDTO(plan));
    }

    // POST api/planesdeestudio
    [HttpPost]
    public async Task<ActionResult<PlanDeEstudioDto>> Create([FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        var entity = new PlanDeEstudio
        {
            Nombre = dto.Nombre,
            Codigo = dto.Codigo,
            Fecha = dto.Fecha,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId
        };

        _context.PlanesEstudio.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    // PUT api/planesdeestudio/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlanDeEstudioDto dto, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();

        plan.Nombre = dto.Nombre;
        plan.Codigo = dto.Codigo;
        plan.Fecha = dto.Fecha;
        plan.Estado = dto.Estado;
        plan.CarreraId = dto.CarreraId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
    
    // GET api/planesdeestudio/{id}/materias
[HttpGet("{id:int}/materias")]
public async Task<ActionResult<IEnumerable<MateriaRequestDto>>> GetMateriasDePlan(int id, CancellationToken ct = default)
{
    // 1. Validar existencia del plan
    var existePlan = await _context.PlanesEstudio
        .AsNoTracking()
        .AnyAsync(p => p.Id == id, ct);

    if (!existePlan)
        return NotFound($"No existe un PlanDeEstudio con Id={id}");

    // 2. Consultar materias asociadas al plan
    var materias = await _context.PlanMaterias
        .AsNoTracking()
        .Where(pm => pm.PlanId == id)
        .Include(pm => pm.Materia) // Incluimos datos de Materia
        .Select(pm => new MateriaRequestDto
        {
            Id = pm.Materia.Id,
            Codigo = pm.Materia.Codigo,
            Nombre = pm.Materia.Nombre,
            Creditos = pm.Materia.Creditos,
            NivelId = pm.Materia.NivelId
        })
        .OrderBy(m => m.Nombre)
        .ToListAsync(ct);

    // 3. Retornar resultado
    return Ok(materias);
}

    // DELETE api/planesdeestudio/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var plan = await _context.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return NotFound();

        _context.PlanesEstudio.Remove(plan);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static PlanDeEstudioDto ToDTO(PlanDeEstudio p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Codigo = p.Codigo,
        Fecha = p.Fecha,
        Estado = p.Estado,
        CarreraId = p.CarreraId
    };
}