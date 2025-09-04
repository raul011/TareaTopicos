using System.ComponentModel.DataAnnotations;
namespace TAREATOPICOS.ServicioA.Dtos;

public class DetalleInscripcionDto
{
    public int Id { get; set; }
    [Required]
    [StringLength(20)]
    public string Codigo { get; set; } = null!;
    [StringLength(20)]
    public string Estado { get; set; } = "INSCRITO";
    [Range(0, 100)]
    public int? NotaFinal { get; set; }
    [Required]
    public int InscripcionId { get; set; }
    [Required]
    public int GrupoMateriaId { get; set; }
}