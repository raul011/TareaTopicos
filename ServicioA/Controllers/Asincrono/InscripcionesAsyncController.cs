using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Microsoft.AspNetCore.Authorization;

using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;

namespace TAREATOPICOS.ServicioA.Controllers.Asincrono;


[ApiController]
[Route("api/[controller]")]
// [Authorize] // habilítalor para seguridad
public class InscripcionesAsyncController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly TransaccionStore _store;
    private readonly ServicioAContext _db;

    public InscripcionesAsyncController(IBackgroundTaskQueue queue, TransaccionStore store, ServicioAContext db)
    {
        _queue = queue;
        _store = store;
        _db = db;
    }

 
    // crear inscripción ingresando el id del esstudiantes y el periodo id (asincrono)
    // POST /api/inscripciones/async
 
    
    [HttpPost("async")]
    public IActionResult CrearInscripcionAsync([FromBody] CrearInscripcionAsyncDto dto)
    {
        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "CrearInscripcion",
            Payload = JsonSerializer.Serialize(dto), // guarda datos mínimos
            Estado = "PENDIENTE" // o EN_COLA si así estandarizaste
        };

        _store.Add(tx);
        _queue.Enqueue(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // ======================================
    // 2) Agregar detalle (async)
    // POST /api/inscripciones/{id}/detalles/async
    // Body: { InscripcionId, GrupoMateriaId }
    // ======================================
    [HttpPost("{id:int}/detalles/async")]
    public IActionResult AgregarDetalleAsync(int id, [FromBody] AgregarDetalleAsyncDto dto)
    {
        
        if (dto.InscripcionId != id) return BadRequest("El Id de la inscripción no coincide.");

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "AgregarDetalle",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "PENDIENTE"
        };

        _store.Add(tx);
        _queue.Enqueue(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // ==================================================
    // 3a) Quitar detalle por DetalleId (async, con ruta)
    // DELETE /api/inscripciones/{id}/detalles/{detalleId}/async
    // ==================================================
    [HttpDelete("{id:int}/detalles/{detalleId:int}/async")]
    public IActionResult QuitarDetallePorIdAsync(int id, int detalleId)
    {
        var dto = new QuitarDetallePorIdAsyncDto { InscripcionId = id, DetalleId = detalleId };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorId",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "PENDIENTE"
        };

        _store.Add(tx);
        _queue.Enqueue(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }
 
    // 3b) Quitar un GrupoMateriaId (asincrono)
    // DELETE /api/inscripciones/{id}/detalles/async
    [HttpDelete("{id:int}/detalles/async")]
    public IActionResult QuitarDetallePorGrupoAsync(int id, [FromBody] QuitarDetallePorGrupoAsyncDto dto)
    {
        if (dto.InscripcionId != id) return BadRequest("El Id de la inscripción no coincide.");

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorGrupo",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "PENDIENTE"
        };

        _store.Add(tx);
        _queue.Enqueue(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

     
    // para finalizar la inscripcion (asincrono)
    // POST /api/inscripciones/{id}/finalizar/async
    [HttpPost("{id:int}/finalizar/async")]
    public IActionResult FinalizarInscripcionAsync(int id)
    {
        var dto = new FinalizarInscripcionAsyncDto { InscripcionId = id };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "FinalizarInscripcion",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "PENDIENTE"
        };

        _store.Add(tx);
        _queue.Enqueue(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

     
    // consulta el estado de una transaccion
    // GET /api/inscripciones/estado/{txId}
    [HttpGet("estado/{txId:guid}")]
    public IActionResult Estado(Guid txId)
    {
        var tx = _store.Get(txId);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });
        return Ok(new { id = tx.Id, estado = tx.Estado });// si añadiste Error en Transaccion
    }

    
    // muestra el maestro de oferta habilitada para un estudiante (sincrono)
    // GET /api/inscripciones/{id}/oferta
[HttpGet("{id:int}/oferta")]
public async Task<ActionResult<IEnumerable<GrupoOfertaDto>>> GetOferta(int id, CancellationToken ct)
{
    var insc = await _db.Inscripciones
        .AsNoTracking()
        .FirstOrDefaultAsync(i => i.Id == id, ct);

    if (insc is null) return NotFound("Inscripción no existe.");

    var grupos = await _db.GruposMaterias
        .AsNoTracking()
        .Include(g => g.Materia)
        .Include(g => g.Docente)
        .Include(g => g.Aula)
        .Include(g => g.Horario) 
        .Where(g => g.PeriodoId == insc.PeriodoId)
        .ToListAsync(ct);

    var result = grupos.Select(g => new GrupoOfertaDto
    {
        GrupoMateriaId = g.Id,
        MateriaCodigo  = g.Materia.Codigo,
        MateriaNombre  = g.Materia.Nombre,
        Grupo          = g.Grupo,
        Cupo           = g.Cupo,
        Docente        = g.Docente?.Nombre ?? "",    
        Aula           = g.Aula?.Codigo ?? "",
        Horarios = g.Horario is null
            ? new List<HorarioOfertaDto>()
            : new List<HorarioOfertaDto> {
                new HorarioOfertaDto {
                    Dia        = g.Horario.Dia,
                    HoraInicio = g.Horario.HoraInicio, 
                    HoraFin    = g.Horario.HoraFin     
                }
            }
    }).ToList();

    return Ok(result);
}

}
