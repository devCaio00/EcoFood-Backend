using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoFood.Api.Controllers;
using EcoFood.Api.Dtos;
using EcoFood.Api.Models;
using Xunit;

namespace EcoFood.Api.Tests.Controllers;

public class DoacoesControllerTests
{
    [Fact]
    public async Task Create_DeveRecusar_QuandoUsuarioNaoEhDoador()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, receptor, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);
        
        var dto = new DoacaoCreateDto(
            DoadorId: receptor.Id, // Um receptor tentando doar
            Descricao: "Cenouras",
            Categoria: "Legumes",
            Quantidade: 5,
            UnidadeMedida: "kg",
            ValidadeAlimento: DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
            Observacoes: ""
        );


        var resultado = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        
        // Garante que a doacao do cenario inicial continua sendo a unica
        Assert.Single(context.Doacoes);
    }

    [Fact]
    public async Task Cancelar_DeveRetornar204_QuandoDoacaoEstaDisponivel()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, _, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.Cancelar(doacao.Id);

        Assert.IsType<NoContentResult>(resultado);
        var doacaoRecarregada = await context.Doacoes.FindAsync(doacao.Id);
        Assert.Equal(StatusDoacao.Cancelado, doacaoRecarregada!.Status);
    }

    [Fact]
    public async Task Cancelar_DeveRecusar_QuandoDoacaoJaReservada()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, _, doacao) = TestDbContextFactory.SemearCenario(context);
        
        // Muda o status para simular que alguem ja reservou
        doacao.Status = StatusDoacao.Reservado;
        await context.SaveChangesAsync();
        
        var controller = new DoacoesController(context);

        var resultado = await controller.Cancelar(doacao.Id);

        Assert.IsType<BadRequestObjectResult>(resultado);
        var doacaoRecarregada = await context.Doacoes.FindAsync(doacao.Id);
        
        // Nao cancelou
        Assert.Equal(StatusDoacao.Reservado, doacaoRecarregada!.Status); 
    }

    [Fact]
    public async Task GetAll_DeveRetornarApenasStatusDisponivel()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.GetAll(StatusDoacao.Disponivel);

        var okResult = Assert.IsType<OkObjectResult>(resultado.Result);
        var doacoes = Assert.IsAssignableFrom<IEnumerable<DoacaoResponseDto>>(okResult.Value);
        
        Assert.Single(doacoes);
        
        Assert.Equal(StatusDoacao.Disponivel.ToString(), doacoes.First().Status.ToString());
    }
}