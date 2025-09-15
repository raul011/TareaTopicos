// File: Services/Processors/DefaultProcessor.cs
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Services.Processors
{
    /// <summary>
    /// Router simple: resuelve qué IProcessor maneja una Transaccion según Entidad.
    /// Cada IProcessor interno puede diferenciar por TipoOperacion.
    /// </summary>
    public sealed class DefaultProcessor : IQueueProcessor
    {
        private readonly ILogger<DefaultProcessor> _logger;
        private readonly ConcurrentDictionary<string, IProcessor> _map;

        public DefaultProcessor(ILogger<DefaultProcessor> logger,
                                IEnumerable<IProcessor> processors)
        {
            _logger = logger;

            // Construimos el mapa Entidad -> IProcessor a partir de los processors registrados.
            // Si tuvieras múltiples entidades, aquí irías agregando más entradas.
            _map = new ConcurrentDictionary<string, IProcessor>(StringComparer.OrdinalIgnoreCase);

            // Buscar el processor de Nivel (implementa IProcessor)
            var nivelProc = processors.FirstOrDefault(p => p.GetType().Name.Contains("NivelProcessor", StringComparison.OrdinalIgnoreCase));
            if (nivelProc is not null)
                _map["Nivel"] = nivelProc;

            // Si necesitas más entidades, registra aquí:
            // _map["OtraEntidad"] = processors.First(p => p is OtraEntidadProcessor);
        }

        public async Task ProcessAsync(Transaccion tx, CancellationToken ct)
        {
            // Valida entrada
            if (string.IsNullOrWhiteSpace(tx.Entidad))
                throw new InvalidOperationException("Transaccion sin Entidad");

            // Busca handler por entidad
            if (!_map.TryGetValue(tx.Entidad, out var handler))
            {
                _logger.LogError("DefaultProcessor: no existe processor para Entidad={Entidad} Tipo={Tipo}",
                                 tx.Entidad, tx.TipoOperacion);
                throw new InvalidOperationException($"Processor no definido para {tx.Entidad}/{tx.TipoOperacion}");
            }

            // Delegar en el processor específico (él discrimina por TipoOperacion).
            await handler.ProcessAsync(tx, ct);
        }
    }
}
