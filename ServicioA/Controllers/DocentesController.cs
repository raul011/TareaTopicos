using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocentesController : ControllerBase
{
    private readonly ServicioAContext _context;

    public DocentesController(ServicioAContext context)
    {
        _context = context;
    }

    // GET: api/docentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocenteDto>>> GetAll(CancellationToken ct = default)
    {
        var docentes = await _context.Docentes
            .AsNoTracking()
            .OrderBy(d => d.Nombre)
            .ToListAsync(ct);

        return Ok(docentes.Select(ToDto));
    }

    // GET: api/docentes/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DocenteDto>> GetById(int id, CancellationToken ct = default)
    {
        var docente = await _context.Docentes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return docente is null ? NotFound() : Ok(ToDto(docente));
    }

    // POST: api/docentes
    [HttpPost]
    public async Task<ActionResult<DocenteDto>> Create([FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var entity = new Docente
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado
        };

        _context.Docentes.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    // PUT: api/docentes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (docente is null) return NotFound();

        docente.Registro = dto.Registro;
        docente.Ci = dto.Ci;
        docente.Nombre = dto.Nombre;
        docente.Telefono = dto.Telefono;
        docente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? docente.Estado : dto.Estado;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/docentes/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (docente is null) return NotFound();

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static DocenteDto ToDto(Docente d) => new()
    {
        Id = d.Id,
        Registro = d.Registro,
        Ci = d.Ci,
        Nombre = d.Nombre,
        Telefono = d.Telefono,
        Estado = d.Estado
    };
}