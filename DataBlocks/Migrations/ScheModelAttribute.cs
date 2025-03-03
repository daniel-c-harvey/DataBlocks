namespace DataBlocks.Migrations;

public class ScheModelAttribute : Attribute { }

public static class ScheModelUtil
{
    public static string GetTableName(Type type)
    {
        int underscores = 0;
        for (var i = 0; i < type.Name.Length; i++)
        {
            if (i > 0 && char.IsUpper(type.Name[i]))
                underscores++;
        }
        return string.Create(type.Name.Length + underscores, type.Name, (span, name) => {
            var position = 0;
            for (var i = 0; i < name.Length; i++) {
                if (i > 0 && char.IsUpper(name[i]))
                    span[position++] = '_';
                span[position++] = char.ToLower(name[i]);
            }
        });
    }
}