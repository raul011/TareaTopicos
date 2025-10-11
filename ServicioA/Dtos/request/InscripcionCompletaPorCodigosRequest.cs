namespace TAREATOPICOS.ServicioA.Dtos.request;

public record InscripcionCompletaPorCodigosRequest(
    string EstudianteRegistro,
    string PeriodoGestion,
    List<string> MateriaGrupoCodigos,
    string? Observaciones,
    string? CallbackUrl   // 👈 aquí va como parámetro más
);