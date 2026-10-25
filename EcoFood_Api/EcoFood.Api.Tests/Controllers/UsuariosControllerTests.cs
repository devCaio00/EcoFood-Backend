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
}