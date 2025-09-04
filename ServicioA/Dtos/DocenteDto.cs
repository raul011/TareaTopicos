using System.ComponentModel.DataAnnotations;

namespace TAREATOPICOS.ServicioA.Dtos;

public class DocenteDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Registro { get; set; } = null!;

    [Required]
    [StringLength(15)]
    public string Ci { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    [StringLength(20)]
    public string? Telefono { get; set; }

    public string Estado { get; set; } = "ACTIVO";
}
