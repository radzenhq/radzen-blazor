using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PropertyAccessTests
    {
        public partial class TestData
        {
            public string PROPERTY { get; set; }
            public string Property { get; set; }
        }

        class CountingHolder
        {
            public int InnerReads;
            private SimpleObject inner;
            public SimpleObject Inner { get { InnerReads++; return inner; } set { inner = value; } }
        }

        [Fact]
        public void NullSafeGetter_ReadsEachIntermediateOnce()
        {
            // A computed/side-effecting intermediate must be read once, not once for the null-check and again
            // for the leaf - otherwise a getter returning a value then null would throw.
            var holder = new CountingHolder { Inner = new SimpleObject { Prop1 = "X" } };
            var getter = PropertyAccess.NullSafeGetter<CountingHolder>("Inner.Prop1");

            var value = getter(holder);

            Assert.Equal("X", value);
            Assert.Equal(1, holder.InnerReads);
        }

        [Fact]
        public void Getter_With_DifferentTargetType()
        {
            var o = new TestData { Property = "test" };
            var getter = PropertyAccess.Getter<object, object>(nameof(TestData.Property), typeof(TestData));
            var value = getter(o);
            Assert.Equal(o.Property, value);
        }

        [Fact]
        public void Getter_With_Members_That_Differ_Only_In_Casing()
        {
            var o = new TestData { PROPERTY = nameof(TestData.PROPERTY), Property = nameof(TestData.Property) };
            var getter = PropertyAccess.Getter<TestData, string>(nameof(TestData.PROPERTY));
            var value = getter(o);
            Assert.Equal(nameof(TestData.PROPERTY), value);
        }

        [Fact]
        public void Getter_Resolves_Property_On_Simple_Object()
        {
            var o = new SimpleObject() { Prop1 = "TestString" };
            var getter = PropertyAccess.Getter<SimpleObject, string>("Prop1");
            var value = getter(o);
            Assert.Equal("TestString", value);
        }

        [Fact]
        public void Getter_Resolves_Property_On_Simple_Object_QueryableType()
        {
            var _data = new List<SimpleObject>()
            {
                new SimpleObject() { Prop1 = "TestString" },
            };

            Func<object, object> getter = PropertyAccess.Getter<object, object>("Prop1", typeof(SimpleObject));

            var value = getter(_data[0]);
            Assert.Equal("TestString", value);
        }

        [Fact]
        public void Getter_Resolves_Property_On_Nested_Object()
        {
            var o = new NestedObject() { Obj = new SimpleObject { Prop1 = "TestString" } };
            var getter = PropertyAccess.Getter<NestedObject, string>("Obj.Prop1");
            var value = getter(o);
            Assert.Equal("TestString", value);
        }

        [Fact]
        public void Getter_Resolves_Property_From_Array()
        {
            var o = new ArrayObject() { Values = new string[] { "1", "2", "3" } };
            var getter = PropertyAccess.Getter<ArrayObject, string>("Values[1]");
            var value = getter(o);
            Assert.Equal("2", value);
        }

        [Fact]
        public void Getter_Resolves_Property_From_Nested_Array()
        {
            var o = new NestedArrayObject() { Obj = new ArrayObject() { Values = new string[] { "1", "2", "3" } } };
            var getter = PropertyAccess.Getter<NestedArrayObject, string>("Obj.Values[2]");
            var value = getter(o);
            Assert.Equal("3", value);
        }

        [Fact]
        public void Getter_Resolves_Property_From_List()
        {
            var o = new ListObject() { Values = new List<string>() { "1", "2", "3" } };
            var getter = PropertyAccess.Getter<ListObject, string>("Values[1]");
            var value = getter(o);
            Assert.Equal("2", value);
        }

        public class SimpleObject
        {
            public string Prop1 { get; set; }
        }

        public class NestedObject
        {
            public SimpleObject Obj { get; set; }
        }

        public class ArrayObject
        {
            public string[] Values { get; set; }
        }

        public class NestedArrayObject
        {
            public ArrayObject Obj { get; set; }
        }

        public class ListObject
        {
            public List<string> Values { get; set; }
        }

        public class Order
        {
            public DateTime? OrderDate { get; set; }
        }

        [Fact]
        public void GetProperty_Should_Resolve_DescriptionProperty()
        {
            var descriptionProperty = PropertyAccess.GetProperty(typeof(ISimpleInterface), nameof(ISimpleInterface.Description));

            Assert.NotNull(descriptionProperty);
        }

        [Fact]
        public void GetProperty_Should_Resolve_NameProperty()
        {
            var nameProperty = PropertyAccess.GetProperty(typeof(ISimpleInterface), nameof(ISimpleInterface.Name));

            Assert.NotNull(nameProperty);
        }

        [Fact]
        public void GetProperty_Should_Resolve_IdProperty()
        {
            var idProperty = PropertyAccess.GetProperty(typeof(ISimpleInterface), nameof(ISimpleBaseInterface.Id));
            Assert.NotNull(idProperty);
        }

        [Fact]
        public void GetPropertyType_Resolves_NullableDateTime_Date()
        {
            var propertyType = PropertyAccess.GetPropertyType(typeof(Order), "OrderDate.Date");

            Assert.Equal(typeof(DateTime), propertyType);
        }

        interface ISimpleInterface : ISimpleNestedInterface
        {
            string Description { get; set; }
        }

        interface ISimpleNestedInterface : ISimpleBaseInterface
        {
            string Name { get; set; }
        }

        interface ISimpleBaseInterface
        {
            int Id { get; set; }
        }

        interface ISubInterface
        {
        }

        class SubModel : ISubInterface
        {
            public bool SubModelProperty { get; set; }
        }

        class ModelWithInterfaceProperty
        {
            public ISubInterface SubModelInstance { get; set; }
        }

        [Fact]
        public void GetValue_Resolves_Property_Through_Interface()
        {
            var model = new ModelWithInterfaceProperty
            {
                SubModelInstance = new SubModel { SubModelProperty = true }
            };

            var value = PropertyAccess.GetValue(model, "SubModelInstance.SubModelProperty");

            Assert.Equal(true, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference UseCollectibleType(string property)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"PropertyAccessTests_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
            var module = assembly.DefineDynamicModule("Main");
            var builder = module.DefineType("Item", TypeAttributes.Public);
            builder.DefineDefaultConstructor(MethodAttributes.Public);

            var getter = builder.DefineMethod("get_Name",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(string), Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldstr, "Item name");
            il.Emit(OpCodes.Ret);
            var name = builder.DefineProperty("Name", PropertyAttributes.None, typeof(string), null);
            name.SetGetMethod(getter);

            var type = builder.CreateType()!;
            var item = Activator.CreateInstance(type)!;
            PropertyAccess.GetItemProperty(item, property);

            return new WeakReference(type);
        }

        static void AssertCollected(WeakReference reference)
        {
            for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(reference.IsAlive);
        }

        [Fact]
        public void ItemPropertyCache_DoesNotRetainCollectibleTypes()
        {
            AssertCollected(UseCollectibleType("Name"));
        }

        [Fact]
        public void ItemPropertyCache_DoesNotRetainFailedLookupsForCollectibleTypes()
        {
            AssertCollected(UseCollectibleType("Missing"));
        }
    }
}
