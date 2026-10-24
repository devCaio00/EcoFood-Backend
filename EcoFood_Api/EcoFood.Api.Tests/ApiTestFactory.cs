using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EcoFood.Api.Data;
using EcoFood.Api.Models;

namespace EcoFood.Api.Tests;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descritor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                
            if (descritor is not null)
                services.Remove(descritor);


            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ecofood-integracao")
                       .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));


            using var scope = services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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