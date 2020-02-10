using M6T.Core.TupleModelBinder;
using System;
using System.Runtime.CompilerServices;
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
    }


    public class TupleMemberTestData
    {
        public (TestUserClass User, string SomeData, string NullCheck, bool BooleanCheck, TestUserClass ComplexNullCheck) Value { get; set; }
    }

    public class TestUserClass
    {
        public string String { get; set; }
        public int Integer { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }
}
