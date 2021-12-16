using Moq;
using Simplic.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Simplic.AutoRelationalMapper.Test
{
    public class ObjectParserTest
    {
        [Fact]
        public void ParseObjects_Contact_Subtable()
        {
            var contact = GetTestObject();

            var sqlService = new Mock<ISqlService>().Object;
            var sqlColumnService = new Mock<ISqlColumnService>().Object;

            var consumer = new TestConsumer(sqlService, sqlColumnService);

            var stack = new Stack<object>();
            var queue = new Queue<object>();

            consumer.ParseObjects(contact, stack, queue);

            Assert.True(stack.Count == 7);
            Assert.True(queue.Count == 7);

            Assert.True(stack.OfType<ContactTestClass>().Count() == 1);
            Assert.True(stack.OfType<AddressTestClass>().Count() == 2);
            Assert.True(stack.OfType<PhoneNumberTestClass_Second>().Count() == 4);
        }

        class TestConsumer : RelationalMapperConsumer<ContactSavedCommand, ContactTestClass>
        {
            public TestConsumer(ISqlService sqlService, ISqlColumnService sqlColumnService) : base(sqlService, sqlColumnService)
            {
                MapTable<ContactTestClass>("IT_Contacts")
                    .PrimaryKey(x => x.Id);

                MapTable<AddressTestClass>("IT_Contacts_Address")
                    .PrimaryKey(x => x.Id)
                    .ForeignKey("ContactId", (ContactTestClass x) => x.Id);

                MapTable<PhoneNumberTestClass_Second>("IT_Contacts_PhoneNumber")
                    .PrimaryKey(x => x.Id)
                    .ForeignKey("ContactId", (ContactTestClass x) => x.Id)
                    .ForeignKey("ContactAddressId", (AddressTestClass x) => x.Id);
            }

            protected override ContactTestClass GetObject(ContactSavedCommand command) => command.Contact;
        }

        private ContactTestClass GetTestObject()
        {
            return new ContactTestClass
            {
                Id = Guid.Parse("460c91ee-70a2-4a92-83b3-554b0669e90d"),
                PrimaryAddress = new AddressTestClass
                {
                    Id = Guid.Parse("0144a985-6a1f-4670-aa85-c2483fcf7f9b"),
                    PhoneNumbers = new List<PhoneNumberTestClass_Second>
                    {
                        new PhoneNumberTestClass_Second
                        {
                            Id = Guid.Parse("6182964e-9911-4b4e-bdc0-91bfbbdcac49"),
                            Number = "+49 1234567"
                        },
                        new PhoneNumberTestClass_Second
                        {
                            Id = Guid.Parse("17132e06-8732-4adc-835b-3fdd9296a0ae"),
                            Number = "+49 987654"
                        }
                    }
                },
                Addresses = new List<AddressTestClass>
                {
                    new AddressTestClass
                    {
                        Id = Guid.Parse("0144a985-6a1f-4670-aa85-c2483fcf7f9b"),
                        PhoneNumbers = new List<PhoneNumberTestClass_Second>
                        {
                            new PhoneNumberTestClass_Second
                            {
                                Id = Guid.Parse("6182964e-9911-4b4e-bdc0-91bfbbdcac49"),
                                Number = "+49 1234567"
                            },
                            new PhoneNumberTestClass_Second
                            {
                                Id = Guid.Parse("17132e06-8732-4adc-835b-3fdd9296a0ae"),
                                Number = "+49 987654"
                            }
                        }
                    },
                    new AddressTestClass
                    {
                        Id = Guid.Parse("25dabb57-03d6-4b3c-98c6-56e3eccb3e2e"),
                        PhoneNumbers = new List<PhoneNumberTestClass_Second>
                        {
                            new PhoneNumberTestClass_Second
                            {
                                Id = Guid.Parse("a11cb5b6-fdd8-4d6a-b6b6-7f7380a22ee8"),
                                Number = "+49 1234567"
                            },
                            new PhoneNumberTestClass_Second
                            {
                                Id = Guid.Parse("99992e06-8732-4adc-835b-3fdd9296a0ae"),
                                Number = "+49 987654"
                            }
                        }
                    }
                }
            };
        }

        interface ContactSavedCommand
        {
            ContactTestClass Contact { get; set; }
        }

        class ContactTestClass
        {
            public Guid Id { get; set; }
            public AddressTestClass PrimaryAddress { get; set; }
            public IList<AddressTestClass> Addresses { get; set; }
        }

        class AddressTestClass
        {
            public Guid Id { get; set; }
            public IList<PhoneNumberTestClass_Second> PhoneNumbers { get; set; }
        }

        class PhoneNumberTestClass_Second
        {
            public Guid Id { get; set; }
            public string Number { get; set; }
        }
    }
}
