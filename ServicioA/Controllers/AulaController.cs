using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AulasController : ControllerBase
{
    private readonly ServicioAContext _context;
    public AulasController(ServicioAContext context) => _context = context;

    // GET api/aulas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AulaDto>>> Get(CancellationToken ct = default)
    {
        var items = await _context.Aulas
            .AsNoTracking()
            .OrderBy(a => a.Codigo)
            .ToListAsync(ct);

        return Ok(items.Select(ToDTO));
    }

    // GET api/aulas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AulaDto>> GetById(int id, CancellationToken ct = default)
    {
        var aula = await _context.Aulas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return aula is null ? NotFound() : Ok(ToDTO(aula));
    }

    // POST api/aulas
    [HttpPost]
    public async Task<ActionResult<AulaDto>> Create([FromBody] AulaDto dto, CancellationToken ct = default)
    {
        var entity = new Aula
        {
            Codigo = dto.Codigo,
            Capacidad = dto.Capacidad,
            Ubicacion = dto.Ubicacion
        };

        _context.Aulas.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    // PUT api/aulas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AulaDto dto, CancellationToken ct = default)
    {
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula is null) return NotFound();

        aula.Codigo = dto.Codigo;
        aula.Capacidad = dto.Capacidad;
        aula.Ubicacion = dto.Ubicacion;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE api/aulas/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula is null) return NotFound();

        _context.Aulas.Remove(aula);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static AulaDto ToDTO(Aula a) => new()
    {
        Id = a.Id,
        Codigo = a.Codigo,
        Capacidad = a.Capacidad,
        Ubicacion = a.Ubicacion
    };
}