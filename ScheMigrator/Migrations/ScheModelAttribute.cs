using System;
using System.Text;

namespace ScheMigrator.Migrations;

public class ScheModelAttribute : Attribute { }

public static class ScheModelUtil
{
    public static string GetTableName(Type type)
    {
        // int underscores = 0;
        // for (var i = 0; i < type.Name.Length; i++)
        // {
        //     if (i > 0 && char.IsUpper(type.Name[i]))
        //         underscores++;
        // }
        var sb = new StringBuilder();
        for (var i = 0; i < type.Name.Length; i++)
        {
            if (i > 0 && char.IsUpper(type.Name[i]))
                sb.Append('_');
            sb.Append(type.Name[i]);
        }
        return sb.ToString();
        //return string.Create(type.Name.Length + underscores, type.Name, (span, name) => {
        //    var position = 0;
        //    for (var i = 0; i < name.Length; i++) {
        //        if (i > 0 && char.IsUpper(name[i]))
        //            span[position++] = '_';
        //        span[position++] = char.ToLower(name[i]);
        //    }
        //});
    }
}