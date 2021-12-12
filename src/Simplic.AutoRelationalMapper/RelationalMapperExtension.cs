using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simplic.AutoRelationalMapper
{
    /// <summary>
    /// Contains a set of methods for creating <see cref="TableConfiguration{T}"/>
    /// </summary>
    public static class RelationalMapperExtension
    {
        /// <summary>
        /// Add a new primary-key to a table configuration
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <typeparam name="F">Key type</typeparam>
        /// <param name="cfg">Table configuration instance</param>
        /// <param name="primaryKeyField">Primary key expression</param>
        /// <returns>Table configuration instance (for method chaining)</returns>
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

        /// <summary>
        /// Add a new foreign-key to a table configuration
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <typeparam name="O">Owner type</typeparam>
        /// <typeparam name="F">Key type</typeparam>
        /// <param name="cfg">Table configuration instance</param>
        /// <param name="primaryKeyField">Foreign key expression</param>
        /// <returns>Table configuration instance (for method chaining)</returns>
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

        /// <summary>
        /// Enables the auto-delete option in a table configuration
        /// <para>
        /// If auto delete is enabled, missing sub-data, e.g. of an collection will be deleted
        /// automatically. A foreign-reference is required for this function
        /// </para>
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="cfg">Table configuration instance</param>
        /// <returns>Table configuration instance (for method chaining)</returns>
        public static TableConfiguration<T> AutoDelete<T>(this TableConfiguration<T> cfg)
        {
            cfg.AutoDelete = true;

            return cfg;
        }
    }
}
