using FluentMigrator;

namespace ConsoleApp1.Migrations;

[Migration(1, "Initial")]
public class Initial : MigrationBase
{
    protected override string GetUpSql(IServiceProvider services) =>
        $"""
         create table Words (
            Id int primary key identity(1,1),
            Word varchar(511) unique not null,
            Count int not null default 0
         );
         """;

    protected override string GetDownSql(IServiceProvider services) =>
        $"""
         drop table Words;
         """;
}