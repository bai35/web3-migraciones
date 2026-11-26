using GimnasioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GimnasioApi.Data;

/// <summary>
/// VERSIÓN CODE FIRST: el esquema de la base de datos se define aquí (entidades + configuración)
/// y se crea con migraciones (dotnet ef migrations add / database update).
/// </summary>
public class GimnasioDbContext : DbContext
{
    public GimnasioDbContext(DbContextOptions<GimnasioDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Entrenador> Entrenadores => Set<Entrenador>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Socio> Socios => Set<Socio>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<PlanEntrenamiento> PlanesEntrenamiento => Set<PlanEntrenamiento>();
    public DbSet<Ejercicio> Ejercicios => Set<Ejercicio>();
    public DbSet<PlanEjercicio> PlanEjercicios => Set<PlanEjercicio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario", t => t.HasCheckConstraint("CK_Usuario_Rol", "[Rol] IN ('ADMIN','ENTRENADOR','SOCIO')"));
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.PasswordHash).HasMaxLength(200);
            e.Property(x => x.Rol).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshToken");
            e.Property(x => x.TokenHash).HasMaxLength(100);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Entrenador>(e =>
        {
            e.ToTable("Entrenador");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Documento).HasMaxLength(20);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.Property(x => x.Especialidad).HasMaxLength(100);
            e.HasIndex(x => x.Documento).IsUnique();
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Membresia>(e =>
        {
            e.ToTable("Membresia");
            e.Property(x => x.Nombre).HasMaxLength(80);
            e.Property(x => x.Precio).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Socio>(e =>
        {
            e.ToTable("Socio");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Documento).HasMaxLength(20);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasIndex(x => x.Documento).IsUnique();
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Entrenador).WithMany().HasForeignKey(x => x.EntrenadorId);
            e.HasOne(x => x.Membresia).WithMany().HasForeignKey(x => x.MembresiaId);
        });

        modelBuilder.Entity<Asistencia>(e =>
        {
            e.ToTable("Asistencia");
            e.Property(x => x.Observacion).HasMaxLength(250);
            e.HasIndex(x => new { x.SocioId, x.FechaHora });
            e.HasOne(x => x.Socio).WithMany().HasForeignKey(x => x.SocioId);
            e.HasOne(x => x.Entrenador).WithMany().HasForeignKey(x => x.EntrenadorId);
        });

        modelBuilder.Entity<PlanEntrenamiento>(e =>
        {
            e.ToTable("PlanEntrenamiento");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Objetivo).HasMaxLength(250);
            e.HasOne(x => x.Socio).WithMany().HasForeignKey(x => x.SocioId);
            e.HasOne(x => x.Entrenador).WithMany().HasForeignKey(x => x.EntrenadorId);
        });

        modelBuilder.Entity<Ejercicio>(e =>
        {
            e.ToTable("Ejercicio");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.GrupoMuscular).HasMaxLength(50);
        });

        modelBuilder.Entity<PlanEjercicio>(e =>
        {
            e.ToTable("PlanEjercicio");
            e.HasKey(x => new { x.PlanEntrenamientoId, x.EjercicioId });
            e.HasOne(x => x.PlanEntrenamiento).WithMany().HasForeignKey(x => x.PlanEntrenamientoId);
            e.HasOne(x => x.Ejercicio).WithMany().HasForeignKey(x => x.EjercicioId);
        });

        // Sin borrado en cascada: las bajas se hacen de forma lógica (Activo = false)
        // y así se evitan los "multiple cascade paths" de SQL Server.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
