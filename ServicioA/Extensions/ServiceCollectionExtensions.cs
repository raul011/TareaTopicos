// File: Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Collections.Generic;
using TAREATOPICOS.ServicioA.Options;
using TAREATOPICOS.ServicioA.Services;
using TAREATOPICOS.ServicioA.Services.Processors;
 

namespace TAREATOPICOS.ServicioA.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registro central de Redis, colas, workers, opciones y health.
        /// </summary>
        public static IServiceCollection AddServicioAQueues(this IServiceCollection services, IConfiguration cfg)
        {
            // ===== Options (SIN ambigüedad) =====
            services.Configure<RedisOptions>(cfg.GetSection("Redis"));
            services.Configure<RedisQueueOptions>(cfg.GetSection("RedisQueue"));
            services.Configure<QueuesOptions>(opts =>
            {
                opts.Queues = cfg.GetSection("Queues").Get<List<QueueItemOptions>>() ?? new();
            });

            // ===== Redis Connection =====
            var redisConn = cfg.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

            // ===== Cola + Store + utilitarios =====
            services.AddSingleton<IBackgroundTaskQueue, RedisTaskQueue>();
            services.AddSingleton<ITransaccionStore, RedisTransaccionStore>();
            services.AddSingleton<QueueManager>();
            services.AddSingleton<DeadLetterService>();
            services.AddSingleton<VisibilityReclaimer>();
            services.AddSingleton<RateLimiter>();
            services.AddSingleton<ConfigWatcher>();
            services.AddSingleton<RedisScaleBackplane>();

            // ===== Processors (negocio) =====
            // ✅ ahora
services.AddScoped<IQueueProcessor, NivelProcessor>();
services.AddScoped<IQueueProcessor, DefaultProcessor>();

            // ===== Worker Host (HostedService que crea pools/hilos por cola) =====
            services.AddHostedService<WorkerHost>();

            return services;
        }
    }
}
