using System;
using System.Text;

namespace ScheMigrator.Migrations;

public class ScheModelAttribute : Attribute { }

public static class ScheModelUtil
{
    public static string GetTableName(Type type)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < type.Name.Length; i++)
        {
            if (i > 0 && char.IsUpper(type.Name[i]))
                sb.Append('_');
            sb.Append(char.ToLower(type.Name[i]));
        }
        return sb.ToString();
    }
}