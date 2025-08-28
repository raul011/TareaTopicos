using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController : ControllerBase
{
    private readonly ServicioAContext _context;
    public EstudiantesController(ServicioAContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EstudianteDto>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Estudiantes.AsNoTracking().ToListAsync(ct);
        return Ok(list.Select(ToDTO));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstudianteDto>> Get(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? NotFound() : Ok(ToDTO(e));
    }

    [HttpPost]
    public async Task<ActionResult<EstudianteDto>> Create([FromBody] EstudianteDto dto, CancellationToken ct)
    {
        var e = new Estudiante
        {
            Registro = dto.Registro,
            Ci = dto.Ci,
            Nombre = dto.Nombre,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            CarreraId = dto.CarreraId
        };
        _context.Estudiantes.Add(e);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = e.Id }, ToDTO(e));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EstudianteDto dto, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();

        e.Registro = dto.Registro;
        e.Ci = dto.Ci;
        e.Nombre = dto.Nombre;
        e.Email = dto.Email;
        e.Telefono = dto.Telefono;
        e.Direccion = dto.Direccion;
        e.Estado = dto.Estado;
        e.CarreraId = dto.CarreraId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _context.Estudiantes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();
        _context.Estudiantes.Remove(e);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static EstudianteDto ToDTO(Estudiante e) => new()
    {
        Id = e.Id,
        Registro = e.Registro,
        Ci = e.Ci,
        Nombre = e.Nombre,
        Email = e.Email,
        Telefono = e.Telefono,
        Direccion = e.Direccion,
        Estado = e.Estado,
        CarreraId = e.CarreraId
    };
}
