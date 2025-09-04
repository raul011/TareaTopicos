using StackExchange.Redis;
using System.Linq;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class RedisTransaccionStore
{
    private readonly IDatabase _redisDb;

    public RedisTransaccionStore(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task AddAsync(Transaccion transaccion)
    {
        var key = $"transaccion:{transaccion.Id}";
        var value = JsonSerializer.Serialize(transaccion);
        // Guardamos la transacción en Redis con un tiempo de expiración (ej. 24 horas)
        await _redisDb.StringSetAsync(key, value, TimeSpan.FromHours(24));
    }

    public async Task<Transaccion?> GetByIdAsync(Guid id)
    {
        var key = $"transaccion:{id}";
        var value = await _redisDb.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Transaccion>(value!);
    }

    public async Task UpdateEstadoAsync(Guid id, string nuevoEstado)
    {
        var transaccion = await GetByIdAsync(id);
        if (transaccion != null)
        {
            transaccion.Estado = nuevoEstado;
            await AddAsync(transaccion); // Re-guardar actualiza el valor
        }
    }

    public async Task<IEnumerable<Transaccion>> GetAllAsync()
    {
        var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "transaccion:*");
        var values = await _redisDb.StringGetAsync(keys.ToArray());
        return values.Select(v => JsonSerializer.Deserialize<Transaccion>(v!)!);
    }
}