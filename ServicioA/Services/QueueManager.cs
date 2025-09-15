using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Fachada de alto nivel para trabajar con la cola y el store de estados.
/// </summary>
public class QueueManager
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;

    public QueueManager(IBackgroundTaskQueue queue, ITransaccionStore store)
    {
        _queue = queue;
        _store = store;
    }

    /// <summary>Encola y guarda estado EN_COLA.</summary>
    public async Task EnqueueAsync(Transaccion tx, string? queueName = null, CancellationToken ct = default)
    {
        tx.Estado = "EN_COLA";
        await _store.AddAsync(tx, ct);
        await _queue.EnqueueAsync(tx, queueName, ct);
    }

    /// <summary>Intenta sacar una transacción para procesarla.</summary>
    public Task<Transaccion?> TryDequeueAsync(CancellationToken ct = default, string? queueName = null)
        => _queue.TryDequeueAsync(ct, queueName);
}
