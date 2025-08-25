using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data.Seeders
{
    public static class EstudianteSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Estudiante>().HasData(
                new Estudiante
                {
                    Id = 1,
                    NumeroRegistro = "20251234",
                    CarnetIdentidad = "7894561",
                    PasswordHash = "$2a$11$YP4gNElOt/d79qZIHbwT3eAxgSg8R.DkDofhZOnq2dH1/IytjKdSq",
                    Nombre = "Maria",
                    Apellido = "Fernandez",
                    Correo = "maria@uagrm.edu.bo"
                }
            );
        }
    }
}
