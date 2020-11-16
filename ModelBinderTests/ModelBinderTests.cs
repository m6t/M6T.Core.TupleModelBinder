using M6T.Core.TupleModelBinder;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace ModelBinderTests
{
    public class ModelBinderTests
    {
        [Fact]
        public void Compile_With_Tuple_Element_Names_Attribute()
        {
            var type = typeof(TupleMemberTestData);
            var prop = type.GetProperty("Value");
            var tupleElementNamesQ = prop.GetCustomAttributes(typeof(TupleElementNamesAttribute), true);
            if (tupleElementNamesQ.Length == 0)
            {
                Assert.True(false, "It seems ValueTuples no longer compile with TupleElementNamesAttribute");
                return;
            }

            Assert.True(true);
        }

        [Fact]
        public void TestTupleModelBinder()
        {
            var type = typeof(TupleMemberTestData);
            var prop = type.GetProperty("Value");
            var tupleType = prop.PropertyType;
            string body = @"
                            {
                              ""User"" : {
                                ""String"":""Test"",
                                ""Integer"":444,
                                ""Double"": 1.44,
                                ""Decimal"": 1.44
                              },
                              ""SomeData"" : ""Test String Root"",
                              ""NullCheck"": null,
                              ""BooleanCheck"": true,
                              ""ComplexNullCheck"":null
                            }";


            var tupleElementNames = (TupleElementNamesAttribute)prop.GetCustomAttributes(typeof(TupleElementNamesAttribute), true)[0];

            var result =
                ((TestUserClass User, string SomeData, string NullCheck, bool BooleanCheck, TestUserClass ComplexNullCheck))
                TupleModelBinder.ParseTupleFromModelAttributes(body, tupleElementNames, tupleType);

            Assert.NotNull(result.User);
            Assert.Equal("Test", result.User.String);
            Assert.Equal(444, result.User.Integer);
            Assert.Equal(1.44d, result.User.Double);
            Assert.Equal(1.44m, result.User.Decimal);

            Assert.Equal("Test String Root", result.SomeData);
            Assert.Null(result.NullCheck);
            Assert.Null(result.ComplexNullCheck);
            Assert.True(result.BooleanCheck);
        }

        [Fact]
        public void TestNullHandling()
        {
            var type = typeof(NullMemberTestData);
            var prop = type.GetProperty("Value");
            var tupleType = prop.PropertyType;

            string body = @"
                            {
                              ""SomeData"" : ""Test String Root"",
                            }";

            var tupleElementNames = (TupleElementNamesAttribute)prop.GetCustomAttributes(typeof(TupleElementNamesAttribute), true)[0];

            var result =
                ((string SomeData, string NullCheck, bool BooleanNullCheck, TestUserClass ComplexNullCheck))
                TupleModelBinder.ParseTupleFromModelAttributes(body, tupleElementNames, tupleType);

            Assert.Equal("Test String Root", result.SomeData);
            Assert.Null(result.NullCheck);
            Assert.Null(result.ComplexNullCheck);
            Assert.False(result.BooleanNullCheck); //boolean not accept null
        }

        /// <summary>
        /// Tests all possible guid formats as input
        /// </summary>
        [Fact]
        public void TestGuidHandling()
        {
            var type = typeof(GuidMemberTestData);
            var prop = type.GetProperty("Value");
            var tupleType = prop.PropertyType;
            var testGuid = Guid.Parse("728cb7a1-b8eb-471e-a90d-f17531baf918");

            List<string> guidFormattingTypes = new List<string>() {
                "N", //00000000000000000000000000000000
                "D", //00000000-0000-0000-0000-000000000000
                "B", //{00000000-0000-0000-0000-000000000000}
                "P", //(00000000-0000-0000-0000-000000000000)
                "X" //{0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
            };

            foreach (var guidFormat in guidFormattingTypes)
            {
                string expectedGuid = testGuid.ToString(guidFormat);
                string body = @"{""clientId"":""" + expectedGuid + @""",""name"":""Name1"",""email"":""email@email.com""}";

                var tupleElementNames = (TupleElementNamesAttribute)prop.GetCustomAttributes(typeof(TupleElementNamesAttribute), true)[0];

                var result =
                    ((Guid clientId, string name, string email, string NullCheck, bool BooleanNullCheck, TestUserClass ComplexNullCheck))
                    TupleModelBinder.ParseTupleFromModelAttributes(body, tupleElementNames, tupleType);

                var resultGuid = result.clientId.ToString(guidFormat);
                Assert.Equal(expectedGuid, resultGuid);
                Assert.Equal("Name1", result.name);
                Assert.Equal("email@email.com", result.email);

                Assert.Null(result.NullCheck);
                Assert.Null(result.ComplexNullCheck);
                Assert.False(result.BooleanNullCheck);
            }


        }
    }


    public class TupleMemberTestData
    {
        public (TestUserClass User, string SomeData, string NullCheck, bool BooleanCheck, TestUserClass ComplexNullCheck) Value { get; set; }
    }

    public class NullMemberTestData
    {
        public (string SomeData, string NullCheck, bool BooleanNullCheck, TestUserClass ComplexNullCheck) Value { get; set; }
    }

    public class GuidMemberTestData
    {
        public (Guid clientId, string name, string email, string NullCheck, bool BooleanNullCheck, TestUserClass ComplexNullCheck) Value { get; set; }
    }

    public class TestUserClass
    {
        public string String { get; set; }
        public int Integer { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }
}
