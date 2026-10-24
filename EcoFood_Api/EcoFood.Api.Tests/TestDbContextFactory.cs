using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Data;
using EcoFood.Api.Models;


namespace EcoFood.Api.Tests;

public static class TestDbContextFactory
{
    /// Cria um nome aleatório para o banco, assim um teste não entra em comflito com o outro
    public static AppDbContext Criar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ecofood-teste-{Guid.NewGuid()}")
            .Options;
            
        return new AppDbContext(options);
    }

    /// Popula o banco com um cenário minimo para ter uma reserva.
    /// Tem um doador, recptor e uma doação
    public static (Usuario doador, Usuario receptor, Doacao doacao) SemearCenario(AppDbContext context)
    {
        var doador = new Usuario
        {
            Nome = "Padaria Teste",
            Email = "padaria@teste.com",
            TipoUsuario = TipoUsuario.Doador,
            Endereco = "R. das Flores, 100 Contagem/MG"
        };
        
        var receptor = new Usuario
        {
            Nome = "ONG Teste",
            Email = "ong@teste.com",
            TipoUsuario = TipoUsuario.Receptor,
            Endereco = "R. da Esperanca, 50 Contagem/MG"
        };
        
        context.Usuarios.AddRange(doador, receptor);
        context.SaveChanges();
        
        var doacao = new Doacao
        {
            DoadorId = doador.Id,
            Descricao = "Paes do dia",
            Quantidade = 10,
            UnidadeMedida = "kg",
            Status = StatusDoacao.Disponivel
        };
        
        context.Doacoes.Add(doacao);
        context.SaveChanges();
        
        return (doador, receptor, doacao);
    }
}