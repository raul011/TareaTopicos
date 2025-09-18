using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Orquesta pools por cola leyendo configuración ("Queues").
/// Arranca al iniciar la app y detiene al apagar.
/// </summary>
public sealed class WorkerHost : IHostedService, IAsyncDisposable
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly ILogger<WorkerHost> _logger;

    private readonly Dictionary<string, WorkerPool> _pools = new();

    public WorkerHost(IServiceProvider sp, IConfiguration cfg, ILogger<WorkerHost> logger)
    {
        _sp = sp;
        _cfg = cfg;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var queuesSection = _cfg.GetSection("Queues");
        foreach (var q in queuesSection.GetChildren())
        {
            var name = q.GetValue<string>("Name") ?? "default";
            var workers = Math.Max(1, q.GetValue<int?>("Workers") ?? 1);

            var pool = new WorkerPool(name, CreateWorker, _sp.GetRequiredService<ILogger<WorkerPool>>());
            pool.SetConcurrency(workers);
            _pools[name] = pool;

            _logger.LogInformation("WorkerHost: cola {Name} iniciada con {Workers} hilos", name, workers);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var pool in _pools.Values)
            await pool.StopAsync();
        _pools.Clear();
    }

    private WorkerService CreateWorker(string queueName)
    {
        // Usaremos el IServiceProvider principal para obtener el IServiceScopeFactory
        var sp = _sp;
        var qm = sp.GetRequiredService<QueueManager>();
        var store = sp.GetRequiredService<ITransaccionStore>();
        var dlq = sp.GetRequiredService<DeadLetterService>();
        var limiter = sp.GetRequiredService<RateLimiter>();
        var logger = sp.GetRequiredService<ILogger<WorkerService>>();
        var cb = sp.GetRequiredService<CallbackService>();

        // ✅ Nuevo: obtener QueueStateService
        var queueState = sp.GetRequiredService<QueueStateService>();

        // Lee backoff/retries de config (por cola si existe)
        var qSection = _cfg.GetSection("Queues").GetChildren()
                          .FirstOrDefault(s => (s.GetValue<string>("Name") ?? "default") == queueName);
        var maxRetries = qSection?.GetValue<int?>("MaxRetries") ?? 5;
        var baseBackoffMs = qSection?.GetValue<int?>("BaseBackoffMs") ?? 300;

        // ✅ Nuevo: obtener el factory para pasarlo al worker
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        // ✅ Nuevo: pasar queueState
        return new WorkerService(queueName, qm, store, dlq, limiter, logger, cb, queueState, scopeFactory, maxRetries, baseBackoffMs);
    }

    public async ValueTask DisposeAsync() => await StopAsync(CancellationToken.None);
}
