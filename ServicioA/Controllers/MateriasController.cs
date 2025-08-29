using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MateriasController : ControllerBase
{
    private readonly ServicioAContext _context;

    public MateriasController(ServicioAContext context)
    {
        _context = context;
    }

    // GET: api/materias
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MateriaDto>>> GetAll(CancellationToken ct = default)
    {
        var materias = await _context.Materias
            .AsNoTracking()
            .OrderBy(m => m.Codigo)
            .ToListAsync(ct);

        return Ok(materias.Select(ToDto));
    }

    // GET: api/materias/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MateriaDto>> GetById(int id, CancellationToken ct = default)
    {
        var materia = await _context.Materias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return materia is null ? NotFound() : Ok(ToDto(materia));
    }

    // POST: api/materias
    [HttpPost]
    public async Task<ActionResult<MateriaDto>> Create([FromBody] MateriaDto dto, CancellationToken ct = default)
    {
        var entity = new Materia
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Creditos = dto.Creditos,
            NivelId = dto.NivelId
        };

        _context.Materias.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    // PUT: api/materias/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MateriaDto dto, CancellationToken ct = default)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia is null) return NotFound();

        materia.Codigo = dto.Codigo;
        materia.Nombre = dto.Nombre;
        materia.Creditos = dto.Creditos;
        materia.NivelId = dto.NivelId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/materias/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia is null) return NotFound();

        _context.Materias.Remove(materia);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // Mapeo interno
    private static MateriaDto ToDto(Materia m) => new()
    {
        Id = m.Id,
        Codigo = m.Codigo,
        Nombre = m.Nombre,
        Creditos = m.Creditos,
        NivelId = m.NivelId
    };
}