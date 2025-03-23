
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheMigrator.Migrations
{

    public static class ScheModelGenerator
    {
        public static string GenerateModelDDL<T>(SqlImplementation sqlImplementation, string schema = "public")
        {
            var type = typeof(T);
            return GenerateModelDDL(type, sqlImplementation, schema);
        }

        public static string GenerateModelDDL(Type type, SqlImplementation sqlImplementation, string schema = "public")
        {
            var tableName = ScheModelUtil.GetTableName(type);
            var sqlGenerator = SqlGeneratorFactory.Build(sqlImplementation, schema, tableName);
            var columns = GetColumns(type);

            if (!columns.Any())
            {
                throw new ArgumentException($"No columns found in type {type.Name}. Make sure properties are marked with [Column] attribute.");
            }

            var sb = new IndentedStringBuilder();

            // Generate DDL in steps using the SQL generator
            sb.AppendLine(sqlGenerator.GenerateOpenBlock());
            sb.Indent();

            sb.AppendLine(sqlGenerator.GenerateCreateSchema());
            sb.AppendLine();

            sb.AppendLine(sqlGenerator.GenerateCreateTempTable());
            sb.AppendLine();

            sb.AppendLine(sqlGenerator.GenerateCreateTable(columns));
            sb.AppendLine();

            // Add new columns
            foreach (var column in columns)
            {
                sb.AppendLine(sqlGenerator.GenerateAddColumn(column));
                sb.AppendLine();
            }

            // Let the implementation handle schema changes appropriately
            sb.AppendLine(sqlGenerator.GenerateTableRecreation(columns));
            sb.AppendLine();

            // Modify existing columns
            foreach (var column in columns)
            {
                sb.AppendLine(sqlGenerator.GenerateModifyColumn(column));
                sb.AppendLine();
            }

            // Drop unused columns
            sb.AppendLine(sqlGenerator.GenerateDropUnusedColumns(columns));
            sb.AppendLine();

            // Cleanup
            sb.AppendLine(sqlGenerator.GenerateCleanup());

            // Close the script
            sb.Unindent();
            sb.AppendLine(sqlGenerator.GenerateCloseBlock());

            return sb.ToString();
        }

        private static IEnumerable<ColumnInfo> GetColumns(Type type)
        {
            string[] COLUMN_ATTRS = { nameof(ScheDataAttribute), nameof(ScheKeyAttribute) };

            return type.GetProperties()
                .Select(p => (Property: p, Attribute: p.GetCustomAttributesData()
                    .FirstOrDefault(a => COLUMN_ATTRS.Contains(a.AttributeType.Name))))
                .Where(x => x.Attribute != null)
                .Select(x =>
                {
                    var attrType = x.Attribute.GetType();
                    var isAttrNullable = (bool?)attrType.GetProperty(nameof(ScheDataAttribute.IsNullable))?.GetValue(x.Attribute) ?? true;
                    var isTypeNullable = x.Property.PropertyType.IsGenericType && x.Property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

                    return new ColumnInfo
                    {
                        Name = (attrType.GetProperty("Name")?.GetValue(x.Attribute) as string)
                            ?? x.Property.Name.ToLower(),
                        PropertyType = x.Property.PropertyType,
                        IsPrimaryKey = (bool?)attrType.GetProperty(nameof(ScheDataAttribute.IsPrimaryKey))?.GetValue(x.Attribute) ?? false,
                        IsNullable = isAttrNullable || isTypeNullable
                    };
                });
        }
    }
}