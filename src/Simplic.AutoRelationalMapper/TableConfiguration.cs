using System;
using System.Collections.Generic;

namespace Simplic.AutoRelationalMapper
{
    /// <inheritdoc />
    /// <typeparam name="T">Table/mapper type</typeparam>
    public class TableConfiguration<T> : ITableConfiguration
    {
        /// <inheritdoc />
        public Type Owner { get; set; }

        /// <inheritdoc />
        public Type Type => GetType().GenericTypeArguments[0];

        /// <inheritdoc />
        public string Table { get; set; }

        /// <inheritdoc />
        public IList<string> PrimaryKeys { get; } = new List<string>();

        /// <inheritdoc />
        public IList<ForeignKey> ForeignKeys { get; set; } = new List<ForeignKey>();

        /// <inheritdoc />
        public bool DeleteIfNotExisting { get; set; }
    }
}
