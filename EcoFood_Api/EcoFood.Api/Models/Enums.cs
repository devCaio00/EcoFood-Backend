namespace EcoFood.Api.Models;
public enum TipoUsuario
{
Doador,
Receptor,
Admin
}
public enum StatusDoacao
{
Disponivel,
Reservado,
Retirado,
Expirado,
Cancelado
}
public enum StatusReserva
{
Pendente,
Concluida,
Cancelada
}