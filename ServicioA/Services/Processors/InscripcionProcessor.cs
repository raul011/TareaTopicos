 
// // using System.Text.Json;
// // using Microsoft.EntityFrameworkCore;
// // using TAREATOPICOS.ServicioA.Data;
// // using TAREATOPICOS.ServicioA.Models;

// // namespace TAREATOPICOS.ServicioA.Services.Processors;

// // public sealed class InscripcionProcessor : IProcessor
// // {
// //     private readonly ServicioAContext _db;
// //     private readonly ILogger<InscripcionProcessor> _log;

// //     public InscripcionProcessor(ServicioAContext db, ILogger<InscripcionProcessor> log)
// //     {
// //         _db = db;
// //         _log = log;
// //     }

// //     // === Payload DTO ===
// //     private sealed record PayloadInscripcion(
// //         string Registro,
// //         int PeriodoId,
// //         List<MateriaGrupo> Materias,
// //         int InscripcionId
// //     );

// //     private sealed record MateriaGrupo(string MateriaCodigo, string Grupo);

// //     // === Procesamiento principal ===
// //     public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
// //     {
// //         _log.LogInformation("📥 Procesando Tx {TxId} ({Entidad})", tx.Id, tx.Entidad);

// //         if (string.IsNullOrWhiteSpace(tx.Payload))
// //         {
// //             Skip(tx, "Payload vacío o nulo");
// //             return;
// //         }

// //         PayloadInscripcion? payload;
// //         try
// //         {
// //             payload = JsonSerializer.Deserialize<PayloadInscripcion>(tx.Payload);
// //         }
// //         catch (Exception ex)
// //         {
// //             Skip(tx, $"Payload inválido: {ex.Message}");
// //             return;
// //         }

// //         if (payload == null)
// //         {
// //             Skip(tx, "Payload nulo o sin formato válido");
// //             return;
// //         }

// //         await using var dbTx = await _db.Database.BeginTransactionAsync(ct);

// //         try
// //         {
// //             // 🔎 Buscar inscripción base (ya creada como PENDIENTE)
// //             var inscripcion = await _db.Inscripciones
// //                 .Include(i => i.Detalles)
// //                 .FirstOrDefaultAsync(i => i.Id == payload.InscripcionId, ct);

// //             if (inscripcion == null)
// //             {
// //                 Skip(tx, $"Inscripción base {payload.InscripcionId} no encontrada");
// //                 await dbTx.RollbackAsync(ct);
// //                 return;
// //             }

// //             var estudiante = await _db.Estudiantes
// //                 .FirstOrDefaultAsync(e => e.Registro == payload.Registro, ct);

// //             if (estudiante == null)
// //             {
// //                 _log.LogWarning("⚠️ Estudiante {Registro} no encontrado", payload.Registro);
// //                 inscripcion.Estado = "RECHAZADA";
// //                 await _db.SaveChangesAsync(ct);
// //                 await dbTx.CommitAsync(ct);
// //                 return;
// //             }

// //             int confirmadas = 0;
// //             int total = payload.Materias.Count;

// //             foreach (var m in payload.Materias)
// //             {
// //                 var grupo = await _db.GruposMaterias
// //                     .Include(g => g.Materia)
// //                     .FirstOrDefaultAsync(g =>
// //                         g.Materia.Codigo == m.MateriaCodigo &&
// //                         g.Grupo == m.Grupo &&
// //                         g.PeriodoId == payload.PeriodoId, ct);

// //                 if (grupo == null)
// //                 {
// //                     _log.LogWarning("⚠️ Grupo no encontrado para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
// //                     continue;
// //                 }

// //                 // ✅ Verificar si el estudiante ya está inscrito en ese grupo
// //                 bool yaExiste = await _db.DetallesInscripciones
// //                     .Include(d => d.Inscripcion)
// //                     .AnyAsync(d =>
// //                         d.Inscripcion!.EstudianteId == estudiante.Id &&
// //                         d.GrupoMateriaId == grupo.Id, ct);

// //                 if (yaExiste)
// //                 {
// //                     _log.LogWarning("⏩ Estudiante {Registro} ya inscrito en {MateriaCodigo}-{Grupo}, se omite",
// //                         payload.Registro, m.MateriaCodigo, m.Grupo);
// //                     continue;
// //                 }

// //                // Verificar si hay cupos
// // if (grupo.Cupo <= 0)
// // {
// //     _log.LogWarning("❌ Sin cupos para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
// //     continue;
// // }

// // // 🕓 Verificar choques de horario (según tu modelo actual)
// // bool hayChoque = false;

// // if (grupo.HorarioId.HasValue)
// // {
// //     var horarioNuevo = await _db.Horarios
// //         .AsNoTracking()
// //         .FirstOrDefaultAsync(h => h.Id == grupo.HorarioId.Value, ct);

// //     if (horarioNuevo != null)
// //     {
// //         // Traer todos los horarios actuales del estudiante en este mismo periodo
// //         var horariosExistentes = await _db.DetallesInscripciones
// //             .Include(d => d.GrupoMateria)
// //                 .ThenInclude(gm => gm.Horario)
// //             .Where(d => d.Inscripcion!.EstudianteId == estudiante.Id &&
// //                         d.GrupoMateria!.PeriodoId == payload.PeriodoId &&
// //                         d.GrupoMateria.Horario != null)
// //             .Select(d => d.GrupoMateria!.Horario!)
// //             .ToListAsync(ct);

// //         hayChoque = horariosExistentes.Any(h =>
// //             h.Dia == horarioNuevo.Dia &&
// //             h.HoraInicio < horarioNuevo.HoraFin &&
// //             horarioNuevo.HoraInicio < h.HoraFin
// //         );
// //     }
// // }

// // if (hayChoque)
// // {
// //     _log.LogWarning("⛔ Choque de horario para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
// //     tx.MensajeError = $"Choque de horario con {m.MateriaCodigo}-{m.Grupo}";
// //     continue; // ❗ No se inscribe ni descuenta cupo
// // }

// // // Crear nuevo detalle de inscripción
// // var detalle = new DetalleInscripcion
// // {
// //     Codigo = $"{m.MateriaCodigo}-{m.Grupo}-{inscripcion.Id}",
// //     Estado = "INSCRITO",
// //     InscripcionId = inscripcion.Id,
// //     GrupoMateriaId = grupo.Id
// // };
// // _db.DetallesInscripciones.Add(detalle);
// // confirmadas++;

// // grupo.Cupo -= 1;
// // _db.GruposMaterias.Update(grupo);

// // _log.LogInformation("📉 Cupo actualizado {MateriaCodigo}-{Grupo}: nuevo cupo={NuevoCupo}",
// //     m.MateriaCodigo, m.Grupo, grupo.Cupo);
// //             }

// //             // 🟢 Actualizar estado final
// //             inscripcion.Estado = confirmadas switch
// //             {
// //                 0 => "RECHAZADA",
// //                 var c when c < total => "PARCIAL",
// //                 _ => "CONFIRMADA"
// //             };

// //             inscripcion.Fecha = DateTime.UtcNow; // Actualizamos la fecha también

// //             await _db.SaveChangesAsync(ct);
// //             await dbTx.CommitAsync(ct);

// //             // 🔁 Actualizar estado de la transacción
// //             tx.Estado = inscripcion.Estado switch
// //             {
// //                 "CONFIRMADA" => "OK",
// //                 "PARCIAL" => "OK_PARTIAL",
// //                 "RECHAZADA" => "REJECTED",
// //                 _ => "COMPLETADO"
// //             };

// //             // 💾 Guardar estado final
// //             _db.Transacciones.Update(tx);
// //             await _db.SaveChangesAsync(ct);

// //             _log.LogInformation("✅ Tx {TxId} → Inscripción {Id} {Estado} ({Confirmadas}/{Total})",
// //                 tx.Id, inscripcion.Id, inscripcion.Estado, confirmadas, total);
// //         }
// //         catch (Exception ex)
// //         {
// //             await dbTx.RollbackAsync(ct);
// //             tx.Estado = "ERROR";
// //             _log.LogError(ex, "💥 Error procesando Tx {TxId}", tx.Id);
// //         }
// //     }

// //     private void Skip(Transaccion tx, string motivo)
// //     {
// //         tx.Estado = "SKIP";
// //         _log.LogWarning("⚠️ Tx {TxId} omitida: {Motivo}", tx.Id, motivo);
// //     }
// // }
// using System.Text.Json;
// using Microsoft.EntityFrameworkCore;
// using TAREATOPICOS.ServicioA.Data;
// using TAREATOPICOS.ServicioA.Models;

// namespace TAREATOPICOS.ServicioA.Services.Processors;

// public sealed class InscripcionProcessor : IProcessor
// {
//     private readonly ServicioAContext _db;
//     private readonly ILogger<InscripcionProcessor> _log;

//     public InscripcionProcessor(ServicioAContext db, ILogger<InscripcionProcessor> log)
//     {
//         _db = db;
//         _log = log;
//     }

//     // === Payload DTO ===
//     private sealed record PayloadInscripcion(
//         string Registro,
//         int PeriodoId,
//         List<MateriaGrupo> Materias,
//         int InscripcionId
//     );

//     private sealed record MateriaGrupo(string MateriaCodigo, string Grupo);

//     // === Procesamiento principal ===
//     public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
//     {
//         _log.LogInformation("📥 Procesando Tx {TxId} ({Entidad})", tx.Id, tx.Entidad);

//         if (string.IsNullOrWhiteSpace(tx.Payload))
//         {
//             Skip(tx, "Payload vacío o nulo");
//             return;
//         }

//         PayloadInscripcion? payload;
//         try
//         {
//             payload = JsonSerializer.Deserialize<PayloadInscripcion>(tx.Payload);
//         }
//         catch (Exception ex)
//         {
//             Skip(tx, $"Payload inválido: {ex.Message}");
//             return;
//         }

//         if (payload == null)
//         {
//             Skip(tx, "Payload nulo o sin formato válido");
//             return;
//         }

//         await using var dbTx = await _db.Database.BeginTransactionAsync(ct);

//         try
//         {
//             // 🔎 Buscar inscripción base (ya creada como PENDIENTE)
//             var inscripcion = await _db.Inscripciones
//                 .Include(i => i.Detalles)
//                 .FirstOrDefaultAsync(i => i.Id == payload.InscripcionId, ct);

//             if (inscripcion == null)
//             {
//                 Skip(tx, $"Inscripción base {payload.InscripcionId} no encontrada");
//                 await dbTx.RollbackAsync(ct);
//                 return;
//             }

//             var estudiante = await _db.Estudiantes
//                 .FirstOrDefaultAsync(e => e.Registro == payload.Registro, ct);

//             if (estudiante == null)
//             {
//                 _log.LogWarning("⚠️ Estudiante {Registro} no encontrado", payload.Registro);
//                 inscripcion.Estado = "RECHAZADA";
//                 await _db.SaveChangesAsync(ct);
//                 await dbTx.CommitAsync(ct);
//                 return;
//             }

//             int confirmadas = 0;
//             int total = payload.Materias.Count;

//             foreach (var m in payload.Materias)
//             {
//                 var grupo = await _db.GruposMaterias
//                     .Include(g => g.Materia)
//                     .FirstOrDefaultAsync(g =>
//                         g.Materia.Codigo == m.MateriaCodigo &&
//                         g.Grupo == m.Grupo &&
//                         g.PeriodoId == payload.PeriodoId, ct);

//                 if (grupo == null)
//                 {
//                     var msg = $"Grupo no encontrado para {m.MateriaCodigo}-{m.Grupo}";
//                     _log.LogWarning("⚠️ {Msg}", msg);
//                     tx.MensajeError = msg;
//                     continue;
//                 }

//                 // ✅ Verificar si el estudiante ya está inscrito en ese grupo
//                 bool yaExiste = await _db.DetallesInscripciones
//                     .Include(d => d.Inscripcion)
//                     .AnyAsync(d =>
//                         d.Inscripcion!.EstudianteId == estudiante.Id &&
//                         d.GrupoMateriaId == grupo.Id, ct);

//                 if (yaExiste)
//                 {
//                     var msg = $"Estudiante ya inscrito en {m.MateriaCodigo}-{m.Grupo}";
//                     _log.LogWarning("⏩ {Msg}", msg);
//                     tx.MensajeError = msg;
//                     continue;
//                 }

//                 // ❌ Verificar si hay cupos
//                 if (grupo.Cupo <= 0)
//                 {
//                     var msg = $"Sin cupos disponibles para {m.MateriaCodigo}-{m.Grupo}";
//                     _log.LogWarning("❌ {Msg}", msg);
//                     tx.MensajeError = msg;
//                     continue;
//                 }

//                 // 🕓 Verificar choques de horario considerando días múltiples
// bool hayChoque = false;

// if (grupo.HorarioId.HasValue)
// {
//     var horarioNuevo = await _db.Horarios
//         .AsNoTracking()
//         .FirstOrDefaultAsync(h => h.Id == grupo.HorarioId.Value, ct);

//     if (horarioNuevo != null)
//     {
//         var diasNuevo = horarioNuevo.Dia.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

//         // Traer horarios existentes del estudiante
//         var horariosExistentes = await _db.DetallesInscripciones
//             .Include(d => d.GrupoMateria)
//                 .ThenInclude(gm => gm.Horario)
//             .Where(d => d.Inscripcion!.EstudianteId == estudiante.Id &&
//                         d.GrupoMateria!.PeriodoId == payload.PeriodoId &&
//                         d.GrupoMateria.Horario != null)
//             .Select(d => d.GrupoMateria!.Horario!)
//             .ToListAsync(ct);

//         foreach (var hExist in horariosExistentes)
//         {
//             var diasExist = hExist.Dia.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

//             // Si comparten algún día y se cruzan en hora → hay choque
//             if (diasNuevo.Any(dn => diasExist.Contains(dn)) &&
//                 hExist.HoraInicio < horarioNuevo.HoraFin &&
//                 horarioNuevo.HoraInicio < hExist.HoraFin)
//             {
//                 hayChoque = true;
//                 _log.LogWarning("⛔ Choque detectado: {MateriaCodigo}-{Grupo} se cruza con horario existente ({Dias} {Inicio}-{Fin})",
//                     m.MateriaCodigo, m.Grupo, hExist.Dia, hExist.HoraInicio, hExist.HoraFin);
//                 break;
//             }
//         }
//     }
// }


//                 if (hayChoque)
//                 {
//                     var msg = $"Choque de horario con {m.MateriaCodigo}-{m.Grupo}";
//                     _log.LogWarning("⛔ {Msg}", msg);
//                     tx.MensajeError = msg;
//                     continue; // ❗ No se inscribe ni descuenta cupo
//                 }

//                 // Crear nuevo detalle de inscripción
//                 var detalle = new DetalleInscripcion
//                 {
//                     Codigo = $"{m.MateriaCodigo}-{m.Grupo}-{inscripcion.Id}",
//                     Estado = "INSCRITO",
//                     InscripcionId = inscripcion.Id,
//                     GrupoMateriaId = grupo.Id
//                 };

//                 _db.DetallesInscripciones.Add(detalle);
//                 confirmadas++;

//                 grupo.Cupo -= 1;
//                 _db.GruposMaterias.Update(grupo);

//                 _log.LogInformation("📉 Cupo actualizado {MateriaCodigo}-{Grupo}: nuevo cupo={NuevoCupo}",
//                     m.MateriaCodigo, m.Grupo, grupo.Cupo);
//             }

//             // 🟢 Actualizar estado final
//             inscripcion.Estado = confirmadas switch
//             {
//                 0 => "RECHAZADA",
//                 var c when c < total => "PARCIAL",
//                 _ => "CONFIRMADA"
//             };

//             inscripcion.Fecha = DateTime.UtcNow; // Actualizamos la fecha también

//             await _db.SaveChangesAsync(ct);
//             await dbTx.CommitAsync(ct);

//             // 🔁 Actualizar estado de la transacción
//             tx.Estado = inscripcion.Estado switch
//             {
//                 "CONFIRMADA" => "OK",
//                 "PARCIAL" => "OK_PARTIAL",
//                 "RECHAZADA" => "REJECTED",
//                 _ => "COMPLETADO"
//             };

//             // 💾 Guardar estado final
//             _db.Transacciones.Update(tx);
//             await _db.SaveChangesAsync(ct);

//             _log.LogInformation("✅ Tx {TxId} → Inscripción {Id} {Estado} ({Confirmadas}/{Total})",
//                 tx.Id, inscripcion.Id, inscripcion.Estado, confirmadas, total);
//         }
//         catch (Exception ex)
//         {
//             await dbTx.RollbackAsync(ct);
//             tx.Estado = "ERROR";
//             tx.MensajeError = $"Error interno: {ex.Message}";
//             _log.LogError(ex, "💥 Error procesando Tx {TxId}", tx.Id);
//         }
//     }

//     private void Skip(Transaccion tx, string motivo)
//     {
//         tx.Estado = "SKIP";
//         tx.MensajeError = motivo;
//         _log.LogWarning("⚠️ Tx {TxId} omitida: {Motivo}", tx.Id, motivo);
//     }
// }
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors;

public sealed class InscripcionProcessor : IProcessor
{
    private readonly ServicioAContext _db;
    private readonly ILogger<InscripcionProcessor> _log;

    public InscripcionProcessor(ServicioAContext db, ILogger<InscripcionProcessor> log)
    {
        _db = db;
        _log = log;
    }

    private sealed record PayloadInscripcion(
        string Registro,
        int PeriodoId,
        List<MateriaGrupo> Materias,
        int InscripcionId
    );

    private sealed record MateriaGrupo(string MateriaCodigo, string Grupo);

    public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
    {
        _log.LogInformation("📥 Procesando Tx {TxId} ({Entidad})", tx.Id, tx.Entidad);

        if (string.IsNullOrWhiteSpace(tx.Payload))
        {
            Skip(tx, "Payload vacío o nulo");
            return;
        }

        PayloadInscripcion? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PayloadInscripcion>(tx.Payload);
        }
        catch (Exception ex)
        {
            Skip(tx, $"Payload inválido: {ex.Message}");
            return;
        }

        if (payload == null)
        {
            Skip(tx, "Payload nulo o sin formato válido");
            return;
        }

        await using var dbTx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var inscripcion = await _db.Inscripciones
                .Include(i => i.Detalles)
                .FirstOrDefaultAsync(i => i.Id == payload.InscripcionId, ct);

            if (inscripcion == null)
            {
                Skip(tx, $"Inscripción base {payload.InscripcionId} no encontrada");
                await dbTx.RollbackAsync(ct);
                return;
            }

            var estudiante = await _db.Estudiantes
                .FirstOrDefaultAsync(e => e.Registro == payload.Registro, ct);

            if (estudiante == null)
            {
                _log.LogWarning("⚠️ Estudiante {Registro} no encontrado", payload.Registro);
                inscripcion.Estado = "RECHAZADA";
                await _db.SaveChangesAsync(ct);
                await dbTx.CommitAsync(ct);
                return;
            }

            int confirmadas = 0;
            int total = payload.Materias.Count;

            // 📚 Lista temporal de horarios en esta misma transacción
            var horariosTemporal = new List<Horario>();

            foreach (var m in payload.Materias)
            {
                var grupo = await _db.GruposMaterias
                    .Include(g => g.Materia)
                    .FirstOrDefaultAsync(g =>
                        g.Materia.Codigo == m.MateriaCodigo &&
                        g.Grupo == m.Grupo &&
                        g.PeriodoId == payload.PeriodoId, ct);

                if (grupo == null)
                {
                    _log.LogWarning("⚠️ Grupo no encontrado para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    tx.MensajeError = $"Grupo no encontrado para {m.MateriaCodigo}-{m.Grupo}";
                    continue;
                }

                bool yaExiste = await _db.DetallesInscripciones
                    .Include(d => d.Inscripcion)
                    .AnyAsync(d =>
                        d.Inscripcion!.EstudianteId == estudiante.Id &&
                        d.GrupoMateriaId == grupo.Id, ct);

                if (yaExiste)
                {
                    _log.LogWarning("⏩ Estudiante {Registro} ya inscrito en {MateriaCodigo}-{Grupo}, se omite",
                        payload.Registro, m.MateriaCodigo, m.Grupo);
                    tx.MensajeError = $"Estudiante ya inscrito en {m.MateriaCodigo}-{m.Grupo}";
                    continue;
                }

                if (grupo.Cupo <= 0)
                {
                    _log.LogWarning("❌ Sin cupos para {MateriaCodigo}-{Grupo}", m.MateriaCodigo, m.Grupo);
                    tx.MensajeError = $"Sin cupos para {m.MateriaCodigo}-{m.Grupo}";
                    continue;
                }

                // 🕓 Verificar choques de horario
                bool hayChoque = false;

                if (grupo.HorarioId.HasValue)
                {
                    var horarioNuevo = await _db.Horarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(h => h.Id == grupo.HorarioId.Value, ct);

                    if (horarioNuevo != null)
                    {
                        var separadores = new[] { '-', ',', ';', ' ' };
                        var diasNuevo = horarioNuevo.Dia
                            .Split(separadores, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        // Traer horarios ya existentes del estudiante
                        var horariosExistentes = await _db.DetallesInscripciones
                            .Include(d => d.GrupoMateria)
                                .ThenInclude(gm => gm.Horario)
                            .Where(d => d.Inscripcion!.EstudianteId == estudiante.Id &&
                                        d.GrupoMateria!.PeriodoId == payload.PeriodoId &&
                                        d.GrupoMateria.Horario != null)
                            .Select(d => d.GrupoMateria!.Horario!)
                            .ToListAsync(ct);

                        // Combinar con los horarios de materias ya aceptadas en esta misma transacción
                        var todosHorarios = horariosExistentes.Concat(horariosTemporal).ToList();

                        foreach (var hExist in todosHorarios)
                        {
                            var diasExist = hExist.Dia
                                .Split(separadores, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            if (diasNuevo.Any(dn => diasExist.Contains(dn)) &&
                                hExist.HoraInicio < horarioNuevo.HoraFin &&
                                horarioNuevo.HoraInicio < hExist.HoraFin)
                            {
                                hayChoque = true;
                                _log.LogWarning("⛔ Choque detectado: {MateriaCodigo}-{Grupo} se cruza con horario existente ({Dias} {Inicio}-{Fin})",
                                    m.MateriaCodigo, m.Grupo, hExist.Dia, hExist.HoraInicio, hExist.HoraFin);
                                break;
                            }
                        }
                    }
                }

                if (hayChoque)
                {
                    tx.MensajeError = $"Choque de horario con {m.MateriaCodigo}-{m.Grupo}";
                    continue;
                }

                // Crear nuevo detalle de inscripción
                var detalle = new DetalleInscripcion
                {
                    Codigo = $"{m.MateriaCodigo}-{m.Grupo}-{inscripcion.Id}",
                    Estado = "INSCRITO",
                    InscripcionId = inscripcion.Id,
                    GrupoMateriaId = grupo.Id
                };

                _db.DetallesInscripciones.Add(detalle);
                confirmadas++;

                grupo.Cupo -= 1;
                _db.GruposMaterias.Update(grupo);

                // Agregar el horario al registro temporal
                if (grupo.HorarioId.HasValue)
                {
                    var h = await _db.Horarios.FindAsync(new object[] { grupo.HorarioId.Value }, ct);
                    if (h != null) horariosTemporal.Add(h);
                }

                _log.LogInformation("📉 Cupo actualizado {MateriaCodigo}-{Grupo}: nuevo cupo={NuevoCupo}",
                    m.MateriaCodigo, m.Grupo, grupo.Cupo);
            }

            // 🟢 Actualizar estado final
            inscripcion.Estado = confirmadas switch
            {
                0 => "RECHAZADA",
                var c when c < total => "PARCIAL",
                _ => "CONFIRMADA"
            };

            inscripcion.Fecha = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);

            tx.Estado = inscripcion.Estado switch
            {
                "CONFIRMADA" => "OK",
                "PARCIAL" => "OK_PARTIAL",
                "RECHAZADA" => "REJECTED",
                _ => "COMPLETADO"
            };

            _db.Transacciones.Update(tx);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("🧾 Resultado final → {Confirmadas}/{Total} confirmadas → Estado={Estado}",
                confirmadas, total, inscripcion.Estado);

            _log.LogInformation("✅ Tx {TxId} → Inscripción {Id} {Estado} ({Confirmadas}/{Total})",
                tx.Id, inscripcion.Id, inscripcion.Estado, confirmadas, total);
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync(ct);
            tx.Estado = "ERROR";
            tx.MensajeError = $"Error interno: {ex.Message}";
            _log.LogError(ex, "💥 Error procesando Tx {TxId}", tx.Id);
        }
    }

    private void Skip(Transaccion tx, string motivo)
    {
        tx.Estado = "SKIP";
        tx.MensajeError = motivo;
        _log.LogWarning("⚠️ Tx {TxId} omitida: {Motivo}", tx.Id, motivo);
    }
}
