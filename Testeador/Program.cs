using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

// Modelo para guardar la respuesta del API
public class TransaccionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        string baseUrl = "http://localhost:5112/api/niveles"; // Ajusta si tu API corre en otro puerto

        var transacciones = new List<TransaccionResponse>();

        Console.WriteLine(" Enviando transacciones (POST, PUT, DELETE)...");

        // 1) Mandamos 5 POST → crear niveles
        for (int i = 1; i <= 5; i++)
        {
            var nivel = new { numero = i, nombre = $"Nivel {i}" };
            await EnviarTransaccion(client, $"{baseUrl}/async", nivel, transacciones, HttpMethod.Post);
        }

        // 2) Mandamos 2 PUT → actualizar niveles
        for (int i = 1; i <= 2; i++)
        {
            var nivel = new { id = i, numero = i, nombre = $"Nivel {i} (Actualizado)" };
            await EnviarTransaccion(client, $"{baseUrl}/async/{i}", nivel, transacciones, HttpMethod.Put);
        }

        // 3) Mandamos 1 DELETE → borrar un nivel
        await EnviarTransaccion(client, $"{baseUrl}/async/7", null, transacciones, HttpMethod.Delete);

        Console.WriteLine("\n Consultando estados cada 2 segundos...\n");

        // 4) Consultamos estado de cada transacción hasta que todas estén COMPLETADO o ERROR
        bool todasCompletadas = false;
        while (!todasCompletadas)
        {
            todasCompletadas = true;

            foreach (var tx in transacciones)
            {
                var response = await client.GetAsync($"{baseUrl}/estado/{tx.Id}");
                var body = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    Console.WriteLine($" Transacción {tx.Id} devolvió respuesta vacía");
                    todasCompletadas = false;
                    continue;
                }

                var actualizado = JsonSerializer.Deserialize<TransaccionResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (actualizado != null)
                {
                    Console.WriteLine($"Transacción {actualizado.Id} → Estado: {actualizado.Estado}");
                    if (actualizado.Estado != "COMPLETADO" && actualizado.Estado != "ERROR")
                        todasCompletadas = false;
                }
            }

            await Task.Delay(2000); // espera 2 segundos entre rondas
            Console.WriteLine("----------------------------");
        }

        Console.WriteLine("\n Todas las transacciones fueron procesadas!");
    }

    // Método helper para enviar y registrar transacciones
    static async Task EnviarTransaccion(HttpClient client, string url, object? payload, List<TransaccionResponse> lista, HttpMethod method)
    {
        HttpResponseMessage response;

        if (method == HttpMethod.Delete)
        {
            response = await client.DeleteAsync(url);
        }
        else if (method == HttpMethod.Put)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await client.PutAsync(url, content);
        }
        else // default POST
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await client.PostAsync(url, content);
        }

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DEBUG → {body}"); // para ver qué devuelve tu API

        if (!string.IsNullOrWhiteSpace(body))
        {
            var tx = JsonSerializer.Deserialize<TransaccionResponse>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (tx != null)
            {
                lista.Add(tx);
                Console.WriteLine($" Transacción {tx.Id} encolada → Estado inicial: {tx.Estado}");
            }
        }
        else
        {
            Console.WriteLine(" La API devolvió respuesta vacía.");
        }
    }
}
