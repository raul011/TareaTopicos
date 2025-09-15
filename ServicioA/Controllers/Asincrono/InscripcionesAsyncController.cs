using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;

namespace TAREATOPICOS.ServicioA.Controllers.Asincrono;

[ApiController]
[Route("api/[controller]")]
public class InscripcionesAsyncController : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue; // encola la transacción (Redis)
    private readonly ITransaccionStore _store;    // guarda/lee estado (Redis)
    private readonly ServicioAContext _db;        // EF Core (solo para lecturas síncronas de negocio)

    public InscripcionesAsyncController(IBackgroundTaskQueue queue, ITransaccionStore store, ServicioAContext db)
    {
        _queue = queue;
        _store = store;
        _db = db;
    }

    // 1) Crear inscripción (ASÍNCRONO: encola y responde 202)
    // POST /api/inscripciones/async
    [HttpPost("async")]
    public async Task<IActionResult> CrearInscripcionAsync([FromBody] CrearInscripcionAsyncDto dto, CancellationToken ct)
    {
        // Validación previa: ¿Existen el estudiante y el período?
        var estudiante = await _db.Estudiantes.AsNoTracking().FirstOrDefaultAsync(e => e.Registro == dto.Registro, ct);
        if (estudiante is null) return BadRequest(new { mensaje = "El registro del estudiante no existe." });
        var periodo = await _db.PeriodosAcademicos.AsNoTracking().FirstOrDefaultAsync(p => p.Gestion == dto.Gestion, ct);
        if (periodo is null) return BadRequest(new { mensaje = "La gestión del período no existe." });

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "CrearInscripcion",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 2) Agregar detalle (ASÍNCRONO)
    // POST /api/inscripciones/detalles/async
    [HttpPost("detalles/async")]
    public async Task<IActionResult> AgregarDetalleAsync([FromBody] AgregarDetalleAInscripcionRequestDto dto, CancellationToken ct)
    {
        // Buscamos la inscripción por registro y gestión
        var inscripcion = await _db.Inscripciones.AsNoTracking()
            .Include(i => i.Estudiante)
            .Include(i => i.Periodo)
            .FirstOrDefaultAsync(i => i.Estudiante.Registro == dto.Registro && i.Periodo.Gestion == dto.Gestion, ct);

        // Validación previa
        if (inscripcion is null) return NotFound(new { mensaje = "Inscripción no encontrada." });

        var grupoMateria = await _db.GruposMaterias.AsNoTracking()
            .Include(gm => gm.Materia)
            .FirstOrDefaultAsync(gm => gm.Materia.Codigo == dto.MateriaCodigo && gm.Grupo == dto.Grupo && gm.PeriodoId == inscripcion.PeriodoId, ct);
        if (grupoMateria is null) return BadRequest(new { mensaje = "El grupo para la materia especificada no existe en el período de la inscripción." });

        var payload = new
        {
            InscripcionId = inscripcion.Id, // Internamente usamos el ID
            dto.MateriaCodigo,
            dto.Grupo
        };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "AgregarDetalle",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 3a) Quitar detalle por DetalleId (ASÍNCRONO)
    // DELETE /api/inscripciones/detalles/async
    [HttpDelete("detalles/async")]
    public async Task<IActionResult> QuitarDetalleAsync([FromBody] QuitarDetalleDeInscripcionRequestDto dto, CancellationToken ct)
    {
        var inscripcion = await _db.Inscripciones.AsNoTracking()
            .Include(i => i.Estudiante)
            .Include(i => i.Periodo)
            .FirstOrDefaultAsync(i => i.Estudiante.Registro == dto.Registro && i.Periodo.Gestion == dto.Gestion, ct);

        if (inscripcion is null) return NotFound(new { mensaje = "Inscripción no encontrada." });

        // Validación previa
        var detalle = await _db.DetallesInscripciones.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dto.DetalleId && d.InscripcionId == inscripcion.Id, ct);
        if (detalle is null) return NotFound(new { mensaje = "El detalle de inscripción no existe o no pertenece a la inscripción indicada." });

        var dto = new QuitarDetalleAsyncDto
        {
            DetalleId = detalleId
        };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorId",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 3b) Quitar por Código de Materia y Grupo (ASÍNCRONO)
    // DELETE /api/inscripciones/detalles/por-materia/async
    [HttpDelete("detalles/por-materia/async")]
    public async Task<IActionResult> QuitarDetallePorMateriaAsync([FromBody] QuitarDetallePorMateriaDeInscripcionRequestDto dto, CancellationToken ct)
    {
        var inscripcion = await _db.Inscripciones.AsNoTracking()
            .Include(i => i.Estudiante)
            .Include(i => i.Periodo)
            .FirstOrDefaultAsync(i => i.Estudiante.Registro == dto.Registro && i.Periodo.Gestion == dto.Gestion, ct);

        if (inscripcion is null) return NotFound(new { mensaje = "Inscripción no encontrada." });

        // Validación previa
        var detalle = await _db.DetallesInscripciones.AsNoTracking()
            .Include(d => d.GrupoMateria.Materia)
            .FirstOrDefaultAsync(d => d.InscripcionId == inscripcion.Id &&
                                      d.GrupoMateria.Materia.Codigo == dto.MateriaCodigo &&
                                      d.GrupoMateria.Grupo == dto.Grupo, ct);
        if (detalle is null) return NotFound(new { mensaje = "No se encontró un detalle con esa materia y grupo en la inscripción." });

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "QuitarDetallePorMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 4) Finalizar inscripción (ASÍNCRONO)
    // POST /api/inscripciones/finalizar/async
    [HttpPost("finalizar/async")]
    public async Task<IActionResult> FinalizarInscripcionAsync([FromBody] FinalizarInscripcionRequestDto dto, CancellationToken ct)
    {
        var inscripcion = await _db.Inscripciones.AsNoTracking()
            .Include(i => i.Estudiante)
            .Include(i => i.Periodo)
            .FirstOrDefaultAsync(i => i.Estudiante.Registro == dto.Registro && i.Periodo.Gestion == dto.Gestion, ct);

        // Validación previa
        if (inscripcion is null) return NotFound(new { mensaje = "Inscripción no encontrada." });

        // El payload para el worker sí llevará el ID
        var payload = new { InscripcionId = inscripcion.Id };

        var tx = new Transaccion
        {
            Entidad = "Inscripcion",
            TipoOperacion = "FinalizarInscripcion",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // 5) Consultar estado de una transacción (ASÍNCRONO)
    // GET /api/inscripcionesasync/estado/{txId}
    [HttpGet("estado/{txId:guid}")]
    public async Task<IActionResult> Estado(Guid txId, CancellationToken ct)
    {
        var tx = await _store.GetAsync(txId);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });
        return Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // 6) Oferta (Lectura síncrona)
    // GET /api/inscripciones/oferta
    [HttpGet("oferta")]
    public async Task<ActionResult<IEnumerable<GrupoOfertaDto>>> GetOferta([FromQuery] string registro, [FromQuery] string gestion, CancellationToken ct)
    {
        var insc = await _db.Inscripciones
            .AsNoTracking()
            .Include(i => i.Estudiante)
            .Include(i => i.Periodo)
            .FirstOrDefaultAsync(i =>
                i.Estudiante.Registro == registro &&
                i.Periodo.Gestion == gestion, ct);

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
            MateriaCodigo = g.Materia.Codigo,
            MateriaNombre = g.Materia.Nombre,
            Grupo = g.Grupo,
            Cupo = g.Cupo,
            Docente = g.Docente?.Nombre ?? "",
            Aula = g.Aula?.Codigo ?? "",
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
