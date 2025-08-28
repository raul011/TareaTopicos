using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class NivelSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Nivel>().HasData(
                new Nivel { Id = 1, Numero = 1, Nombre = "Primer Nivel" },
                new Nivel { Id = 2, Numero = 2, Nombre = "Segundo Nivel" },
                new Nivel { Id = 3, Numero = 3, Nombre = "Tercer Nivel" }
            );
        }
    }
}
