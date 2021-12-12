using System;
using System.Collections.Generic;

namespace Simplic.AutoRelationalMapper
{
    public class TableConfiguration<T> : ITableConfiguration
    {
        public Type Owner { get; set; }
        public Type Type => GetType().GenericTypeArguments[0];
        public string Table { get; set; }
        public IList<string> PrimaryKeys { get; } = new List<string>();
        public IList<ForeignKey> ForeignKeys { get; set; } = new List<ForeignKey>();
        public bool DeleteIfNotExisting { get; set; }
    }
}
