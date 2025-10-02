// using TAREATOPICOS.ServicioA.Models;

// namespace TAREATOPICOS.ServicioA.Services;

// /// <summary>
// /// Fachada de alto nivel para trabajar con la cola y el store de estados.
// /// </summary>
// public class QueueManager
// {
//     private readonly IBackgroundTaskQueue _queue;
//     private readonly ITransaccionStore _store;

//     public QueueManager(IBackgroundTaskQueue queue, ITransaccionStore store)
//     {
//         _queue = queue;
//         _store = store;
//     }

//     /// <summary>Encola y guarda estado EN_COLA.</summary>
//     public async Task EnqueueAsync(Transaccion tx, string? queueName = null, CancellationToken ct = default)
//     {
//         tx.Estado = "EN_COLA";
//         await _store.AddAsync(tx, ct);
//         await _queue.EnqueueAsync(tx, queueName, ct);
//     }

//     /// <summary>Intenta sacar una transacción para procesarla.</summary>
//     public Task<Transaccion?> TryDequeueAsync(CancellationToken ct = default, string? queueName = null)
//         => _queue.TryDequeueAsync(ct, queueName);
// }
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

/// <summary>
/// Fachada de alto nivel para trabajar con la cola y el store de estados.
/// Ahora incluye balanceo automático entre colas (Round Robin).
/// </summary>
public class QueueManager
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ITransaccionStore _store;
    private readonly WorkerHost _workerHost;

    // Para round robin
    private static readonly ConcurrentDictionary<string, int> _rrIndexes = new();

    public QueueManager(IBackgroundTaskQueue queue, ITransaccionStore store, WorkerHost workerHost)
    {
        _queue = queue;
        _store = store;
         _workerHost = workerHost;
    }

    /// <summary>
    /// Encola en una cola específica (modo clásico).
    /// </summary>
    // public async Task EnqueueAsync(
    //     Transaccion tx,
    //     string? queueName = null,
    //     CancellationToken ct = default)
    // {
    //     tx.Estado = "EN_COLA";
    //     await _store.AddAsync(tx, ct);
    //     await _queue.EnqueueAsync(tx, queueName, ct);
    // }

    /// <summary>
    /// Encola balanceando automáticamente entre N colas usando round robin.
    /// </summary>
    // public async Task EnqueueBalancedAsync(
    //     Transaccion tx,
    //     IEnumerable<string> availableQueues,
    //     CancellationToken ct = default)
    // {
    //     var queues = availableQueues?.ToList() ?? new List<string>();
    //     if (queues.Count == 0)
    //         throw new InvalidOperationException("No hay colas disponibles para balancear.");

    //     // Round robin por clave estática
    //     var key = string.Join("|", queues.OrderBy(x => x));
    //     var index = _rrIndexes.AddOrUpdate(key, 0, (_, old) => (old + 1) % queues.Count);
    //     var chosenQueue = queues[index];

    //     tx.Estado = "EN_COLA";
    //     await _store.AddAsync(tx, ct);
    //     await _queue.EnqueueAsync(tx, chosenQueue, ct);
    // }
public async Task EnqueueAsync(
    Transaccion tx,
    string? queueName = null,
    CancellationToken ct = default)
{
    tx.Estado = "EN_COLA";
    await _store.AddAsync(tx, ct);

    if (string.IsNullOrWhiteSpace(queueName) || queueName == "balanced")
    {
        //  Round robin balanceado
        var queues = _workerHost.ListQueues().Keys.ToList();
        if (queues.Count == 0)
            throw new InvalidOperationException("No hay colas disponibles para balancear.");

        var key = string.Join("|", queues.OrderBy(x => x));
        var index = _rrIndexes.AddOrUpdate(key, 0, (_, old) => (old + 1) % queues.Count);
        var chosenQueue = queues[index];

        await _queue.EnqueueAsync(tx, chosenQueue, ct);
    }
    else
    {
        //  Encola directo en la cola indicada
        await _queue.EnqueueAsync(tx, queueName, ct);
    }
}

    /// <summary>
    /// Intenta sacar una transacción para procesarla.
    /// </summary>
    public Task<Transaccion?> TryDequeueAsync(
        CancellationToken ct = default,
        string? queueName = null)
        => _queue.TryDequeueAsync(ct, queueName);
}
