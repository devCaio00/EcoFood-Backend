using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Controllers;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;
using Xunit;

namespace EcoFood.Api.Tests.Controllers;

public class ReservasControllerTests
{
    [Fact]
    public async Task Create_DeveRecusar_QuandoEnderecoNaoConfirmado()
    {
       
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);
        var dto = new ReservaCreateDto(
            DoacaoId: doacao.Id,
            ReceptorId: receptor.Id,
            EnderecoConfirmado: false); 

        var resultado = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);

        Assert.Empty(context.Reservas);
        
        var doacaoRecarregada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Disponivel, doacaoRecarregada!.Status);
    }

    [Fact]
    public async Task Create_DeveCriarReserva_QuandoEnderecoConfirmado()
    {

        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);
        var dto = new ReservaCreateDto(doacao.Id, receptor.Id, EnderecoConfirmado: true);


        var resultado = await controller.Create(dto);


        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var reserva = await context.Reservas.SingleAsync();
        Assert.True(reserva.EnderecoConfirmado);
        Assert.Equal(StatusReserva.Pendente, reserva.Status);
    }

    [Fact]
    public async Task Create_DeveGravarSnapshotDoEnderecoDoDoador()
    {

        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);


        await controller.Create(new ReservaCreateDto(doacao.Id, receptor.Id, true));


        var reserva = await context.Reservas.SingleAsync();
        Assert.Equal(doador.Endereco, reserva.EnderecoRetiradaSnapshot);
        Assert.NotEqual(receptor.Endereco, reserva.EnderecoRetiradaSnapshot);
    }

    [Fact]
    public async Task Create_DeveRecusar_QuandoDoacaoJaReservada()
    {
    
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);
        await controller.Create(new ReservaCreateDto(doacao.Id, receptor.Id, true));


        var outroReceptor = new Usuario
        {
            Nome = "Abrigo Teste",
            Email = "abrigo@teste.com",
            TipoUsuario = TipoUsuario.Receptor,
            Endereco = "Tv. da Uniao, 20"
        };
        context.Usuarios.Add(outroReceptor);
        await context.SaveChangesAsync();
        
        var resultado = await controller.Create(
            new ReservaCreateDto(doacao.Id, outroReceptor.Id, true));


        Assert.IsType<ConflictObjectResult>(resultado.Result);
        Assert.Single(context.Reservas); // continua havendo apenas uma
    }

    [Fact]
    public async Task Create_DeveRecusar_QuandoUsuarioNaoEhReceptor()
    {

        using var context = TestDbContextFactory.Criar();
        var (doador, _, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);


        var resultado = await controller.Create(
            new ReservaCreateDto(doacao.Id, doador.Id, true));


        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Empty(context.Reservas);
    }
}