using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Models;

namespace EcoFood.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasIndex(u => u.Email).IsUnique();

            // Grava o NOME do enum no banco ('Doador'), nao o numero (0)
            e.Property(u => u.TipoUsuario).HasConversion<string>();
        });

        modelBuilder.Entity<Doacao>(e =>
        {
            e.ToTable("Doacoes");
            e.Property(d => d.Status).HasConversion<string>();

            // Um doador tem muitas doacoes; apagar o doador apaga as doacoes
            e.HasOne(d => d.Doador)
                .WithMany(u => u.Doacoes)
                .HasForeignKey(d => d.DoadorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reserva>(e =>
        {
            e.ToTable("Reservas");
            e.Property(r => r.Status).HasConversion<string>();

            // Espelha a restricao UNIQUE do banco: uma reserva por doacao
            e.HasIndex(r => r.DoacaoId).IsUnique();

            // Relacao um-para-um entre Doacao e Reserva
            e.HasOne(r => r.Doacao)
                .WithOne(d => d.Reserva)
                .HasForeignKey<Reserva>(r => r.DoacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Receptor)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.ReceptorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}