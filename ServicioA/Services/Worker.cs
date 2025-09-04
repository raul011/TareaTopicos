using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Dtos; // Necesario para DocenteDto y AulaDto
using TAREATOPICOS.ServicioA.Dtos.request;

namespace TAREATOPICOS.ServicioA.Services;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackgroundTaskQueue _queue;
    private readonly RedisTransaccionStore _store;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger,
                  IBackgroundTaskQueue queue,
                  RedisTransaccionStore store,
                  IServiceProvider serviceProvider)
    {
        _logger = logger;
        _queue = queue;
        _store = store;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado ");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var transaccion) && transaccion != null)
            {
                try
                {
                    await _store.UpdateEstadoAsync(transaccion.Id, "PROCESANDO");

                    await Task.Delay(2000, stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ServicioAContext>();

                        switch (transaccion.TipoOperacion.ToUpper())
                        {
                            // ==================== POST ====================
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
                                else if (transaccion.Entidad == "Docente" && transaccion.Payload != null)
                                {
                                    var docenteDto = JsonSerializer.Deserialize<DocenteDto>(transaccion.Payload.ToString()!);
                                    if (docenteDto != null)
                                    {
                                        var docente = new Docente { Registro = docenteDto.Registro, Ci = docenteDto.Ci, Nombre = docenteDto.Nombre, Telefono = docenteDto.Telefono, Estado = docenteDto.Estado };
                                        db.Docentes.Add(docente);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Docente creado → {Nombre}", docente.Nombre);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en POST para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                else if (transaccion.Entidad == "Aula" && transaccion.Payload != null)
                                {
                                    var aulaDto = JsonSerializer.Deserialize<AulaDto>(transaccion.Payload.ToString()!);
                                    if (aulaDto != null)
                                    {
                                        var aula = new Aula { Codigo = aulaDto.Codigo, Capacidad = aulaDto.Capacidad, Ubicacion = aulaDto.Ubicacion };
                                        db.Aulas.Add(aula);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Aula creada → {Codigo}", aula.Codigo);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en POST para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                else if (transaccion.Entidad == "Materia" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<MateriaRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new Materia { Codigo = dto.Codigo, Nombre = dto.Nombre, Creditos = dto.Creditos, NivelId = dto.NivelId };
                                        db.Materias.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Materia creada → {Nombre}", entity.Nombre);
                                    }
                                }
                                else if (transaccion.Entidad == "Estudiante" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<EstudianteRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new Estudiante
                                        {
                                            Registro = dto.Registro,
                                            Ci = dto.Ci,
                                            Nombre = dto.Nombre,
                                            Email = dto.Email,
                                            Telefono = dto.Telefono,
                                            Direccion = dto.Direccion,
                                            Estado = dto.Estado,
                                            CarreraId = dto.CarreraId,
                                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                                        };
                                        db.Estudiantes.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Estudiante creado → {Nombre}", entity.Nombre);
                                    }
                                }
                                else if (transaccion.Entidad == "Carrera" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<CarreraDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new Carrera { Nombre = dto.Nombre };
                                        db.Carreras.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("Carrera creada → {Nombre}", entity.Nombre);
                                    }
                                }
                                else if (transaccion.Entidad == "PlanDeEstudio" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<PlanDeEstudioDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new PlanDeEstudio { Nombre = dto.Nombre, Codigo = dto.Codigo, Fecha = dto.Fecha, Estado = dto.Estado, CarreraId = dto.CarreraId };
                                        db.PlanesEstudio.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("PlanDeEstudio creado → {Nombre}", entity.Nombre);
                                    }
                                }
                                else if (transaccion.Entidad == "PeriodoAcademico" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<PeriodoAcademicoRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new PeriodoAcademico { Gestion = dto.Gestion, FechaInicio = dto.FechaInicio, FechaFin = dto.FechaFin };
                                        db.PeriodosAcademicos.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("PeriodoAcademico creado → {Gestion}", entity.Gestion);
                                    }
                                }
                                else if (transaccion.Entidad == "GrupoMateria" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<GrupoMateriaRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var entity = new GrupoMateria
                                        {
                                            Grupo = dto.Grupo,
                                            Cupo = dto.Cupo,
                                            Estado = dto.Estado,
                                            MateriaId = dto.MateriaId,
                                            DocenteId = dto.DocenteId,
                                            PeriodoId = dto.PeriodoId,
                                            HorarioId = dto.HorarioId,
                                            AulaId = dto.AulaId
                                        };
                                        db.GruposMaterias.Add(entity);
                                        await db.SaveChangesAsync(stoppingToken);
                                        _logger.LogInformation("GrupoMateria creado → Grupo {Grupo} para MateriaId {MateriaId}", entity.Grupo, entity.MateriaId);
                                    }
                                }
                                break;

                            // ==================== PUT ====================
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
                                            _logger.LogWarning("Intento de actualizar Nivel {Id}, pero no existe ❌", nivel.Id);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en PUT para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                else if (transaccion.Entidad == "Docente" && transaccion.Payload != null)
                                {
                                    var docenteDto = JsonSerializer.Deserialize<DocenteDto>(transaccion.Payload.ToString()!);
                                    if (docenteDto != null)
                                    {
                                        var existente = await db.Docentes.FindAsync(new object[] { docenteDto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Registro = docenteDto.Registro;
                                            existente.Ci = docenteDto.Ci;
                                            existente.Nombre = docenteDto.Nombre;
                                            existente.Telefono = docenteDto.Telefono;
                                            existente.Estado = docenteDto.Estado;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Docente actualizado → {Id}", docenteDto.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Intento de actualizar Docente {Id}, pero no existe ❌", docenteDto.Id);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en PUT para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                else if (transaccion.Entidad == "Aula" && transaccion.Payload != null)
                                {
                                    var aulaDto = JsonSerializer.Deserialize<AulaDto>(transaccion.Payload.ToString()!);
                                    if (aulaDto != null)
                                    {
                                        var existente = await db.Aulas.FindAsync(new object[] { aulaDto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Codigo = aulaDto.Codigo;
                                            existente.Capacidad = aulaDto.Capacidad;
                                            existente.Ubicacion = aulaDto.Ubicacion;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Aula actualizada → {Id}", aulaDto.Id);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Intento de actualizar Aula {Id}, pero no existe ❌", aulaDto.Id);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Payload inválido en PUT para {Entidad}", transaccion.Entidad);
                                    }
                                }
                                else if (transaccion.Entidad == "Materia" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<MateriaRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.Materias.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Codigo = dto.Codigo;
                                            existente.Nombre = dto.Nombre;
                                            existente.Creditos = dto.Creditos;
                                            existente.NivelId = dto.NivelId;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Materia actualizada → {Id}", dto.Id);
                                        }
                                    }
                                }
                                else if (transaccion.Entidad == "Estudiante" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<EstudianteRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.Estudiantes.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Registro = dto.Registro; existente.Ci = dto.Ci; existente.Nombre = dto.Nombre; existente.Email = dto.Email;
                                            existente.Telefono = dto.Telefono; existente.Direccion = dto.Direccion; existente.Estado = dto.Estado; existente.CarreraId = dto.CarreraId;
                                            if (!string.IsNullOrEmpty(dto.Password)) { existente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password); }
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Estudiante actualizado → {Id}", dto.Id);
                                        }
                                    }
                                }
                                else if (transaccion.Entidad == "Carrera" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<CarreraDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.Carreras.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Nombre = dto.Nombre;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("Carrera actualizada → {Id}", dto.Id);
                                        }
                                    }
                                }
                                else if (transaccion.Entidad == "PlanDeEstudio" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<PlanDeEstudioDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.PlanesEstudio.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Nombre = dto.Nombre; existente.Codigo = dto.Codigo; existente.Fecha = dto.Fecha; existente.Estado = dto.Estado; existente.CarreraId = dto.CarreraId;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("PlanDeEstudio actualizado → {Id}", dto.Id);
                                        }
                                    }
                                }
                                else if (transaccion.Entidad == "PeriodoAcademico" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<PeriodoAcademicoRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.PeriodosAcademicos.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Gestion = dto.Gestion; existente.FechaInicio = dto.FechaInicio; existente.FechaFin = dto.FechaFin;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("PeriodoAcademico actualizado → {Id}", dto.Id);
                                        }
                                    }
                                }
                                else if (transaccion.Entidad == "GrupoMateria" && transaccion.Payload != null)
                                {
                                    var dto = JsonSerializer.Deserialize<GrupoMateriaRequestDto>(transaccion.Payload.ToString()!);
                                    if (dto != null)
                                    {
                                        var existente = await db.GruposMaterias.FindAsync(new object[] { dto.Id }, stoppingToken);
                                        if (existente != null)
                                        {
                                            existente.Grupo = dto.Grupo; existente.Cupo = dto.Cupo; existente.Estado = dto.Estado; existente.MateriaId = dto.MateriaId; existente.DocenteId = dto.DocenteId; existente.PeriodoId = dto.PeriodoId; existente.HorarioId = dto.HorarioId; existente.AulaId = dto.AulaId;
                                            await db.SaveChangesAsync(stoppingToken);
                                            _logger.LogInformation("GrupoMateria actualizado → {Id}", dto.Id);
                                        }
                                    }
                                }
                                break;

                            // ==================== DELETE ====================
                            case "DELETE":
                                if (transaccion.Entidad == "Nivel" && transaccion.Payload != null)
                                {
                                    try
                                    {
                                        // El payload puede ser solo { Id = x }
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                                        {
                                            var id = idProp.GetInt32();
                                            var existente = await db.Niveles.FindAsync(new object[] { id }, stoppingToken);
                                            if (existente != null)
                                            {
                                                db.Niveles.Remove(existente);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("Nivel {Id} eliminado correctamente ✅", id);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Intento de eliminar Nivel {Id}, pero no existe ❌", id);
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Payload de DELETE no contenía 'Id'");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error interpretando payload de DELETE para Nivel");
                                    }
                                }
                                else if (transaccion.Entidad == "Docente" && transaccion.Payload != null)
                                {
                                    try
                                    {
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                                        {
                                            var id = idProp.GetInt32();
                                            var existente = await db.Docentes.FindAsync(new object[] { id }, stoppingToken);
                                            if (existente != null)
                                            {
                                                db.Docentes.Remove(existente);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("Docente {Id} eliminado correctamente ✅", id);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Intento de eliminar Docente {Id}, pero no existe ❌", id);
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Payload de DELETE para Docente no contenía 'Id'");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error interpretando payload de DELETE para Docente");
                                    }
                                }
                                else if (transaccion.Entidad == "Aula" && transaccion.Payload != null)
                                {
                                    try
                                    {
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp))
                                        {
                                            var id = idProp.GetInt32();
                                            var existente = await db.Aulas.FindAsync(new object[] { id }, stoppingToken);
                                            if (existente != null)
                                            {
                                                db.Aulas.Remove(existente);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("Aula {Id} eliminada correctamente ✅", id);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Intento de eliminar Aula {Id}, pero no existe ❌", id);
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Payload de DELETE para Aula no contenía 'Id'");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error interpretando payload de DELETE");
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        var jsonDoc = JsonDocument.Parse(transaccion.Payload.ToString()!);
                                        if (jsonDoc.RootElement.TryGetProperty("Id", out var idProp) && transaccion.Entidad is not null)
                                        {
                                            var id = idProp.GetInt32();
                                            var entity = await db.FindAsync(Type.GetType($"TAREATOPICOS.ServicioA.Models.{transaccion.Entidad}")!, new object[] { id }, stoppingToken);
                                            if (entity != null)
                                            {
                                                db.Remove(entity);
                                                await db.SaveChangesAsync(stoppingToken);
                                                _logger.LogInformation("{Entidad} {Id} eliminado correctamente ✅", transaccion.Entidad, id);
                                            }
                                        }
                                    }
                                    catch (Exception ex) { _logger.LogWarning(ex, "Error genérico en DELETE para {Entidad}", transaccion.Entidad); }
                                }
                                break;

                            // ==================== GET (opcional) ====================
                            case "GET":
                                _logger.LogInformation("GET en cola aún no implementado para {Entidad}", transaccion.Entidad);
                                break;
                        }
                    }

                    // Siempre que no haya excepción real → marcar como COMPLETADO
                    await _store.UpdateEstadoAsync(transaccion.Id, "COMPLETADO");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando transacción {Id}", transaccion.Id);
                    await _store.UpdateEstadoAsync(transaccion.Id, "ERROR");
                }
            }
            else
            {
                await Task.Delay(500, stoppingToken); // espera medio segundo si no hay tareas
            }
        }
    }
}
