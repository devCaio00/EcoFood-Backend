using System.ComponentModel.DataAnnotations;

namespace EcoFood.Api.Dtos;

public record LoginDto(
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(128)] string Senha
);
