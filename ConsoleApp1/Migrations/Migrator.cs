using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp1.Migrations;

public class Migrator(string connectionString)
{
    private readonly IServiceProvider _provider = GetProvider(connectionString);
    
    public void MigrateUp()
    {
        using var scope = _provider.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        migrator.MigrateUp();
    }

    private static ServiceProvider GetProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(
                builder => builder
                    .AddSqlServer()
                    .ScanIn(typeof(MigrationBase).Assembly)
                    .For.Migrations())
            .AddOptions<ProcessorOptions>()
            .Configure(
                options =>
                {
                    options.ConnectionString = connectionString;
                    options.Timeout          = TimeSpan.FromMinutes(1);
                    options.ProviderSwitches = "Force Quote=false";
                });
        return services.BuildServiceProvider();
    }
}