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
            e.Property(u => u.SenhaHash).IsConcurrencyToken();

            // Grava o NOME do enum no banco ('Doador'), nao o numero (0)
            e.Property(u => u.TipoUsuario).HasConversion<string>();
        });

        modelBuilder.Entity<Doacao>(e =>
        {
            e.ToTable("Doacoes");
            e.Property(d => d.Status).HasConversion<string>();

            // Preserva doacoes e reservas ao tentar excluir uma conta.
            e.HasOne(d => d.Doador)
                .WithMany(u => u.Doacoes)
                .HasForeignKey(d => d.DoadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reserva>(e =>
        {
            e.ToTable("Reservas");
            e.Property(r => r.Status).HasConversion<string>();

            // Indice de apoio para buscar reservas por doacao (NAO unique:
            // uma doacao pode ter mais de uma reserva ao longo do tempo,
            // ex. uma cancelada e outra criada depois dela).
            e.HasIndex(r => r.DoacaoId);

            // Relacao um-para-muitos: uma doacao pode ter varias reservas
            // historicamente, mas no maximo uma ativa por vez (garantido
            // pelo Status da doacao no controller, nao pelo banco).
            e.HasOne(r => r.Doacao)
                .WithMany(d => d.Reservas)
                .HasForeignKey(r => r.DoacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Receptor)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.ReceptorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
