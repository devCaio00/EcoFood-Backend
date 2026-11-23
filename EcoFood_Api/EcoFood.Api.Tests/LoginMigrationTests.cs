using EcoFood.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EcoFood.Api.Tests;

public class LoginMigrationTests
{
    [Fact]
    public void MigracaoMySql_CorrespondeAoModeloEPreservaVinculos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=ecofood;", new MySqlServerVersion(new Version(8, 0)))
            .Options;

        using var context = new AppDbContext(options);

        Assert.False(context.Database.HasPendingModelChanges());

        var migrator = context.GetService<IMigrator>();
        var script = migrator.GenerateScript("20261024170940_Inicial", "20261122160000_LoginUsuarios");
        Assert.Contains("ADD `SenhaHash` varchar(255) NULL", script);
        Assert.Contains("FOREIGN KEY (`DoadorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT", script);
        Assert.Contains("FOREIGN KEY (`ReceptorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT", script);
    }
}
