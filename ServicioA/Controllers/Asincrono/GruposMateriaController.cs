using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos.response;
using TAREATOPICOS.ServicioA.Services;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers.Sincrono;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class GruposMateriaAsyncController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;
    public GruposMateriaAsyncController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
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
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        // Validaciones previas
        var materia = await _context.Materias.AnyAsync(m => m.Id == dto.MateriaId, ct);
        if (!materia) return BadRequest("La materia especificada no existe.");

        var tx = new Transaccion
        {
            Entidad = "GrupoMateria",
            TipoOperacion = "CrearGrupoMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] GrupoMateriaRequestDto dto, CancellationToken ct)
    {
        if (!await _context.GruposMaterias.AnyAsync(g => g.Id == id, ct))
            return NotFound(new { mensaje = "El grupo de materia no existe." });

        dto.Id = id;

        var tx = new Transaccion
        {
            Entidad = "GrupoMateria",
            TipoOperacion = "ActualizarGrupoMateria",
            Payload = JsonSerializer.Serialize(dto),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        if (!await _context.GruposMaterias.AnyAsync(g => g.Id == id, ct))
            return NotFound(new { mensaje = "El grupo de materia no existe." });

        var payload = new { Id = id };

        var tx = new Transaccion
        {
            Entidad = "GrupoMateria",
            TipoOperacion = "EliminarGrupoMateria",
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA"
        };

        await _store.AddAsync(tx);
        await _queue.EnqueueAsync(tx);

        return Accepted(new { id = tx.Id, estado = tx.Estado });
    }

    [HttpGet("estado/{txId:guid}")]
    public async Task<IActionResult> Estado(Guid txId, CancellationToken ct)
    {
        var tx = await _store.GetAsync(txId);
        if (tx is null) return NotFound(new { mensaje = "Transacción no encontrada" });
        return Ok(new { id = tx.Id, estado = tx.Estado });
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
