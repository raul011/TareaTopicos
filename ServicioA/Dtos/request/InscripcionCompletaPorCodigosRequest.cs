namespace TAREATOPICOS.ServicioA.Dtos.request;

public record InscripcionCompletaPorCodigosRequest(
    string EstudianteRegistro,
    string PeriodoGestion,
    List<string> MateriaGrupoCodigos, // Formato: "MAT101-A", "INF121-SB", etc.
    string? Observaciones
);