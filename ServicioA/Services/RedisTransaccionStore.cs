// // tareatopicos.ServicioA/Services/RedisTransaccionStore.cs
using StackExchange.Redis;
using System.Text.Json;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services;

public class RedisTransaccionStore : ITransaccionStore
{
    private readonly IDatabase _db;

    public RedisTransaccionStore(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    private static string Key(Guid id) => $"tx:{id}";

    public async Task AddAsync(Transaccion t)
    {
        await _db.HashSetAsync(Key(t.Id), new HashEntry[] {
            new("id", t.Id.ToString()),
            new("estado", t.Estado ?? "PENDIENTE"),
            new("entidad", t.Entidad ?? ""),
            new("tipo", t.TipoOperacion ?? ""),
            new("payload", t.Payload ?? "")
        });

         
        await _db.SetAddAsync("tx:index", t.Id.ToString());
    }

    public async Task<Transaccion?> GetAsync(Guid id)
    {
        var entries = await _db.HashGetAllAsync(Key(id));
        if (entries.Length == 0) return null;

        var map = entries.ToStringDictionary();
        return new Transaccion {
            Id = id,
            Estado = map.GetValueOrDefault("estado"),
            Entidad = map.GetValueOrDefault("entidad") ?? "",
            TipoOperacion = map.GetValueOrDefault("tipo") ?? "",
            Payload = map.GetValueOrDefault("payload") ?? ""
        };
    }

    public async Task UpdateEstadoAsync(Guid id, string nuevoEstado)
    {
        await _db.HashSetAsync(Key(id), new HashEntry[] { new("estado", nuevoEstado) });
    }

    public async Task<IEnumerable<Transaccion>> GetAllAsync()
    {
        var ids = await _db.SetMembersAsync("tx:index");
        var list = new List<Transaccion>();

        foreach (var id in ids)
        {
            var entries = await _db.HashGetAllAsync($"tx:{id}");
            if (entries.Length == 0) continue;

            var map = entries.ToDictionary(x => (string)x.Name!, x => (string?)x.Value);
            list.Add(new Transaccion {
                Id = Guid.Parse(map["id"]!),
                Estado = map.GetValueOrDefault("estado"),
                Entidad = map.GetValueOrDefault("entidad") ?? "",
                TipoOperacion = map.GetValueOrDefault("tipo") ?? "",
                Payload = map.GetValueOrDefault("payload") ?? ""
            });
        }

        return list;
    }
}
