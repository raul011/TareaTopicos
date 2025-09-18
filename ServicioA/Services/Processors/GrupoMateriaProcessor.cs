using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Dtos.request;
using TAREATOPICOS.ServicioA.Dtos;

namespace TAREATOPICOS.ServicioA.Services.Processors;

public sealed class GrupoMateriaProcessor : IProcessor, IQueueProcessor
{
    public string Entidad => "GrupoMateria";

    private readonly ServicioAContext _db;
    private readonly IIdempotencyGuard _guard;
    private readonly ILogger<GrupoMateriaProcessor> _logger;

    public GrupoMateriaProcessor(ServicioAContext db, IIdempotencyGuard guard, ILogger<GrupoMateriaProcessor> logger)
    {
        _db = db;
        _guard = guard;
        _logger = logger;
    }

    public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
    {
        _logger.LogInformation("🔄 Procesando Tx {TxId} tipo {Tipo} para GrupoMateria", tx.Id, tx.TipoOperacion);

        if (await _guard.IsProcessedAsync(tx.Id, ct))
        {
            _logger.LogInformation("🟡 Tx {TxId} ya fue procesada (idempotente)", tx.Id);
            tx.Estado = "COMPLETADO";
            return;
        }

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            switch (tx.TipoOperacion)
            {
                case "POST":
                    await ProcessPostAsync(tx, opts, ct);
                    break;
                case "PUT":
                    await ProcessPutAsync(tx, opts, ct);
                    break;
                case "DELETE":
                    await ProcessDeleteAsync(tx, opts, ct);
                    break;
                default:
                    Skip(tx, $"Tipo {tx.TipoOperacion} no soportado para GrupoMateria");
                    return;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (DbUpdateException ex) when (IsTransient(ex)) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Excepción inesperada en Tx {TxId}: {Msg}", tx.Id, ex.Message);
            throw new InvalidOperationException($"Error inesperado en GrupoMateriaProcessor: {ex.Message}", ex);
        }
    }

    private async Task ProcessPostAsync(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
    {
        var (dto, materia, docente, periodo, aula, horario) = await DeserializeAndValidateAsync(tx, opts, ct);
        if (dto is null) return; // Skip was called inside

        if (await _db.GruposMaterias.AnyAsync(g => g.Grupo == dto.Grupo, ct))
        {
            Skip(tx, $"GrupoMateria con grupo '{dto.Grupo}' ya existe");
            return;
        }
        if (materia is null || docente is null || periodo is null || aula is null || horario is null)
        {
            Skip(tx, "Relaciones incompletas en POST/GrupoMateria");
            return;
        }

        var entity = new GrupoMateria
        {
            Grupo = dto.Grupo,
            Cupo = dto.Cupo,
            Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado,
            MateriaId = materia.Id,
            DocenteId = docente.Id,
            PeriodoId = periodo.Id,
            AulaId = aula.Id,
            HorarioId = horario.Id
        };

        _db.GruposMaterias.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _guard.MarkProcessedAsync(tx.Id, ct);
        tx.Estado = "COMPLETADO";
    }

    private async Task ProcessPutAsync(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
    {
        var (dto, materia, docente, periodo, aula, horario) = await DeserializeAndValidateAsync(tx, opts, ct);
        if (dto is null) return; // Skip was called inside

        var existente = await _db.GruposMaterias.FirstOrDefaultAsync(g => g.Grupo == dto.Grupo, ct);
        if (existente is null)
        {
            Skip(tx, $"GrupoMateria con grupo '{dto.Grupo}' no existe");
            return;
        }
        if (materia is null || docente is null || periodo is null || aula is null || horario is null)
        {
            Skip(tx, "Relaciones incompletas en PUT/GrupoMateria");
            return;
        }

        existente.Cupo = dto.Cupo;
        existente.Estado = string.IsNullOrWhiteSpace(dto.Estado) ? "ACTIVO" : dto.Estado;
        existente.MateriaId = materia.Id;
        existente.DocenteId = docente.Id;
        existente.PeriodoId = periodo.Id;
        existente.AulaId = aula.Id;
        existente.HorarioId = horario.Id;

        await _db.SaveChangesAsync(ct);
        await _guard.MarkProcessedAsync(tx.Id, ct);
        tx.Estado = "COMPLETADO";
    }

    private async Task ProcessDeleteAsync(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
    {
        if (!TryGetGrupo(tx.Payload ?? "", out var grupo) || string.IsNullOrWhiteSpace(grupo))
        {
            Skip(tx, "DELETE/GrupoMateria requiere Grupo");
            return;
        }

        var entity = await _db.GruposMaterias.FirstOrDefaultAsync(g => g.Grupo == grupo, ct);
        if (entity is null)
        {
            Skip(tx, $"GrupoMateria con grupo '{grupo}' no existe");
            return;
        }

        try
        {
            _db.GruposMaterias.Remove(entity);
            await _db.SaveChangesAsync(ct);
            await _guard.MarkProcessedAsync(tx.Id, ct);
            tx.Estado = "COMPLETADO";
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message ?? ex.Message)
                    .Contains("foreign key", StringComparison.OrdinalIgnoreCase))
        {
            Skip(tx, $"No se puede eliminar GrupoMateria '{grupo}': tiene dependencias (FK).");
        }
    }

    private async Task<(GrupoMateriaCreateDto? Dto, Materia? Materia, Docente? Docente, PeriodoAcademico? Periodo, Aula? Aula, Horario? Horario)> DeserializeAndValidateAsync(Transaccion tx, JsonSerializerOptions opts, CancellationToken ct)
    {
        var dto = JsonSerializer.Deserialize<GrupoMateriaCreateDto>(tx.Payload ?? "", opts);
        if (dto is null || string.IsNullOrWhiteSpace(dto.Grupo))
        {
            Skip(tx, $"Payload inválido para {tx.TipoOperacion}/GrupoMateria");
            return (null, null, null, null, null, null);
        }

        var materia = await _db.Materias.AsNoTracking().FirstOrDefaultAsync(m => m.Codigo == dto.MateriaCodigo, ct);
        if (materia is null) _logger.LogWarning("Materia con código '{Codigo}' no encontrada para Tx {TxId}", dto.MateriaCodigo, tx.Id);

        var docente = await _db.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Registro == dto.DocenteRegistro, ct);
        if (docente is null) _logger.LogWarning("Docente con registro '{Registro}' no encontrado para Tx {TxId}", dto.DocenteRegistro, tx.Id);

        var periodo = await _db.PeriodosAcademicos.AsNoTracking().FirstOrDefaultAsync(p => p.Gestion == dto.PeriodoGestion, ct);
        if (periodo is null) _logger.LogWarning("Periodo con gestión '{Gestion}' no encontrado para Tx {TxId}", dto.PeriodoGestion, tx.Id);

        var aula = await _db.Aulas.AsNoTracking().FirstOrDefaultAsync(a => a.Codigo == dto.AulaCodigo, ct);
        if (aula is null) _logger.LogWarning("Aula con código '{Codigo}' no encontrada para Tx {TxId}", dto.AulaCodigo, tx.Id);

        var horario = await _db.Horarios.AsNoTracking().FirstOrDefaultAsync(h => h.Id == dto.HorarioId, ct);
        if (horario is null) _logger.LogWarning("Horario con Id '{Id}' no encontrado para Tx {TxId}", dto.HorarioId, tx.Id);

        return (dto, materia, docente, periodo, aula, horario);
    }

    private static bool TryGetGrupo(string payload, out string grupo)
    {
        grupo = string.Empty;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("Grupo", out var grupoProp) && grupoProp.ValueKind == JsonValueKind.String)
            {
                grupo = grupoProp.GetString() ?? string.Empty;
                return !string.IsNullOrEmpty(grupo);
            }
        }
        catch { }

        return false;
    }

    private void Skip(Transaccion tx, string motivo)
    {
        tx.Estado = "SKIP";
        _logger.LogWarning("⚠️ Tx {TxId} marcada como SKIP: {Motivo}", tx.Id, motivo);
    }

    private static bool IsTransient(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return msg.Contains("timeout") || msg.Contains("deadlock") ||
               msg.Contains("could not open connection") || msg.Contains("temporarily") ||
               msg.Contains("try again") || msg.Contains("connection is broken");
    }
}