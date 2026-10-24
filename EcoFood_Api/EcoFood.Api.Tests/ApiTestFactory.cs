using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EcoFood.Api.Data;
using EcoFood.Api.Models;
using System.Linq;

namespace EcoFood.Api.Tests;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    // Raiz compartilhada: garante que o seed e a API enxerguem o mesmo banco em memória
    private static readonly InMemoryDatabaseRoot DbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1) Remove tudo que o Program.cs registrou para o AppDbContext (MySQL)
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(AppDbContext));

            // 2) Remove a configuração do provider guardada pelo EF Core 9+ (onde fica o UseMySql)
            var configuracoes = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration") &&
                            d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))
                .ToList();

            foreach (var d in configuracoes)
                services.Remove(d);

            // 3) Registra somente o InMemory
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("ecofood-integracao", DbRoot);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // 4) Popula o cenário
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (!context.Usuarios.Any())
            {
                var doador = new Usuario
                {
                    Nome = "Padaria Integracao",
                    Email = "padaria@integracao.com",
                    TipoUsuario = TipoUsuario.Doador,
                    Endereco = "R. das Flores, 100"
                };

                var receptor = new Usuario
                {
                    Nome = "ONG Integracao",
                    Email = "ong@integracao.com",
                    TipoUsuario = TipoUsuario.Receptor,
                    Endereco = "R. da Esperanca, 50"
                };

                context.Usuarios.AddRange(doador, receptor);
                context.SaveChanges();

                context.Doacoes.Add(new Doacao
                {
                    DoadorId = doador.Id,
                    Descricao = "Paes do dia",
                    Quantidade = 10,
                    UnidadeMedida = "kg",
                    Status = StatusDoacao.Disponivel
                });

                context.SaveChanges();
            }
        });
    }
}