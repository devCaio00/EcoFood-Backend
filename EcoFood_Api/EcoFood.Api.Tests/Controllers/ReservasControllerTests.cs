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

    [Fact]
    public async Task Concluir_DeveRetornar204_EAtualizarStatus_QuandoReservaPendente()
    {
        // Arrange
        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        
        // Simulamos uma reserva ja feita no banco
        var reserva = new Reserva
        {
            DoacaoId = doacao.Id,
            ReceptorId = receptor.Id,
            EnderecoConfirmado = true,
            EnderecoRetiradaSnapshot = doador.Endereco,
            Status = StatusReserva.Pendente
        };
        context.Reservas.Add(reserva);
        doacao.Status = StatusDoacao.Reservado; // A doacao fica reservada
        await context.SaveChangesAsync();

        var controller = new ReservasController(context);

        // Act
        var resultado = await controller.Concluir(reserva.Id);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        
        var reservaAtualizada = await context.Reservas.FindAsync(reserva.Id);
        Assert.Equal(StatusReserva.Concluida, reservaAtualizada!.Status);
        
        var doacaoAtualizada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Retirado, doacaoAtualizada!.Status);
    }

    [Fact]
    public async Task Cancelar_DeveRetornar204_ELiberarDoacao_QuandoReservaExiste()
    {
        // Arrange
        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        
        var reserva = new Reserva
        {
            DoacaoId = doacao.Id,
            ReceptorId = receptor.Id,
            EnderecoConfirmado = true,
            EnderecoRetiradaSnapshot = doador.Endereco,
            Status = StatusReserva.Pendente
        };
        context.Reservas.Add(reserva);
        doacao.Status = StatusDoacao.Reservado;
        await context.SaveChangesAsync();

        var controller = new ReservasController(context);

        // Act
        var resultado = await controller.Cancelar(reserva.Id);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        
        var reservaAtualizada = await context.Reservas.FindAsync(reserva.Id);
        Assert.Equal(StatusReserva.Cancelada, reservaAtualizada!.Status);
        
        // A doacao volta a ficar disponivel para outras pessoas
        var doacaoAtualizada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Disponivel, doacaoAtualizada!.Status);
    }

    private static Reserva NovaReserva(
        int doacaoId,
        int receptorId,
        StatusReserva status = StatusReserva.Pendente,
        DateTime? dataReserva = null)
    {
        var reserva = new Reserva
        {
            DoacaoId = doacaoId,
            ReceptorId = receptorId,
            EnderecoConfirmado = true,
            EnderecoRetiradaSnapshot = "Rua Teste, 1",
            Status = status
        };

        if (dataReserva.HasValue)
            reserva.DataReserva = dataReserva.Value;

        return reserva;
    }

    private static Doacao NovaDoacao(int doadorId, string descricao) => new()
    {
        DoadorId = doadorId,
        Descricao = descricao,
        Categoria = "Teste",
        Quantidade = 1,
        UnidadeMedida = "kg",
        Status = StatusDoacao.Disponivel
    };

    [Fact]
    public async Task GetAll_SemFiltro_DeveRetornarTodasAsReservas()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);

        var outraDoacao = NovaDoacao(doador.Id, "Feijao");
        context.Doacoes.Add(outraDoacao);
        await context.SaveChangesAsync();

        context.Reservas.AddRange(
            NovaReserva(doacao.Id, receptor.Id),
            NovaReserva(outraDoacao.Id, receptor.Id));
        await context.SaveChangesAsync();
        var controller = new ReservasController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaResponseDto>>(ok.Value);
        Assert.Equal(2, reservas.Count());
    }

    [Fact]
    public async Task GetAll_ComReceptorId_DeveRetornarApenasReservasDoReceptor()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);

        var outroReceptor = new Usuario
        {
            Nome = "Abrigo Dois",
            Email = "abrigo2@teste.com",
            TipoUsuario = TipoUsuario.Receptor,
            Endereco = "Rua B, 2"
        };
        var outraDoacao = NovaDoacao(doador.Id, "Feijao");
        context.Usuarios.Add(outroReceptor);
        context.Doacoes.Add(outraDoacao);
        await context.SaveChangesAsync();

        context.Reservas.AddRange(
            NovaReserva(doacao.Id, receptor.Id),
            NovaReserva(outraDoacao.Id, outroReceptor.Id));
        await context.SaveChangesAsync();
        var controller = new ReservasController(context);

        var resultado = await controller.GetAll(outroReceptor.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaResponseDto>>(ok.Value);
        Assert.Single(reservas);
    }

    [Fact]
    public async Task GetAll_DeveOrdenarPorDataDeReservaDecrescente()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, receptor, doacao) = TestDbContextFactory.SemearCenario(context);

        var outraDoacao = NovaDoacao(doador.Id, "Feijao");
        context.Doacoes.Add(outraDoacao);
        await context.SaveChangesAsync();

        var antiga = NovaReserva(doacao.Id, receptor.Id, dataReserva: new DateTime(2020, 1, 1));
        var recente = NovaReserva(outraDoacao.Id, receptor.Id, dataReserva: new DateTime(2030, 1, 1));
        context.Reservas.AddRange(antiga, recente);
        await context.SaveChangesAsync();
        var controller = new ReservasController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaResponseDto>>(ok.Value).ToList();
        Assert.Equal(recente.Id, reservas[0].Id);
        Assert.Equal(antiga.Id, reservas[1].Id);
    }

    [Fact]
    public async Task Create_DeveRetornarNotFound_QuandoDoacaoNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);

        var resultado = await controller.Create(
            new ReservaCreateDto(9999, receptor.Id, true));

        Assert.IsType<NotFoundObjectResult>(resultado.Result);
        Assert.Empty(context.Reservas);
    }

    [Fact]
    public async Task Create_DeveRecusar_QuandoReceptorNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, _, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);

        var resultado = await controller.Create(
            new ReservaCreateDto(doacao.Id, 9999, true));

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Empty(context.Reservas);

        var doacaoRecarregada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Disponivel, doacaoRecarregada!.Status);
    }

    [Fact]
    public async Task Create_DeveMarcarDoacaoComoReservada_QuandoSucesso()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);

        await controller.Create(new ReservaCreateDto(doacao.Id, receptor.Id, true));

        var doacaoRecarregada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Reservado, doacaoRecarregada!.Status);
    }

    [Fact]
    public async Task Concluir_DeveRetornarNotFound_QuandoReservaNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);

        var resultado = await controller.Concluir(9999);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Theory]
    [InlineData(StatusReserva.Concluida)]
    [InlineData(StatusReserva.Cancelada)]
    public async Task Concluir_DeveRecusar_QuandoReservaNaoEstaPendente(StatusReserva status)
    {
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var reserva = NovaReserva(doacao.Id, receptor.Id, status);
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();
        var controller = new ReservasController(context);

        var resultado = await controller.Concluir(reserva.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        var recarregada = await context.Reservas.FindAsync(reserva.Id);
        Assert.Equal(status, recarregada!.Status);
    }

    [Fact]
    public async Task Concluir_DeveRegistrarDataDeRetirada()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, doacao) = TestDbContextFactory.SemearCenario(context);
        var reserva = NovaReserva(doacao.Id, receptor.Id);
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync();
        var controller = new ReservasController(context);

        await controller.Concluir(reserva.Id);

        var recarregada = await context.Reservas.FindAsync(reserva.Id);
        Assert.NotNull(recarregada!.DataRetirada);
    }

    [Fact]
    public async Task Cancelar_DeveRetornarNotFound_QuandoReservaNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new ReservasController(context);

        var resultado = await controller.Cancelar(9999);

        Assert.IsType<NotFoundResult>(resultado);
    }

}