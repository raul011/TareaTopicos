// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;

// namespace TAREATOPICOS.ServicioA.Services;

// /// <summary>
// /// Orquesta pools por cola leyendo configuración ("Queues").
// /// Arranca al iniciar la app y detiene al apagar.
// /// </summary>
// public sealed class WorkerHost : IHostedService, IAsyncDisposable
// {
//     private readonly IServiceProvider _sp;
//     private readonly IConfiguration _cfg;
//     private readonly ILogger<WorkerHost> _logger;

//     private readonly Dictionary<string, WorkerPool> _pools = new();

//     public WorkerHost(IServiceProvider sp, IConfiguration cfg, ILogger<WorkerHost> logger)
//     {
//         _sp = sp;
//         _cfg = cfg;
//         _logger = logger;
//     }

//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         var queuesSection = _cfg.GetSection("Queues");
//         foreach (var q in queuesSection.GetChildren())
//         {
//             var name = q.GetValue<string>("Name") ?? "default";
//             var workers = Math.Max(1, q.GetValue<int?>("Workers") ?? 1);

//             var pool = new WorkerPool(name, CreateWorker, _sp.GetRequiredService<ILogger<WorkerPool>>());
//             pool.SetConcurrency(workers);
//             _pools[name] = pool;

//             _logger.LogInformation("WorkerHost: cola {Name} iniciada con {Workers} hilos", name, workers);
//         }

//         return Task.CompletedTask;
//     }

//     public async Task StopAsync(CancellationToken cancellationToken)
//     {
//         foreach (var pool in _pools.Values)
//             await pool.StopAsync();
//         _pools.Clear();
//     }

//     private WorkerService CreateWorker(string queueName)
//     {
//         // Cada worker obtiene sus deps desde el scope
//         var scope = _sp.CreateScope();
//         var sp = scope.ServiceProvider;

//         var qm      = sp.GetRequiredService<QueueManager>();
//         var store   = sp.GetRequiredService<ITransaccionStore>();
//         var proc    = sp.GetRequiredService<Processors.IQueueProcessor>();
//         var dlq     = sp.GetRequiredService<DeadLetterService>();
//         var limiter = sp.GetRequiredService<RateLimiter>();
//         var logger  = sp.GetRequiredService<ILogger<WorkerService>>();
//         var cb      = sp.GetRequiredService<CallbackService>();

//         // ✅ Nuevo: obtener QueueStateService
//         var queueState = sp.GetRequiredService<QueueStateService>();

//         // Lee backoff/retries de config (por cola si existe)
//         var qSection = _cfg.GetSection("Queues").GetChildren()
//                           .FirstOrDefault(s => (s.GetValue<string>("Name") ?? "default") == queueName);
//         var maxRetries = qSection?.GetValue<int?>("MaxRetries") ?? 5;
//         var baseBackoffMs = qSection?.GetValue<int?>("BaseBackoffMs") ?? 300;

//         // ✅ Nuevo: pasar queueState
//         return new WorkerService(queueName, qm, store, proc, dlq, limiter, logger, cb, queueState, maxRetries, baseBackoffMs);
//     }
//     private readonly object _lock = new();

//     public IReadOnlyDictionary<string, int> ListQueues()
//     {
//         lock (_lock)
//         {
//             return _pools.ToDictionary(p => p.Key, p => p.Value.Concurrency);
//         }
//     }

//     public bool AddQueue(string name, int workers)
//     {
//         lock (_lock)
//         {
//             if (_pools.ContainsKey(name))
//                 return false; // ya existe

//             var pool = new WorkerPool(name, CreateWorker, _sp.GetRequiredService<ILogger<WorkerPool>>());
//             pool.SetConcurrency(Math.Max(1, workers));
//             _pools[name] = pool;

//             _logger.LogInformation("WorkerHost: cola {Name} agregada con {Workers} workers", name, workers);
//             return true;
//         }
//     }

//     public bool ScaleQueue(string name, int workers)
//     {
//         lock (_lock)
//         {
//             if (!_pools.TryGetValue(name, out var pool))
//                 return false;

//             pool.SetConcurrency(Math.Max(0, workers));
//             return true;
//         }
//     }

//   public async Task<bool> RemoveQueueAsync(string name)
// {
//     WorkerPool? pool;
//     lock (_lock)
//     {
//         if (!_pools.TryGetValue(name, out pool))
//             return false; // ✅ corregido
//         _pools.Remove(name);
//     }

//     await pool!.StopAsync();
//     _logger.LogInformation("WorkerHost: cola {Name} eliminada", name);
//     return true;
// }

//     public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);
// }
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TAREATOPICOS.ServicioA.Services.Processors;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Orquesta pools por cola leyendo configuración ("Queues").
/// Arranca al iniciar la app y detiene al apagar.
/// Ahora soporta colas especializadas.
/// </summary>
public sealed class WorkerHost : IHostedService, IAsyncDisposable
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WorkerHost> _logger;

    private readonly Dictionary<string, WorkerPool> _pools = new();
    private readonly Dictionary<string, string> _queueProcessors = new(); // cola -> processor

    private readonly object _lock = new();

    public WorkerHost(IServiceProvider sp, IConfiguration cfg, ILogger<WorkerHost> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    // =======================
    // START/STOP
    // =======================
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var queuesSection = _cfg.GetSection("Queues");
        foreach (var q in queuesSection.GetChildren())
        {
            var name = q.GetValue<string>("Name") ?? "default";
            var workers = Math.Max(1, q.GetValue<int?>("Workers") ?? 1);

            // Por defecto usa DefaultProcessor
            _queueProcessors[name] = "DefaultProcessor";

            var pool = new WorkerPool(name, CreateWorker, _sp.GetRequiredService<ILogger<WorkerPool>>());
            pool.SetConcurrency(workers);
            _pools[name] = pool;

            _logger.LogInformation("WorkerHost: cola {Name} iniciada con {Workers} hilos (processor: DefaultProcessor)",
                name, workers);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var pool in _pools.Values)
            await pool.StopAsync();
        _pools.Clear();
        _queueProcessors.Clear();
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);

    // =======================
    // CREAR WORKER
    // =======================
    private WorkerService CreateWorker(string queueName)
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;

        var qm      = sp.GetRequiredService<QueueManager>();
        var store   = sp.GetRequiredService<ITransaccionStore>();
        var dlq     = sp.GetRequiredService<DeadLetterService>();
        var limiter = sp.GetRequiredService<RateLimiter>();
        var logger  = sp.GetRequiredService<ILogger<WorkerService>>();
        var cb      = sp.GetRequiredService<CallbackService>();
        var queueState = sp.GetRequiredService<QueueStateService>();

        // 👇 Buscar processor dinámico
        IQueueProcessor proc;
        if (_queueProcessors.TryGetValue(queueName, out var procName))
        {
            proc = procName switch
            {
                "NivelProcessor"   => sp.GetRequiredService<NivelProcessor>(),
                _                  => sp.GetRequiredService<DefaultProcessor>()
            };
        }
        else
        {
            proc = sp.GetRequiredService<DefaultProcessor>();
        }

        return new WorkerService(queueName, qm, store, proc, dlq, limiter, logger, cb, queueState);
    }

    // =======================
    // ADMINISTRAR COLAS
    // =======================
    public IReadOnlyDictionary<string, int> ListQueues()
    {
        lock (_lock)
        {
            return _pools.ToDictionary(p => p.Key, p => p.Value.Concurrency);
        }
    }

    public bool AddQueue(string name, int workers, string processorName = "DefaultProcessor")
    {
        lock (_lock)
        {
            if (_pools.ContainsKey(name))
                return false; // ya existe

            _queueProcessors[name] = processorName; // registrar processor

            var pool = new WorkerPool(name, CreateWorker, _sp.GetRequiredService<ILogger<WorkerPool>>());
            pool.SetConcurrency(Math.Max(1, workers));
            _pools[name] = pool;

            _logger.LogInformation("WorkerHost: cola {Name} agregada con {Workers} workers y processor {Processor}",
                name, workers, processorName);

            return true;
        }
    }

    public bool ScaleQueue(string name, int workers)
    {
        lock (_lock)
        {
            if (!_pools.TryGetValue(name, out var pool))
                return false;

            pool.SetConcurrency(Math.Max(0, workers));
            return true;
        }
    }

    public async Task<bool> RemoveQueueAsync(string name)
    {
        WorkerPool? pool;
        lock (_lock)
        {
            if (!_pools.TryGetValue(name, out pool))
                return false;
            _pools.Remove(name);
            _queueProcessors.Remove(name); // quitar también processor
        }

        await pool!.StopAsync();
        _logger.LogInformation("WorkerHost: cola {Name} eliminada", name);
        return true;
    }
}
