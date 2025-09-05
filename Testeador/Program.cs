using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public class TransaccionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

class Program
{
    static async Task Main(string[] args)
    {
        // Puerto/URL configurable
        var baseUrl = args.Length > 0
            ? args[0]
            : (Environment.GetEnvironmentVariable("API_BASEURL") ?? "http://localhost:5112/api/niveles");

        using var client = new HttpClient();
        var transacciones = new List<TransaccionResponse>();

        Console.WriteLine($"Usando baseUrl: {baseUrl}");
        Console.WriteLine("Enviando transacciones (POST, PUT, DELETE)...");

        // POST
        for (int i = 1; i <= 5; i++)
        {
            var nivel = new { numero = i, nombre = $"Nivel {i}" };
            await EnviarTransaccion(client, $"{baseUrl}/async", nivel, transacciones, HttpMethod.Post);
        }

        //  PUT
        for (int i = 1; i <= 2; i++)
        {
            var nivel = new { id = i, numero = i, nombre = $"Nivel {i} (Actualizado)" };
            await EnviarTransaccion(client, $"{baseUrl}/async/{i}", nivel, transacciones, HttpMethod.Put);
        }

        // DELETE elige uno valido
        await EnviarTransaccion(client, $"{baseUrl}/async/7", null, transacciones, HttpMethod.Delete);

       
        Console.WriteLine("\nConsultando estados (cada 500 ms)...\n");
        var ultimoEstado = new Dictionary<string, string>();
        bool todasCompletadas = false;

        while (!todasCompletadas)
        {
            todasCompletadas = true;

            foreach (var tx in transacciones)
            {
                var resp = await client.GetAsync($"{baseUrl}/estado/{tx.Id}");
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"GET estado {tx.Id} → HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
                    todasCompletadas = false;
                    continue;
                }

                var body = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body))
                {
                    Console.WriteLine($"Transacción {tx.Id} → respuesta vacía");
                    todasCompletadas = false;
                    continue;
                }

                var actualizado = JsonSerializer.Deserialize<TransaccionResponse>(
                    body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (actualizado is null)
                {
                    Console.WriteLine($"No pude deserializar estado para {tx.Id}");
                    todasCompletadas = false;
                    continue;
                }

                // imprime solo cuando el estado CAMBIA
                var estadoActual = actualizado.Estado ?? "";
                if (!ultimoEstado.TryGetValue(actualizado.Id, out var previo) || previo != estadoActual)
                {
                    var t = DateTime.Now.ToString("HH:mm:ss.fff");
                    Console.WriteLine($"{t} | Transacción {actualizado.Id} → {estadoActual}");
                    ultimoEstado[actualizado.Id] = estadoActual;
                }

                if (estadoActual != "COMPLETADO" && estadoActual != "ERROR")
                    todasCompletadas = false;
            }

            await Task.Delay(500); // más frecuente para atrapar PROCESANDO
        }

        Console.WriteLine("\n¡Todas las transacciones fueron procesadas!");
    }

    // ---- Helper para enviar las transacciones ----
    static async Task EnviarTransaccion(HttpClient client, string url, object? payload, List<TransaccionResponse> lista, HttpMethod method)
    {
        HttpResponseMessage resp;

        if (method == HttpMethod.Delete)
        {
            resp = await client.DeleteAsync(url);
        }
        else
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            resp = method == HttpMethod.Put ? await client.PutAsync(url, content)
                                            : await client.PostAsync(url, content);
        }

        var body = await resp.Content.ReadAsStringAsync();
        Console.WriteLine($"DEBUG {method} {url} → HTTP {(int)resp.StatusCode} | Body: {body}");

        if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
            return;

        var tx = JsonSerializer.Deserialize<TransaccionResponse>(
            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (tx is not null)
        {
            lista.Add(tx);
            Console.WriteLine($"Transacción {tx.Id} encolada → Estado inicial: {tx.Estado}");
        }
    }
}
