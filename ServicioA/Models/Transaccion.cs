// namespace TAREATOPICOS.ServicioA.Models;

// public class Transaccion
// {
//     public string Id { get; set; } = Guid.NewGuid().ToString();
//     public string Estado { get; set; } = "EN_COLA"; // EN_COLA, PROCESANDO, COMPLETADO, ERROR
//     public string Tipo { get; set; } = string.Empty; // ej: "CrearNivel"
//     public object? Payload { get; set; } // los datos que vienen (ej: Nivel)
// }
public class Transaccion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TipoOperacion { get; set; } = ""; // "POST", "PUT", "DELETE", "GET"
    public string Entidad { get; set; } = "";       // "Nivel", "Materia", etc.
    public string? Payload { get; set; }            // Datos serializados en JSON
    public string Estado { get; set; } = "EN_COLA";
}
// registramos todas las transacciones