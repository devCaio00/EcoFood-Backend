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

public class UsuariosControllerTests
{
    [Fact]
    public async Task Create_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        using var context = TestDbContextFactory.Criar();
        var controller = new UsuariosController(context);
        
        // CORRIGIDO: Utilizando o construtor posicional do record
        var dto = new UsuarioCreateDto(
            Nome: "Novo Doador", 
            Email: "novo@teste.com", 
            Telefone: "11999999999", 
            TipoUsuario: TipoUsuario.Doador, 
            Endereco: "Rua A, 1"
        );

        // Act
        var resultado = await controller.Create(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var usuarioCriado = await context.Usuarios.SingleAsync();
        Assert.Equal("novo@teste.com", usuarioCriado.Email);
    }

    [Fact]
    public async Task Create_DeveRetornarConflict_QuandoEmailJaExiste()
    {
        // Arrange
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new UsuariosController(context);
        
        // CORRIGIDO: Utilizando o construtor posicional do record
        var dto = new UsuarioCreateDto(
            Nome: "Outro Usuario", 
            Email: doador.Email, // O e-mail ja existe no cenario semeado
            Telefone: "11888888888",
            TipoUsuario: TipoUsuario.Receptor, 
            Endereco: "Rua B, 2"
        );

        // Act
        var resultado = await controller.Create(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task GetAll_DeveRetornarApenasReceptores_QuandoFiltradoPorReceptor()
    {
        // Arrange
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context); // Semeia 1 Doador e 1 Receptor
        var controller = new UsuariosController(context);

        // Act
        var resultado = await controller.GetAll(TipoUsuario.Receptor);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(resultado.Result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponseDto>>(okResult.Value);
        
        Assert.Single(usuarios);
        Assert.Equal(TipoUsuario.Receptor.ToString(), usuarios.First().TipoUsuario);
}

    [Fact]
    public async Task GetAll_SemFiltro_DeveRetornarTodosOsUsuarios()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context); // 1 Doador e 1 Receptor
        var controller = new UsuariosController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponseDto>>(ok.Value);
        Assert.Equal(2, usuarios.Count());
    }

    [Fact]
    public async Task GetAll_DeveOrdenarPorNome()
    {
        using var context = TestDbContextFactory.Criar();
        context.Usuarios.AddRange(
            new Usuario
            {
                Nome = "Zeca",
                Email = "zeca@teste.com",
                TipoUsuario = TipoUsuario.Doador,
                Endereco = "Rua Z, 1"
            },
            new Usuario
            {
                Nome = "Ana",
                Email = "ana@teste.com",
                TipoUsuario = TipoUsuario.Receptor,
                Endereco = "Rua A, 1"
            });
        await context.SaveChangesAsync();
        var controller = new UsuariosController(context);

        var resultado = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var usuarios = Assert.IsAssignableFrom<IEnumerable<UsuarioResponseDto>>(ok.Value).ToList();
        Assert.Equal("Ana", usuarios[0].Nome);
        Assert.Equal("Zeca", usuarios[1].Nome);
    }

    [Fact]
    public async Task GetById_DeveRetornarOk_QuandoUsuarioExiste()
    {
        using var context = TestDbContextFactory.Criar();
        var (doador, _, _) = TestDbContextFactory.SemearCenario(context);
        var controller = new UsuariosController(context);

        var resultado = await controller.GetById(doador.Id);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        Assert.IsType<UsuarioResponseDto>(ok.Value);
    }

    [Fact]
    public async Task GetById_DeveRetornarNotFound_QuandoUsuarioNaoExiste()
    {
        using var context = TestDbContextFactory.Criar();
        TestDbContextFactory.SemearCenario(context);
        var controller = new UsuariosController(context);

        var resultado = await controller.GetById(9999);

        Assert.IsType<NotFoundObjectResult>(resultado.Result);
    }

    [Theory]
    [InlineData("", "ok@teste.com")]
    [InlineData("   ", "ok@teste.com")]
    [InlineData("Fulano", "")]
    [InlineData("Fulano", "   ")]
    public async Task Create_DeveRecusar_QuandoNomeOuEmailVazios(string nome, string email)
    {
        using var context = TestDbContextFactory.Criar();
        var controller = new UsuariosController(context);
        var dto = new UsuarioCreateDto(
            Nome: nome,
            Email: email,
            Telefone: "11999999999",
            TipoUsuario: TipoUsuario.Doador,
            Endereco: "Rua A, 1");

        var resultado = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Empty(context.Usuarios);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_DeveRecusar_QuandoEnderecoVazio(string endereco)
    {
        using var context = TestDbContextFactory.Criar();
        var controller = new UsuariosController(context);
        var dto = new UsuarioCreateDto(
            Nome: "Fulano",
            Email: "fulano@teste.com",
            Telefone: "11999999999",
            TipoUsuario: TipoUsuario.Doador,
            Endereco: endereco);

        var resultado = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Empty(context.Usuarios);
    }
}