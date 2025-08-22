using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Data;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Configurar EF Core con PostgreSQL
builder.Services.AddDbContext<ServicioAContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddControllers();

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.WriteIndented = true;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger para probar tu API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
