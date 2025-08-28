using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class MateriaSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Materia>().HasData(
                new Materia { Id = 1, Codigo = "MAT101", Nombre = "Matemáticas I", Creditos = 5, NivelId = 1 },
                new Materia { Id = 2, Codigo = "PRG101", Nombre = "Programación I", Creditos = 5, NivelId = 1 },
                new Materia { Id = 3, Codigo = "BD101", Nombre = "Bases de Datos", Creditos = 4, NivelId = 2 }
            );
        }
    }
}
