using System;
using System.Collections.Generic;

namespace Simplic.AutoRelationalMapper
{
    public interface ITableConfiguration
    {
        Type Owner { get; set; }
        Type Type { get; }
        string Table { get; set; }
        IList<string> PrimaryKeys { get; }
        IList<ForeignKey> ForeignKeys { get; set; }
        bool DeleteIfNotExisting { get; set; }
    }
}
