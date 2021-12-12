using System;
using System.Collections.Generic;

namespace Simplic.AutoRelationalMapper
{
    /// <summary>
    /// Reprents a table-object configuration, this object will be used
    /// to configurate the mapper.
    /// </summary>
    public interface ITableConfiguration
    {
        /// <summary>
        /// Gets or sets the type, that is used for storing the data
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets or sets the table name, to store the data in
        /// </summary>
        string Table { get; set; }

        /// <summary>
        /// Gets a list of available primary keys
        /// </summary>
        IList<string> PrimaryKeys { get; }

        /// <summary>
        /// Gets a list of available foreign keys
        /// </summary>
        IList<ForeignKey> ForeignKeys { get; }

        /// <summary>
        /// 
        /// </summary>
        bool AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets an owner type. The owner type will be used for undefined types,
        /// e.g. when data are stored in <see cref="IDictionary" /> 
        /// </summary>
        Type Owner { get; set; }
    }
}
