using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class DetalleInscripcionSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DetalleInscripcion>().HasData(
                new DetalleInscripcion { Id = 1, Codigo = "DET001", Estado = "INSCRITO",  InscripcionId = 1, GrupoMateriaId = 1 },
                new DetalleInscripcion { Id = 2, Codigo = "DET002", Estado = "INSCRITO", InscripcionId = 2, GrupoMateriaId = 2 }
            );
        }
    }
}
