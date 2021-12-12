using System;

namespace Simplic.AutoRelationalMapper
{
    /// <summary>
    /// Represents a foreign key. The foreign-key will connect two tables using its parent-type and
    /// column-name
    /// </summary>
    public class ForeignKey
    {
        /// <summary>
        /// Gets or sets the source table name.
        /// <para>
        /// Source to actual table relation. E.g. IT_Contacts -> IT_Contacts_Address (IT_Contacts = <see cref="Source"/>)
        /// </para>
        /// </summary>
        public Type Source { get; set; }

        /// <summary>
        /// Gets or sets the column name that is used as primary key
        /// </summary>
        public string PrimaryKeyName { get; set; }

        /// <summary>
        /// Gets or sets the column name that is used as the foreign key
        /// </summary>
        public string ForeignKeyName { get; set; }
    }

}
