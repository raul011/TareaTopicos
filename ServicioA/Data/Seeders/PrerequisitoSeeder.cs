using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class PrerequisitoSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prerequisito>().HasData(
                new Prerequisito { Id = 1, Tipo = "Académico", NotaMin = 60, MateriaId = 3, MateriaPrerequisitoId = 1 }
            );
        }
    }
}
