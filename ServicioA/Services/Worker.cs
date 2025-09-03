// tareatopicos.ServicioA/Services/Worker.cs
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Dtos.request;
using Microsoft.EntityFrameworkCore;

namespace TAREATOPICOS.ServicioA.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackgroundTaskQueue _queue;
    private readonly TransaccionStore _store;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger,
                  IBackgroundTaskQueue queue,
                  TransaccionStore store,
                  IServiceProvider serviceProvider)
    {
        _logger = logger; // para logs
        _queue = queue;  // las transacciones pendientes de la cola
        _store = store;  // registro el estado de cada transaccion
        _serviceProvider = serviceProvider; // crcea una caja para cada tarea
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado ");

        while (!stoppingToken.IsCancellationRequested)
        {   //saca la siguiente transaccion de la cola  tryDequeue
            if (_queue.TryDequeue(out var transaccion) && transaccion != null)
            {
                try
                {   // actualiza el estado de la cola
                    _store.UpdateEstado(transaccion.Id, "PROCESANDO");

                    await Task.Delay(2000, stoppingToken);//trabajo de 2 sg

                    using (var scope = _serviceProvider.CreateScope())
                    {   // crea la cajita para realizar la tarea
                        var db = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

                        switch (transaccion.TipoOperacion.ToUpper())
                        {
                            // POST 
                            case "POST":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    var nivel = JsonSerializer.Deserialize<Nivel>(transaccion.Payload.ToString()!);
                                    if (nivel != null)
                                    {
                                        db.Niveles.Add(nivel);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Nivel creado → {Nombre}", nivel.Nombre);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en POST para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                break;

                            //  PUT 
                            case "PUT":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    var nivel = JsonSerializer.Deserialize<Nivel>(transaccion.Payload.ToString()!);
                                    if (nivel != null)
                                    {
                                        var existente = await db.Niveles.FindAsync(new object[] { nivel.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Nombre = nivel.Nombre;
                                            existente.Numero = nivel.Numero;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Nivel actualizado → {Id}", nivel.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Intento de actualizar Nivel {Id}, pero no existe ", nivel.Id);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en PUT para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                break;

                            // ELIMINAR
                            case "DELETE":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    try
                                    {
                                         
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                                        {
                                            var id = idProp.GetInt32();
                                            var existente = await db.Niveles.FindAsync(new object[] { id }, stoppingToken);
                                            if (existente != null)
                                            {
                                                db.Niveles.Remove(existente);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("Nivel {Id} eliminado correctamente  ", id);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Intento de eliminar Nivel {Id}, pero no existe  ", id);
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Payload de DELETE no contenía 'Id'");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error interpretando payload de DELETE");
                                    }
                                }
                                break;

                             
                            case "GET":
                                _logger.LogInformation("GET en cola aún no implementado para {Entidad}", transaccion.Entidad);
                                break;
                            
                            
case "CREARINSCRIPCION":
{
    if (transaccion.Entidad == "Inscripcion" && transaccion.Payload != null)
    {
        var dto = JsonSerializer.Deserialize<CrearInscripcionAsyncDto>(transaccion.Payload)!;

         
        var existeEst = await db.Estudiantes.AnyAsync(e => e.Id == dto.EstudianteId, stoppingToken);
        var existePer = await db.PeriodosAcademicos.AnyAsync(p => p.Id == dto.PeriodoId, stoppingToken);
        if (!existeEst || !existePer)
            throw new InvalidOperationException("Estudiante o Período inválido.");

         
        var duplicada = await db.Inscripciones
            .AnyAsync(i => i.EstudianteId == dto.EstudianteId && i.PeriodoId == dto.PeriodoId, stoppingToken);
        if (duplicada)
            throw new InvalidOperationException("El estudiante ya tiene una inscripción en este período.");

        var entity = new Inscripcion
        {
            Fecha = DateTime.UtcNow,
            Estado = "PENDIENTE",
            EstudianteId = dto.EstudianteId,
            PeriodoId = dto.PeriodoId
        };
        db.Inscripciones.Add(entity);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Inscripción creada Id={Id} (Est={Est}, Per={Per})",
            entity.Id, entity.EstudianteId, entity.PeriodoId);
    }
    break;
}

case "AGREGARDETALLE":
{
    if (transaccion.Entidad == "Inscripcion" && transaccion.Payload != null)
    {
        var dto = JsonSerializer.Deserialize<AgregarDetalleAsyncDto>(transaccion.Payload)!;

        var insc = await db.Inscripciones
            .Include(i => i.Detalles)
            .FirstOrDefaultAsync(i => i.Id == dto.InscripcionId, stoppingToken);
        if (insc is null) throw new InvalidOperationException("Inscripción no existe.");

        var grupo = await db.GruposMaterias
            .Include(g => g.Materia)
            .Include(g => g.Horario)
            .FirstOrDefaultAsync(g => g.Id == dto.GrupoMateriaId, stoppingToken);
        if (grupo is null) throw new InvalidOperationException("GrupoMateria inválido.");
        if (grupo.PeriodoId != insc.PeriodoId)
            throw new InvalidOperationException("El grupo no pertenece al mismo período de la inscripción.");

         
        var yaEsta = await db.DetallesInscripciones
            .AnyAsync(d => d.InscripcionId == insc.Id && d.GrupoMateriaId == grupo.Id, stoppingToken);
        if (yaEsta) throw new InvalidOperationException("Ese grupo ya está en la inscripción.");

         
        var usados = await db.DetallesInscripciones
            .CountAsync(d => d.GrupoMateriaId == grupo.Id, stoppingToken);
        if (usados >= grupo.Cupo)
            throw new InvalidOperationException("Sin cupos disponibles en ese grupo.");

         
        if (grupo.Horario != null)
        {
            var gruposExistentesIds = insc.Detalles.Select(d => d.GrupoMateriaId).ToList();

            var horariosExistentes = await db.GruposMaterias
                .Include(gm => gm.Horario)
                .Where(gm => gruposExistentesIds.Contains(gm.Id) && gm.HorarioId != null)
                .Select(gm => gm.Horario!)
                .ToListAsync(stoppingToken);

            bool choca = horariosExistentes.Any(h =>
                h.Dia == grupo.Horario!.Dia &&
                grupo.Horario!.HoraInicio < h.HoraFin &&
                grupo.Horario!.HoraFin   > h.HoraInicio);

            if (choca) throw new InvalidOperationException("Choque de horarios con un grupo ya seleccionado.");
        }

        var detalle = new DetalleInscripcion
        {
            Codigo          = grupo.Grupo,
            Estado          = "INSCRITO",  
            InscripcionId   = insc.Id,
            GrupoMateriaId  = grupo.Id
        };
        db.DetallesInscripciones.Add(detalle);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Detalle agregado Insc={Insc} Grupo={Grupo}", insc.Id, grupo.Id);
    }
    break;
}


case "QUITARDETALLEPORID":
{
    if (transaccion.Entidad == "Inscripcion" && transaccion.Payload != null)
    {
        var dto = JsonSerializer.Deserialize<QuitarDetallePorIdAsyncDto>(transaccion.Payload)!;

        var det = await db.DetallesInscripciones
            .FirstOrDefaultAsync(d => d.Id == dto.DetalleId && d.InscripcionId == dto.InscripcionId, stoppingToken);
        if (det is null) throw new InvalidOperationException("Detalle no encontrado para esta inscripción.");

        db.DetallesInscripciones.Remove(det);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Detalle quitado DetalleId={Det} Insc={Insc}", dto.DetalleId, dto.InscripcionId);
    }
    break;
}

case "QUITARDETALLEPORGRUPO":
{
    if (transaccion.Entidad == "Inscripcion" && transaccion.Payload != null)
    {
        var dto = JsonSerializer.Deserialize<QuitarDetallePorGrupoAsyncDto>(transaccion.Payload)!;

        var det = await db.DetallesInscripciones
            .FirstOrDefaultAsync(d => d.InscripcionId == dto.InscripcionId && d.GrupoMateriaId == dto.GrupoMateriaId, stoppingToken);
        if (det is null) throw new InvalidOperationException("No existe ese grupo en la inscripción.");

        db.DetallesInscripciones.Remove(det);
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Detalle quitado por Grupo Insc={Insc} Grupo={Grupo}", dto.InscripcionId, dto.GrupoMateriaId);
    }
    break;
}

case "FINALIZARINSCRIPCION":
{
    if (transaccion.Entidad == "Inscripcion" && transaccion.Payload != null)
    {
        var dto = JsonSerializer.Deserialize<FinalizarInscripcionAsyncDto>(transaccion.Payload)!;

        var insc = await db.Inscripciones
            .Include(i => i.Detalles)
            .FirstOrDefaultAsync(i => i.Id == dto.InscripcionId, stoppingToken);
        if (insc is null) throw new InvalidOperationException("Inscripción no existe.");
        if (!insc.Detalles.Any()) throw new InvalidOperationException("No se puede finalizar sin materias.");

        insc.Estado = "FINALIZADA";
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Inscripción FINALIZADA Id={Id}", insc.Id);
    }
    break;
}














































































































                        }
                    }

                    
                    _store.UpdateEstado(transaccion.Id, "COMPLETADO");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando transacción {Id}", transaccion.Id);
                    _store.UpdateEstado(transaccion.Id, "ERROR");
                }
            }
            else
            {
                await Task.Delay(500, stoppingToken); // espera medio segundo si no hay tareas
            }
        }
    }
}
