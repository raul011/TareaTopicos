var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/carreras", () => "Todas las carreras disponibles ");
app.Urls.Add("http://0.0.0.0:5000");

app.Run();
