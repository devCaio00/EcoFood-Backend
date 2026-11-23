using System.ComponentModel.DataAnnotations;
using EcoFood.Api.Models;

namespace EcoFood.Api.Dtos;

// Entrada: o que o cliente PODE enviar ao cadastrar
public record UsuarioCreateDto(
    [Required, StringLength(150)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [StringLength(20)] string? Telefone,
    TipoUsuario TipoUsuario,
    [Required, StringLength(255)] string Endereco,
    [StringLength(128, MinimumLength = 8)] string? Senha = null
);

// Saída: o que a API devolve
public record UsuarioResponseDto(
    int Id,
    string Nome,
    string Email,
    string? Telefone,
    string TipoUsuario,
    string Endereco,
    DateTime DataCadastro
)
{
    // Fábrica: converte a entidade no DTO de saída.
    // Fica aqui para a conversão existir em um único lugar.
    public static UsuarioResponseDto FromEntity(Usuario u) => new(
        u.Id,
        u.Nome,
        u.Email,
        u.Telefone,
        u.TipoUsuario.ToString(),
        u.Endereco,
        u.DataCadastro
    );
}

public record UsuarioUpdateDto(
    [Required, StringLength(150)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [StringLength(20)] string? Telefone,
    [Required, StringLength(255)] string Endereco,
    [StringLength(128)] string? SenhaAtual = null,
    [StringLength(128, MinimumLength = 8)] string? NovaSenha = null
);
