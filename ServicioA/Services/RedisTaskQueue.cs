using StackExchange.Redis;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class RedisTaskQueue : IBackgroundTaskQueue
{
    private readonly IDatabase _db;
    private const string QueueKey = "tx-queue";  

    public RedisTaskQueue(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    // Encola la transacción al final de la lista
    public async Task EnqueueAsync(Transaccion transaccion)
    {
        var json = JsonSerializer.Serialize(transaccion);
        await _db.ListRightPushAsync(QueueKey, json);
    }

    // Saca una transacción del inicio de la cola
    public async Task<Transaccion?> TryDequeueAsync(CancellationToken ct)
    {
        var val = await _db.ListLeftPopAsync(QueueKey);
        if (val.IsNull) return null;

        return JsonSerializer.Deserialize<Transaccion>(val!);
    }
}
