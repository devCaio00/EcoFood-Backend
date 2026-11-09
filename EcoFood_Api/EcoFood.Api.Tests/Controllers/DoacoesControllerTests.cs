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

    [Fact]
    public async Task GetAll_SemFiltro_DeveRetornarTodasAsDoacoes()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);

        context.Doacoes.Add(new Doacao
        {
            DoadorId = doador.Id,
            Descricao = "Ja cancelada",
            Categoria = "Graos",
            Quantidade = 1,
            UnidadeMedida = "kg",
            Status = StatusDoacao.Cancelado
        });
        await context.SaveChangesAsync();
        var controller = new DoacoesController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var doacoes = Assert.IsAssignableFrom<IEnumerable<DoacaoResponseDto>>(ok.Value);
        Assert.Equal(2, doacoes.Count());
    }

    [Fact]
    public async Task GetAll_DeveOrdenarPorValidade_ComSemValidadeNoFinal()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);

        Doacao Nova(string descricao, DateOnly? validade) => new()
        {
            DoadorId = doador.Id,
            Descricao = descricao,
            Categoria = "Teste",
            Quantidade = 1,
            UnidadeMedida = "kg",
            ValidadeAlimento = validade,
            Status = StatusDoacao.Disponivel
        };

        context.Doacoes.AddRange(
            Nova("SemValidade", null),
            Nova("Tarde", new DateOnly(2099, 12, 31)),
            Nova("Cedo", new DateOnly(2000, 1, 1)));
        await context.SaveChangesAsync();
        var controller = new DoacoesController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var doacoes = Assert.IsAssignableFrom<IEnumerable<DoacaoResponseDto>>(ok.Value);

        var ordem = doacoes
            .Select(d => d.Descricao)
            .Where(d => d is "Cedo" or "Tarde" or "SemValidade")
            .ToList();

        Assert.Equal(new[] { "Cedo", "Tarde", "SemValidade" }, ordem);
    }

    [Fact]
    public async Task GetById_DeveRetornarOk_QuandoDoacaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        var (_, _, doacao) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.GetById(doacao.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        Assert.IsType<DoacaoResponseDto>(ok.Value);
    }

    [Fact]
    public async Task GetById_DeveRetornarNotFound_QuandoDoacaoNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.GetById(9999);

        Assert.IsType<NotFoundObjectResult>(resultado.Result);
    }

    private static DoacaoCreateDto CriarDto(
        int doadorId, int quantidade = 5, string? unidade = "kg") => new(
            DoadorId: doadorId,
            Descricao: "Arroz",
            Categoria: "Graos",
            Quantidade: quantidade,
            UnidadeMedida: unidade!,
            ValidadeAlimento: DateOnly.FromDateTime(DateTime.Now.AddDays(10)),
            Observacoes: "");

    [Fact]
    public async Task Create_DeveRecusar_QuandoDoadorNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.Create(CriarDto(doadorId: 9999));

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Single(context.Doacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Create_DeveRecusar_QuandoQuantidadeNaoEhPositiva(int quantidade)
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.Create(CriarDto(doador.Id, quantidade));

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Single(context.Doacoes);
    }

    [Fact]
    public async Task Create_DevePersistirDoacaoDisponivel_QuandoDadosValidos()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.Create(CriarDto(doador.Id, 5, "litros"));

        var created = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        Assert.Equal(nameof(DoacoesController.GetById), created.ActionName);
        Assert.IsType<DoacaoResponseDto>(created.Value);

        Assert.Equal(2, context.Doacoes.Count());
        var salva = context.Doacoes.Single(d => d.Descricao == "Arroz");
        Assert.Equal(StatusDoacao.Disponivel, salva.Status);
        Assert.Equal("litros", salva.UnidadeMedida);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_DeveUsarKgComoUnidadePadrao_QuandoUnidadeVazia(string unidade)
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        await controller.Create(CriarDto(doador.Id, 5, unidade));

        var salva = context.Doacoes.Single(d => d.Descricao == "Arroz");
        Assert.Equal("kg", salva.UnidadeMedida);
    }

    [Fact]
    public async Task Cancelar_DeveRetornarNotFound_QuandoDoacaoNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new DoacoesController(context);

        var resultado = await controller.Cancelar(9999);

        Assert.IsType<NotFoundResult>(resultado);
    }
}