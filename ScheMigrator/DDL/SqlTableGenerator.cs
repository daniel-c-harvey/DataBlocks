using System.Reflection;
using DataBlocks.Migrations;

namespace ScheMigrator.DDL;

public class SqlTableGenerator
{
    public static SqlTableGeneratorSource GetClassGenerator(Type type)
    {
        var sqlAttr = (ScheModelAttribute?)type.GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name.Contains("GenerateSqlTable"));

        if (sqlAttr == null)
            throw new InvalidOperationException($"Missing \"GenerateSqlTable\" on class {type.Name}");

        return new SqlTableGeneratorSource(SqlGeneratorFactory.Build(sqlAttr.SqlImplementation), type);
    }
}

public sealed class SqlTableGeneratorSource
{
    public ISqlGenerator SqlGenerator { get; }
    public Type Type { get; }

    public SqlTableGeneratorSource(ISqlGenerator sqlGenerator, Type type)
    {
        SqlGenerator = sqlGenerator;
        Type = type;
    }
}