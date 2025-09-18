using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GruposMateriaController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly QueueManager _qm;
    private readonly ITransaccionStore _store;
    private readonly IConfiguration _cfg;

    public GruposMateriaController(ServicioAContext context, QueueManager qm, ITransaccionStore store, IConfiguration cfg)
    {
        _context = context;
        _qm = qm;
        _store = store;
        _cfg = cfg;
    }
    /*
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoMateriaResponseDto>>> Get([FromQuery] int? periodoId, [FromQuery] int? materiaId, [FromQuery] bool soloActivos = true, CancellationToken ct = default)
        {
            var q = _context.GruposMaterias.AsNoTracking();

            if (periodoId.HasValue) q = q.Where(g => g.PeriodoId == periodoId.Value);
            if (materiaId.HasValue) q = q.Where(g => g.MateriaId == materiaId.Value);
            if (soloActivos) q = q.Where(g => g.Estado == "ACTIVO");

            var items = await q.OrderBy(g => g.MateriaId).ThenBy(g => g.Grupo).ToListAsync(ct);
            return Ok(items.Select(ToDTO));
        }

        */
    /*
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GrupoMateriaRequestDto>> GetById(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return g is null ? NotFound() : Ok(ToDTO(g));
    }
    */

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoMateriaResponseDto>>> GetTodos(CancellationToken ct = default)
    {
        var grupos = await _context.GruposMaterias
            .AsNoTracking()
            .Include(x => x.Materia)
                .ThenInclude(m => m.Nivel)
            .Include(x => x.Docente)
            .Include(x => x.Periodo)
            .Include(x => x.Horario)
            .Include(x => x.Aula)
            .OrderBy(x => x.MateriaId)
            .ThenBy(x => x.Grupo)
            .Where(g => g.Materia != null &&
                        g.Materia.Nivel != null &&
                        g.Docente != null &&
                        g.Periodo != null &&
                        g.Horario != null &&
                        g.Aula != null)
            .ToListAsync(ct);

        var completos = grupos.Select(ToResponseDTO).ToList();

        return Ok(completos);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<GrupoMateriaResponseDto>> GetById(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias
            .AsNoTracking()
            .Include(x => x.Materia)
                .ThenInclude(m => m.Nivel)
            .Include(x => x.Docente)
            .Include(x => x.Periodo)
            .Include(x => x.Horario)
            .Include(x => x.Aula)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (g is null)
            return NotFound();

        // Validación defensiva para evitar NullReferenceException
        if (g.Materia is null || g.Materia.Nivel is null || g.Docente is null || g.Periodo is null || g.Horario is null || g.Aula is null)
            return BadRequest($"GrupoMateria con ID {id} tiene relaciones incompletas.");

        return Ok(ToResponseDTO(g));
    }
    // Este endpoint ahora es interno o para casos donde se conoce el ID.
    // El endpoint principal para el usuario será el que usa claves naturales.
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)] // Oculta de Swagger para no confundir
    public async Task<ActionResult<GrupoMateriaRequestDto>> CreateWithIds([FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        var entity = new GrupoMateria { /* ... mapeo ... */ };
        _context.GruposMaterias.Add(entity);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDTO(entity));
    }

    // Este es el endpoint que el usuario debería usar.
    [HttpPost("by-code")]
    public async Task<IActionResult> CreateWithNaturalKeys([FromBody] GrupoMateriaNaturalKeyRequestDto naturalDto, CancellationToken ct)
    {
        // La lógica de creación ahora se delega al procesador asíncrono.
        return await CrearGrupoMateriaAsync(naturalDto, "default", 1, null, ct);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();

        g.Grupo = dto.Grupo;
        g.Cupo = dto.Cupo;
        g.Estado = dto.Estado;
        g.MateriaId = dto.MateriaId;
        g.DocenteId = dto.DocenteId;
        g.PeriodoId = dto.PeriodoId;
        g.HorarioId = dto.HorarioId;
        g.AulaId = dto.AulaId;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var g = await _context.GruposMaterias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (g is null) return NotFound();
        _context.GruposMaterias.Remove(g);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // === ENDPOINTS ASÍNCRONOS (COLA) ===

    [HttpPost("async")]
    public async Task<IActionResult> CrearGrupoMateriaAsync(
        [FromBody] GrupoMateriaNaturalKeyRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var tx = new Transaccion
        {
            TipoOperacion = "POST",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/{id:int}")] // Se mantiene el ID aquí porque es un identificador de recurso, no una clave de negocio
    public async Task<IActionResult> ActualizarGrupoMateriaAsync(
        int id,
        [FromBody] GrupoMateriaNaturalKeyRequestDto dto,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        // Creamos un payload que el procesador pueda entender
        var payload = new { Dto = dto, Id = id };

        var tx = new Transaccion
        {
            TipoOperacion = "PUT",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpDelete("async/{id:int}")] // Se mantiene el ID aquí porque es un identificador de recurso
    public async Task<IActionResult> EliminarGrupoMateriaAsync(
        int id,
        [FromQuery] string? queue = "default",
        [FromQuery] int priority = 1,
        [FromQuery] DateTimeOffset? notBeforeUtc = null,
        CancellationToken ct = default)
    {
        var payload = new { Id = id };

        var tx = new Transaccion
        {
            TipoOperacion = "DELETE",
            Entidad = "GrupoMateria",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            Priority = Math.Clamp(priority, 0, 2),
            NotBefore = notBeforeUtc ?? DateTimeOffset.UtcNow,
            CallbackUrl = _cfg["Webhook:DefaultUrl"],
            CallbackSecret = _cfg["Webhook:DefaultSecret"],
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        await _qm.EnqueueAsync(tx, queue, ct);
        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

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


    //PARA LECTURA DE OBJETOS DENTRO DE INSCRIPCION
    private static GrupoMateriaResponseDto ToResponseDTO(GrupoMateria g) => new()
    {
        Id = g.Id,
        Grupo = g.Grupo,
        Cupo = g.Cupo,
        Estado = g.Estado,
        Materia = g.Materia is null ? null : new MateriaResponseDto
        {
            Id = g.Materia.Id,
            Codigo = g.Materia.Codigo,
            Nombre = g.Materia.Nombre,
            Creditos = g.Materia.Creditos,
            Nivel = g.Materia.Nivel is null ? null : new NivelDto
            {
                Id = g.Materia.Nivel.Id,
                Numero = g.Materia.Nivel.Numero,
                Nombre = g.Materia.Nivel.Nombre
            }
        },
        Docente = g.Docente is null ? null : new DocenteDto
        {
            Id = g.Docente.Id,
            Registro = g.Docente.Registro,
            Ci = g.Docente.Ci,
            Nombre = g.Docente.Nombre,
            Telefono = g.Docente.Telefono,
            Estado = g.Docente.Estado
        },
        Periodo = g.Periodo is null ? null : new PeriodoAcademicoResponseDto
        {
            Gestion = g.Periodo.Gestion,
            FechaInicio = g.Periodo.FechaInicio,
            FechaFin = g.Periodo.FechaFin
        },
        Horario = g.Horario is null ? null : new HorarioDto
        {
            Id = g.Horario.Id,
            Dia = g.Horario.Dia,
            HoraInicio = g.Horario.HoraInicio,
            HoraFin = g.Horario.HoraFin
        },
        Aula = g.Aula is null ? null : new AulaDto
        {
            Id = g.Aula.Id,
            Codigo = g.Aula.Codigo,
            Capacidad = g.Aula.Capacidad,
            Ubicacion = g.Aula.Ubicacion
        }
    };
}
