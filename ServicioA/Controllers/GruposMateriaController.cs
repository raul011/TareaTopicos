using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GruposMateriaController : ControllerBase
{
    private readonly ServicioAContext _context;
    public GruposMateriaController(ServicioAContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoMateriaDto>>> Get([FromQuery] int? periodoId, [FromQuery] int? materiaId, [FromQuery] bool soloActivos = true, CancellationToken ct = default)
    {
        var q = _context.GruposMaterias.AsNoTracking();

        if (periodoId.HasValue) q = q.Where(g => g.PeriodoId == periodoId.Value);
        if (materiaId.HasValue) q = q.Where(g => g.MateriaId == materiaId.Value);
        if (soloActivos) q = q.Where(g => g.Estado == "ACTIVO");

        var items = await q.OrderBy(g => g.MateriaId).ThenBy(g => g.Grupo).ToListAsync(ct);
        return Ok(items.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GrupoMateriaDto>> GetById(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return g is null ? NotFound() : Ok(ToDTO(g));
    }

    [HttpPost]
    public async Task<ActionResult<GrupoMateriaDto>> Create([FromBody] GrupoMateriaDto dto, CancellationToken ct)
    {
        var entity = new GrupoMateria
        {
            Grupo = dto.Grupo,
            Cupo = dto.Cupo,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            MateriaId = dto.MateriaId,
            DocenteId = dto.DocenteId,
            PeriodoId = dto.PeriodoId,
            HorarioId = dto.HorarioId,
            AulaId = dto.AulaId
        };
        _context.GruposMaterias.Add(entity);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoMateriaDto dto, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();

        g.Grupo = dto.Grupo;
        g.Cupo = dto.Cupo;
        g.Estado = dto.Estado;
        g.MateriaId = dto.MateriaId;
        g.DocenteId = dto.DocenteId;
        g.PeriodoId = dto.PeriodoId;
        g.HorarioId = dto.HorarioId;
        g.AulaId = dto.AulaId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();
        _context.GruposMaterias.Remove(g);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static GrupoMateriaDto ToDTO(GrupoMateria g) => new()
    {
        Id = g.Id,
        Grupo = g.Grupo,
        Cupo = g.Cupo,
        Estado = g.Estado,
        MateriaId = g.MateriaId,
        DocenteId = g.DocenteId,
        PeriodoId = g.PeriodoId,
        HorarioId = g.HorarioId,
        AulaId = g.AulaId
    };
}
