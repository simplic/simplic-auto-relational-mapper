using System;

namespace Simplic.AutoRelationalMapper
{
    public class ForeignKey
    {
        public Type Source { get; set; }
        public string PrimaryKeyName { get; set; }
        public string ForeignKeyName { get; set; }
    }

}
