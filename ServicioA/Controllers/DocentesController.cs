using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Asegurarse que este using esté presente
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Dtos;
using TAREATOPICOS.ServicioA.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace TAREATOPICOS.ServicioA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocentesController : ControllerBase
{
    private readonly ServicioAContext _context;
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public DocentesController(ServicioAContext context, IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _context = context;
        _queue = queue;
        _store = store;
    }

    #region Endpoints Síncronos
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
    [HttpGet("{registro}")]
    public async Task<ActionResult<DocenteDto>> GetByRegistro(string registro, CancellationToken ct = default)
    {
        var docente = await _context.Docentes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Registro == registro, ct);

        return docente is null ? NotFound() : Ok(ToDto(docente));
    }

    // POST: api/docentes
    [HttpPost]
    public async Task<ActionResult<DocenteDto>> Create([FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        // Validación: Asegurar que el registro del docente sea único.
        if (await _context.Docentes.AnyAsync(d => d.Registro == dto.Registro, ct))
            return Conflict($"Ya existe un docente con el registro '{dto.Registro}'.");

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

        return CreatedAtAction(nameof(GetByRegistro), new { registro = entity.Registro }, ToDto(entity));
    }

    // PUT: api/docentes/{id}
    [HttpPut("{registro}")]
    public async Task<IActionResult> Update(string registro, [FromBody] DocenteDto dto, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == registro, ct);
        if (docente is null) return NotFound();

        docente.Ci = dto.Ci;
        docente.Nombre = dto.Nombre;
        docente.Telefono = dto.Telefono;
        docente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? docente.Estado : dto.Estado;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/docentes/{id}
    [HttpDelete("{registro}")]
    public async Task<IActionResult> Delete(string registro, CancellationToken ct = default)
    {
        var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Registro == registro, ct);
        if (docente is null) return NotFound();

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
    #endregion

    #region Endpoints Asíncronos
    // POST: api/docentes/async
    [HttpPost("async")]
    public async Task<IActionResult> CreateAsync([FromBody] DocenteDto dto, CancellationToken ct)
    {
        // El DTO para crear no debe llevar ID
        dto.Id = 0;
        return await EnqueueTransaction("CREATE", dto, ct);
    }

    // PUT: api/docentes/async/{id}
    [HttpPut("async/{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] DocenteDto dto, CancellationToken ct)
    {
        dto.Id = id; // Aseguramos que el ID del DTO coincida con la ruta
        return await EnqueueTransaction("UPDATE", dto, ct);
    }

    // DELETE: api/docentes/async/{id}
    [HttpDelete("async/{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken ct)
    {
        // Para el delete, solo necesitamos el ID en el payload
        var dto = new DocenteDto { Id = id };
        return await EnqueueTransaction("DELETE", dto, ct);
    }
    #endregion

    #region Métodos Privados
    private async Task<IActionResult> EnqueueTransaction(string operation, object payload, CancellationToken ct)
    {
        var tx = new Transaccion
        {
            Id = Guid.NewGuid(),
            Entidad = "Docente",
            TipoOperacion = operation,
            Payload = JsonSerializer.Serialize(payload),
            Estado = "EN_COLA",
            NotBefore = DateTimeOffset.UtcNow
        };

        await _store.AddAsync(tx, ct);
        await _queue.EnqueueAsync(tx, "default", ct);

        return AcceptedAtAction(nameof(TransaccionesController.Get), "Transacciones", new { id = tx.Id }, new { transaccionId = tx.Id, estado = tx.Estado });
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
    #endregion
}