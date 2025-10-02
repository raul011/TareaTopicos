using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Services;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class GruposMateriaController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;
    private readonly WorkerHost _workerHost;
    public GruposMateriaController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg,WorkerHost workerHost)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
        _workerHost = workerHost;
    }

    // === ENDPOINTS SÍNCRONOS EXISTENTES ===

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoMateriaResponseDto>>> GetTodos(CancellationToken ct = default)
    {
        var grupos = await _context.GruposMaterias
            .AsNoTracking()
            .Include(x => x.Materia).ThenInclude(m => m.Nivel)
            .Include(x => x.Docente)
            .Include(x => x.Periodo)
            .Include(x => x.Horario)
            .Include(x => x.Aula)
            .OrderBy(x => x.MateriaId).ThenBy(x => x.Grupo)
            .ToListAsync(ct);

        var completos = grupos
            .Where(g => g.Materia != null &&
                        g.Materia.Nivel != null &&
                        g.Docente != null &&
                        g.Periodo != null &&
                        g.Horario != null &&
                        g.Aula != null)
            .Select(ToResponseDTO)
            .ToList();

        return Ok(completos);
    }

    [HttpGet("grupo/{grupo}")]
    public async Task<ActionResult<GrupoMateriaResponseDto>> GetByGrupo(string grupo, CancellationToken ct)
    {
        var g = await _context.GruposMaterias
            .AsNoTracking()
            .Include(x => x.Materia).ThenInclude(m => m.Nivel)
            .Include(x => x.Docente)
            .Include(x => x.Periodo)
            .Include(x => x.Horario)
            .Include(x => x.Aula)
            .FirstOrDefaultAsync(x => x.Grupo == grupo, ct);

        if (g is null)
            return NotFound();

        if (g.Materia is null || g.Materia.Nivel is null || g.Docente is null || g.Periodo is null || g.Horario is null || g.Aula is null)
            return BadRequest($"GrupoMateria con grupo '{grupo}' tiene relaciones incompletas.");

        return Ok(ToResponseDTO(g));
    }

    [HttpPost]
    public async Task<ActionResult<GrupoMateriaRequestDto>> Create([FromBody] GrupoMateriaCreateDto dto, CancellationToken ct)
    {
        var existe = await _context.GruposMaterias.AnyAsync(g => g.Grupo == dto.Grupo, ct);
        if (existe)
            return Conflict($"Ya existe un GrupoMateria con grupo '{dto.Grupo}'.");

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Codigo == dto.MateriaCodigo, ct);
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == dto.DocenteRegistro, ct);
        var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == dto.AulaCodigo, ct);
        var horario = await _context.Horarios.FindAsync(new object[] { dto.HorarioId }, ct);

        if (materia is null || docente is null || periodo is null || aula is null || horario is null)
            return BadRequest("Alguna relación no fue encontrada. Verifica los códigos y registros.");

        var entity = new GrupoMateria
        {
            Grupo = dto.Grupo,
            Cupo = dto.Cupo,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            MateriaId = materia.Id,
            DocenteId = docente.Id,
            PeriodoId = periodo.Id,
            HorarioId = horario.Id,
            AulaId = aula.Id
        };

        _context.GruposMaterias.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetByGrupo), new { grupo = entity.Grupo }, ToDTO(entity));
    }

    [HttpPut("grupo/{grupo}")]
    public async Task<IActionResult> UpdateByGrupo(string grupo, [FromBody] GrupoMateriaCreateDto dto, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Grupo == grupo, ct);
        if (g is null) return NotFound();

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Codigo == dto.MateriaCodigo, ct);
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == dto.DocenteRegistro, ct);
        var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Codigo == dto.AulaCodigo, ct);
        var horario = await _context.Horarios.FindAsync(new object[] { dto.HorarioId }, ct);

        if (materia is null || docente is null || periodo is null || aula is null || horario is null)
            return BadRequest("Una o más relaciones no fueron encontradas. Verifica los códigos y registros.");

        g.Cupo = dto.Cupo;
        g.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado;
        g.MateriaId = materia.Id;
        g.DocenteId = docente.Id;
        g.PeriodoId = periodo.Id;
        g.HorarioId = horario.Id;
        g.AulaId = aula.Id;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("grupo/{grupo}")]
    public async Task<IActionResult> DeleteByGrupo(string grupo, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Grupo == grupo, ct);
        if (g is null) return NotFound();

        _context.GruposMaterias.Remove(g);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // === ENDPOINTS ASÍNCRONOS NUEVOS ===

    // POST /api/gruposmateria/async
    [HttpPost("async")]
    public async Task<IActionResult> CrearGrupoMateriaAsync(
        [FromBody] GrupoMateriaCreateDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        // Verificar si ya existe (idempotencia)
        if (await _context.GruposMaterias.AnyAsync(g => g.Grupo == dto.Grupo, ct))
            return Accepted(new { mensaje = $"GrupoMateria ya existe (grupo '{dto.Grupo}' duplicado)" });

        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // PUT /api/gruposmateria/async/grupo/G01
    [HttpPut("async/grupo/{grupo}")]
    public async Task<IActionResult> ActualizarGrupoMateriaPorGrupoAsync(
        string grupo,
        [FromBody] GrupoMateriaCreateDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        // Forzar el grupo desde la ruta al DTO para consistencia
        dto.Grupo = grupo;

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // DELETE /api/gruposmateria/async/grupo/G01
    [HttpDelete("async/grupo/{grupo}")]
    public async Task<IActionResult> EliminarGrupoMateriaPorGrupoAsync(
        string grupo,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Grupo = grupo };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow
        };
        tx.CallbackUrl ??= _cfg["Webhook:DefaultUrl"];
        tx.CallbackSecret ??= _cfg["Webhook:DefaultSecret"];
        tx.IdempotencyKey ??= tx.Id.ToString();

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    // GET /api/gruposmateria/estado/{id}
    [HttpGet("estado/{id:guid}")]
    public async Task<IActionResult> GetEstado(Guid id, CancellationToken ct = default)
    {
        var tx = await _store.GetAsync(id, ct);
        return tx is null
            ? NotFound(new { mensaje = "Transacción no encontrada" })
            : Ok(new { id = tx.Id, estado = tx.Estado });
    }

    // === MÉTODOS PRIVADOS DE MAPEO ===

    private static GrupoMateriaRequestDto ToDTO(GrupoMateria g) => new()
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

    private static GrupoMateriaResponseDto ToResponseDTO(GrupoMateria m) => new()
    {
        Id = m.Id,
        Grupo = m.Grupo,
        Cupo = m.Cupo,
        Estado = m.Estado,
        Materia = new MateriaResponseDto
        {
            Id = m.Materia.Id,
            Codigo = m.Materia.Codigo,
            Nombre = m.Materia.Nombre,
            Creditos = m.Materia.Creditos,
            Nivel = new NivelDto
            {
                Id = m.Materia.Nivel.Id,
                Numero = m.Materia.Nivel.Numero,
                Nombre = m.Materia.Nivel.Nombre
            }
        },
        Docente = new DocenteDto
        {
            Id = m.Docente.Id,
            Registro = m.Docente.Registro,
            Ci = m.Docente.Ci,
            Nombre = m.Docente.Nombre,
            Telefono = m.Docente.Telefono,
            Estado = m.Docente.Estado
        },
        Periodo = new PeriodoAcademicoResponseDto
        {
            Gestion = m.Periodo.Gestion,
            FechaInicio = m.Periodo.FechaInicio,
            FechaFin = m.Periodo.FechaFin
        },
        Horario = new HorarioDto
        {
            Id = m.Horario.Id,
            Dia = m.Horario.Dia,
            HoraInicio = m.Horario.HoraInicio,
            HoraFin = m.Horario.HoraFin
        },
        Aula = new AulaDto
        {
            Id = m.Aula.Id,
            Codigo = m.Aula.Codigo,
            Capacidad = m.Aula.Capacidad,
            Ubicacion = m.Aula.Ubicacion
        }
    };
}