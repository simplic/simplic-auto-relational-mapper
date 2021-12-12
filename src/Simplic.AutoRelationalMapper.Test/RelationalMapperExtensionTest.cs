using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Simplic.AutoRelationalMapper.Test
{
    public class RelationalMapperExtensionTest
    {
        public interface TestClass
        {
            Guid Id { get; }
            TestClass_Second Second { get; set; }
        }

        public interface TestClass_Second
        {
            Guid Id { get; }
        }

        [Fact]
        public void RegisterTable_TestClass_AddPrimaryKey()
        {
            var table = new TableConfiguration<TestClass>()
                .PrimaryKey(x => x.Id);

            Assert.True(table.PrimaryKeys.Contains("Id"));
        }

        [Fact]
        public void RegisterTable_IDictionary_AddPrimaryKey()
        {
            var table = new TableConfiguration<IDictionary<string, object>>()
                .PrimaryKey(x => x["Guid"]);

            Assert.True(table.PrimaryKeys.Contains("Guid"));
        }

        [Fact]
        public void RegisterTable_TestClass_AddForeignKey()
        {
            var table = new TableConfiguration<TestClass_Second>()
                .ForeignKey("ParentId", (TestClass x) => x.Id);

            var fk = table.ForeignKeys;

            Assert.True(fk.Any());

            Assert.Equal(typeof(TestClass), fk[0].Source);
            Assert.Equal("Id", fk[0].PrimaryKeyName);
            Assert.Equal("ParentId", fk[0].ForeignKeyName);
        }
    }
}
