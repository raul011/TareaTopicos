using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/inscripciones")]
public class InscripcionesController : ControllerBase
{
    private readonly QueueManager _qm;
    private readonly IConfiguration _cfg;
    private readonly ILogger<InscripcionesController> _log;

    public InscripcionesController(QueueManager qm, IConfiguration cfg, ILogger<InscripcionesController> log)
    {
        _qm = qm;
        _cfg = cfg;
        _log = log;
    }

    // === DTOs ===
    public record MateriaGrupoDto(string MateriaCodigo, string Grupo);
    public record InscripcionCreateDto(string Registro, int PeriodoId, List<MateriaGrupoDto> Materias);

 // === POST /api/inscripciones/async ===
[HttpPost("async")]
public async Task<IActionResult> CrearAsync(
    [FromBody] InscripcionCreateDto dto,
    [FromQuery] string? queue = "default",
    [FromQuery] int priority = 1,
    [FromQuery] DateTimeOffset? notBeforeUtc = null,
    CancellationToken ct = default)
{
    if (dto == null || string.IsNullOrWhiteSpace(dto.Registro) || dto.PeriodoId <= 0 || dto.Materias == null || !dto.Materias.Any())
        return BadRequest(new { mensaje = "Registro, PeriodoId y al menos una materia son requeridos." });

    var payloadObj = new
    {
        Registro = dto.Registro.Trim().ToUpperInvariant(),
        PeriodoId = dto.PeriodoId,
        Materias = dto.Materias.Select(m => new
        {
            MateriaCodigo = m.MateriaCodigo.Trim().ToUpperInvariant(),
            Grupo = m.Grupo.Trim().ToUpperInvariant()
        }).ToList()
    };

    try
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "Inscripcion",
            Payload = JsonSerializer.Serialize(payloadObj),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"]
        };

        await _qm.EnqueueAsync(tx, queue, ct);

        _log.LogInformation("Transacción de inscripción encolada. TxId={TxId}", tx.Id);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Error al encolar inscripción");
        return StatusCode(500, new { mensaje = "Error interno", detalle = ex.Message });
    }
}
    

[HttpGet("estado/{registro}")]
public async Task<IActionResult> ObtenerEstado(string registro, [FromServices] ServicioAContext db)
{
    var inscripciones = await db.Inscripciones
        .Include(i => i.Detalles)
        .ThenInclude(d => d.GrupoMateria)
        .ThenInclude(g => g.Materia)
        .Where(i => i.Estudiante.Registro == registro)
        .OrderByDescending(i => i.Fecha)
        .Select(i => new {
            i.Id,
            i.Estado,
            i.Fecha,
            Materias = i.Detalles.Select(d => new {
                d.GrupoMateria.Materia.Nombre,
                d.GrupoMateria.Grupo,
                d.Estado
            })
        })
        .ToListAsync();

    if (!inscripciones.Any())
        return NotFound(new { mensaje = "No hay inscripciones para este estudiante" });

    return Ok(inscripciones);
}










}
 