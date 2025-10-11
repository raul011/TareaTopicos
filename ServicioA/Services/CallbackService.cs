using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;                 // 👈 asegúrate de tener esto
using TAREATOPICOS.ServicioA.Models;
using TAREATOPICOS.ServicioA.Contracts;

namespace TAREATOPICOS.ServicioA.Services
{
    public class CallbackService
    {
        private readonly HttpClient _http;
        private readonly ILogger<CallbackService> _logger;

        public CallbackService(HttpClient http, ILogger<CallbackService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string url, object payload, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(url)) return true;

    var body = JsonSerializer.Serialize(payload);
    using var req = new HttpRequestMessage(HttpMethod.Post, url);
    req.Content = new StringContent(body, Encoding.UTF8, "application/json");

    try
    {
        var resp = await _http.SendAsync(req, ct);
        _logger.LogInformation("Callback → {Url} status {Code}", url, (int)resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Callback → {Url} lanzó excepción", url);
        return false;
    }
}
    }
}
