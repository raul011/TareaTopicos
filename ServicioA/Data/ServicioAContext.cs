using Microsoft.EntityFrameworkCore;
using TAREATOPICOS.ServicioA.Models;

namespace TAREATOPICOS.ServicioA.Data
{
    public class ServicioAContext : DbContext
    {
        public DbSet<PlanEstudio> PlanesEstudio { get; set; }
        public DbSet<Materia> Materias { get; set; }
        public DbSet<PlanMateria> PlanMaterias { get; set; }

        public ServicioAContext(DbContextOptions<ServicioAContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de claves y restricciones
            modelBuilder.Entity<PlanMateria>()
                .HasIndex(pm => new { pm.PlanId, pm.MateriaId })
                .IsUnique();

            modelBuilder.Entity<PlanMateria>()
                .HasOne(pm => pm.Plan)
                .WithMany(p => p.PlanMaterias)
                .HasForeignKey(pm => pm.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanMateria>()
                .HasOne(pm => pm.Materia)
                .WithMany(m => m.PlanMaterias)
                .HasForeignKey(pm => pm.MateriaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Datos iniciales (Seeder)
            TAREATOPICOS.ServicioA.Data.Seeders.MateriaSeeder.Seed(modelBuilder);
            TAREATOPICOS.ServicioA.Data.Seeders.PlanEstudioSeeder.Seed(modelBuilder);
        }

        
    }
}
