using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeriodosAcademicosController : ControllerBase
{
    private readonly ServicioAContext _context;
    public PeriodosAcademicosController(ServicioAContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodoAcademicoDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.PeriodosAcademicos.AsNoTracking().OrderByDescending(p => p.FechaInicio).ToListAsync(ct);
        return Ok(list.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PeriodoAcademicoDto>> Get(int id, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(ToDTO(p));
    }

    [HttpPost]
    public async Task<ActionResult<PeriodoAcademicoDto>> Create([FromBody] PeriodoAcademicoDto dto, CancellationToken ct)
    {
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        var p = new PeriodoAcademico { Gestion = dto.Gestion, FechaInicio = dto.FechaInicio, FechaFin = dto.FechaFin };
        _context.PeriodosAcademicos.Add(p);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, ToDTO(p));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PeriodoAcademicoDto dto, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (dto.FechaFin < dto.FechaInicio) return BadRequest("La fecha fin no puede ser anterior a la fecha inicio.");

        p.Gestion = dto.Gestion;
        p.FechaInicio = dto.FechaInicio;
        p.FechaFin = dto.FechaFin;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        _context.PeriodosAcademicos.Remove(p);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static PeriodoAcademicoDto ToDTO(PeriodoAcademico p) => new()
    {
        Id = p.Id,
        Gestion = p.Gestion,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin
    };
}
