using Npgsql;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Permitir recibir la cadena de conexión desde appsettings.json o variable de entorno
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

var app = builder.Build();

// Endpoint de prueba de conexión
app.MapGet("/test-db", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    return Results.Ok("Conexión a PostgreSQL exitosa!");
});

// Endpoint para listar materias desde la base de datos
app.MapGet("/materias", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    var materias = await conn.QueryAsync("SELECT * FROM materias");
    return Results.Ok(materias);
});

// Escucha en todas las interfaces (Docker friendly)
app.Urls.Add("http://0.0.0.0:5000");

app.Run();
