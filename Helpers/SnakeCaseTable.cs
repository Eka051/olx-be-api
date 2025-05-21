using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.RegularExpressions;

namespace olx_be_api.Helpers
{
    public static class SnakeCaseTable
    {

        public static void UseSnakeCase(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()!));

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(ToSnakeCase(key.GetName()!));
                }

                foreach (var key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(ToSnakeCase(key.GetConstraintName()!));
                }

                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
                }
            }
        }
        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLowerInvariant(name[0]));

            for (int i = 1; i < name.Length; ++i)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
