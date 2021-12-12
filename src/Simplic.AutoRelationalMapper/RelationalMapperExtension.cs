using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simplic.AutoRelationalMapper
{
    public static class RelationalMapperExtension
    {
        public static TableConfiguration<T> PrimaryKey<T, F>(this TableConfiguration<T> cfg, Expression<Func<T, F>> primaryKeyField)
        {
            if (primaryKeyField == null)
                throw new ArgumentNullException(nameof(primaryKeyField));

            string name;
            if (primaryKeyField.Body is MemberExpression)
            {
                MemberExpression expr = (MemberExpression)primaryKeyField.Body;
                name = expr.Member.Name;
            }
            else if (primaryKeyField.Body is UnaryExpression)
            {
                MemberExpression expr = (MemberExpression)((UnaryExpression)primaryKeyField.Body).Operand;
                name = expr.Member.Name;
            }
            else if (primaryKeyField.Body is MethodCallExpression methodExpression 
                && methodExpression.Arguments.Any()
                && methodExpression.Arguments[0] is ConstantExpression constExpression
                && typeof(T) == typeof(IDictionary<string, object>))
            {
                name = constExpression.Value.ToString();
            }
            else
            {
                const string Format = "Expression '{0}' not supported.";
                string message = string.Format(Format, primaryKeyField);

                throw new ArgumentException(message, "Field");
            }

            cfg.PrimaryKeys.Add(name);

            return cfg;
        }

        public static TableConfiguration<T> ForeignKey<T, O, F>(this TableConfiguration<T> cfg, string column, Expression<Func<O, F>> primaryKeyField)
        {
            if (primaryKeyField == null)
                throw new ArgumentNullException(nameof(primaryKeyField));

            string name;
            if (primaryKeyField.Body is MemberExpression)
            {
                MemberExpression expr = (MemberExpression)primaryKeyField.Body;
                name = expr.Member.Name;
            }
            else if (primaryKeyField.Body is UnaryExpression)
            {
                MemberExpression expr = (MemberExpression)((UnaryExpression)primaryKeyField.Body).Operand;
                name = expr.Member.Name;
            }
            else if (primaryKeyField.Body is MethodCallExpression methodExpression
                && methodExpression.Arguments.Any()
                && methodExpression.Arguments[0] is ConstantExpression constExpression
                && typeof(T) == typeof(IDictionary<string, object>))
            {
                name = constExpression.Value.ToString();
            }
            else
            {
                const string Format = "Expression '{0}' not supported.";
                string message = string.Format(Format, primaryKeyField);

                throw new ArgumentException(message, "Field");
            }

            cfg.ForeignKeys.Add(new ForeignKey
            {
                Source = typeof(O),
                PrimaryKeyName = name,
                ForeignKeyName = column
            });

            return cfg;
        }

        public static TableConfiguration<T> DeleteIfNotExisting<T>(this TableConfiguration<T> cfg)
        {
            cfg.DeleteIfNotExisting = true;

            return cfg;
        }
    }
}
