using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;

using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InscripcionesController : ControllerBase
{
    private readonly ServicioAContext _context;
    public InscripcionesController(ServicioAContext context) => _context = context;

    // POST: api/inscripciones
    [HttpPost]
    public async Task<ActionResult<InscripcionRequestDto>> Create([FromBody] InscripcionRequestDto dto, CancellationToken ct)
    {
        var existeEst = await _context.Estudiantes.AnyAsync(e => e.Id == dto.EstudianteId, ct);
        var existePer = await _context.PeriodosAcademicos.AnyAsync(p => p.Id == dto.PeriodoId, ct);
        if (!existeEst || !existePer) return BadRequest("Estudiante o Periodo inválido.");

        var duplicada = await _context.Inscripciones
            .AnyAsync(i => i.EstudianteId == dto.EstudianteId && i.PeriodoId == dto.PeriodoId, ct);
        if (duplicada) return Conflict("El estudiante ya tiene una inscripción en este período.");

        var entity = new Inscripcion
        {
            Fecha = dto.Fecha == default ? DateTime.UtcNow : dto.Fecha,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "PENDIENTE" : dto.Estado,
            EstudianteId = dto.EstudianteId,
            PeriodoId = dto.PeriodoId
        };

        _context.Inscripciones.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    // GET: api/inscripciones/{id}
    [HttpGet("{id:int}")]
public async Task<ActionResult<InscripcionResponseDto>> GetById(int id, CancellationToken ct)
{
    var i = await _context.Inscripciones
    .AsNoTracking()
    .Include(x => x.Estudiante)
        .ThenInclude(e => e.Carrera)
    .Include(x => x.Periodo)
    .FirstOrDefaultAsync(x => x.Id == id, ct);

    return i is null ? NotFound() : Ok(ToResponseDTO(i));
}

    // GET: api/inscripciones/por-estudiante/{estudianteId}
    [HttpGet("por-estudiante/{estudianteId:int}")]
    public async Task<ActionResult<IEnumerable<InscripcionResponseDto>>> GetByEstudiante(int estudianteId, [FromQuery] int? periodoId, CancellationToken ct)
    {
        var query = _context.Inscripciones.AsNoTracking()
            .Where(i => i.EstudianteId == estudianteId);

        if (periodoId.HasValue) query = query.Where(i => i.PeriodoId == periodoId.Value);

        var list = await query.OrderByDescending(i => i.Fecha).ToListAsync(ct);
        return Ok(list.Select(ToDTO));
    }

    // GET: api/inscripciones/{id}/detalles
    [HttpGet("{id:int}/detalles")]
    public async Task<ActionResult<IEnumerable<DetalleInscripcionDto>>> GetDetalles(int id, CancellationToken ct)
    {
        var detalles = await _context.DetallesInscripciones
            .AsNoTracking()
            .Where(d => d.InscripcionId == id)
            .ToListAsync(ct);

        return Ok(detalles.Select(ToDTO));
    }

    // POST: api/inscripciones/{id}/finalizar
    [HttpPost("{id:int}/finalizar")]
    public async Task<IActionResult> Finalizar(int id, CancellationToken ct)
    {
        var insc = await _context.Inscripciones.Include(i => i.Detalles).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (insc is null) return NotFound();
        if (!insc.Detalles.Any()) return BadRequest("No se puede finalizar sin materias.");

        insc.Estado = "FINALIZADA";
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // Utilidades de mapeo
    private static InscripcionRequestDto ToDTO(Inscripcion i) => new()
    {
        Id = i.Id,
        Fecha = i.Fecha,
        Estado = i.Estado,
        EstudianteId = i.EstudianteId,
        PeriodoId = i.PeriodoId
    };
   
   //PARA LECTURA DE OBJETOS DENTRO DE INSCRIPCION
   private static InscripcionResponseDto ToResponseDTO(Inscripcion i) => new()
     {
    Id = i.Id,
    Fecha = i.Fecha,
    Estado = i.Estado,
    Estudiante = new EstudianteResponseDto
    {
        Registro = i.Estudiante.Registro,
        Ci = i.Estudiante.Ci,
        Nombre = i.Estudiante.Nombre,
        Email = i.Estudiante.Email,
        Telefono = i.Estudiante.Telefono,
        Direccion = i.Estudiante.Direccion,
        Estado = i.Estudiante.Estado,
        Carrera = new CarreraDto
        {
            Id = i.Estudiante.Carrera.Id,
            Nombre = i.Estudiante.Carrera.Nombre
        }
    },
    Periodo = new PeriodoAcademicoResponseDto
    {
        Gestion = i.Periodo.Gestion,
        FechaInicio = i.Periodo.FechaInicio,
        FechaFin = i.Periodo.FechaFin
    }
};

    private static DetalleInscripcionDto ToDTO(DetalleInscripcion d) => new()
    {
        Id = d.Id,
        Codigo = d.Codigo,
        Estado = d.Estado,
        InscripcionId = d.InscripcionId,
        GrupoMateriaId = d.GrupoMateriaId
    };
}
