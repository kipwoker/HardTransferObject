using System;
using System.Collections.Generic;
using System.Linq;

namespace HardTransferObject.Tests.Cases
{
    public class Model1InConverter : IConverter<Model1<string>, IModel1<string>>
    {
        public IModel1<string> Convert(Model1<string> @in)
        {
            return new Model1<string>
            {
                Prop = @in.Prop
            };
        }
    }

    public class Model1OutConverter : IConverter<IModel1<string>, Model1<string>>
    {
        public Model1<string> Convert(IModel1<string> @in)
        {
            return new Model1<string>
            {
                Prop = @in.Prop
            };
        }
    }

    public class Model4ToGeneratedClassConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (Model4<string>)@in;
            var converted = new C4<string>
            {
                Prop = casted.Prop,
                Str = casted.Str
            };

            return converted;
        }
    }

    public class InterfaceToModelConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (IModel1<string>) @in;
            var converted = new Model1<string>
            {
                Prop = casted.Prop,
                Number = casted.Number,
                Model2 = casted.Model2
            };
            return converted;
        }
    }

    public class InterfaceToNestedModelConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (IModel1<string>)@in;
            var converted = new Model11<string>
            {
                Prop = casted.Prop,
                Number = casted.Number,
                Model2 = (Model2)ConverterStorage.Instance.GetImplementation(typeof(IModel2), typeof(Model2)).Convert(casted.Model2)
            };
            return converted;
        }
    }

    public class ValueTypeToValueTypeConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (MyGenericStruct<Guid, IModel1<string>, string>)@in;
            var converted = new MyGenericStruct<Guid, Model1<string>, string>(
                casted.P1, 
                (Model1<string>)
                ConverterStorage
                    .Instance
                    .GetImplementation(typeof(IModel1<string>), typeof(Model1<string>))
                    .Convert(casted.P2),
                casted.P3
            );
            return converted;
        }
    }

    public class ObjectToValueTypeConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (Model<Guid, IModel1<string>>)@in;
            var converted = new KeyValuePair<Guid, Model1<string>>(casted.Id, (Model1<string>)ConverterStorage.Instance.GetImplementation(typeof(IModel1<string>), typeof(Model1<string>)).Convert(casted));
            return converted;
        }
    }

    public class CollectionToArrayWithDifferentItemTypesConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (IEnumerable<IModel2>) @in;
            var converted = CollectionConverter.Convert<IModel2, Model2>(casted);
            return converted;
        }
    }

    public class ArrayToCollectionWithDifferentItemTypesConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            IEnumerable<Model2> casted = (Model2[])@in;
            IEnumerable<IModel2> converted = CollectionConverter.Convert<Model2, IModel2>(casted);
            return converted;
        }
    }

    public class ArrayToCollectionWithSameItemTypesConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            return (IEnumerable<Model1<string>>)(Model1<string>[])@in;
        }
    }

    public class CollectionToArrayWithSameItemTypesConverter : IConverter<object, object>
    {
        public object Convert(object @in)
        {
            var casted = (IEnumerable<Model1<string>>)@in;
            var array = casted.ToArray();
            return array;
        }
    }

    public class TestProxyToBaseConverter : IConverter<ModelProxy<Model1<string>, Model2>, IModel<IModel1<string>, IModel2>>
    {
        public IModel<IModel1<string>, IModel2> Convert(ModelProxy<Model1<string>, Model2> proxy)
        {
            Console.WriteLine($"{proxy.GetType()} to {typeof(Model<IModel1<string>, IModel2>)}");
            return new Model<IModel1<string>, IModel2>
            {
                Id = proxy.Id,
                Str = proxy.Str,
                Collection1 = proxy.Collection1.Cast<IModel1<string>>().ToArray(),
                List1 = proxy.List1.Cast<IModel2>().ToList(),
                Array1 = proxy.Array1,
                Class1 = new Model1<IModel2>
                {
                    Prop = proxy.Class1.Prop
                },
                Collection2 = proxy.Collection2,
                Class2 = proxy.Class2,
                Dictionary1 = proxy.Dictionary1.ToDictionary(x => x.Key, x => (IModel1<string>)x.Value),
                Number = proxy.Number
            };
        }
    }

    public class TestBaseToProxyConverter : IConverter<IModel<IModel1<string>, IModel2>, ModelProxy<Model1<string>, Model2>>
    {
        public ModelProxy<Model1<string>, Model2> Convert(IModel<IModel1<string>, IModel2> @base)
        {
            return new ModelProxy<Model1<string>, Model2>
            {
                Id = @base.Id,
                Str = @base.Str,
                Number = @base.Number,
                Array1 = @base.Array1,
                Array2 = @base.Array2
                    .Select(item =>
                        new Model1<Model1<string>>
                        {
                            Prop = new Model1<string>
                            {
                                Prop = item.Prop.Prop
                            }
                        })
                    .ToArray(),
                Collection1 = @base.Collection1
                    .Select(item => new Model1<string>
                    {
                        Prop = item.Prop
                    })
                    .ToArray(),
                List1 = @base.List1
                    .Select(item => new Model2
                    {
                        Id = item.Id
                    })
                    .ToArray(),
                Class1 = new Model1<Model2>
                    {
                        Prop = new Model2
                        {
                            Id = @base.Class1.Prop.Id
                        }
                    },
                Collection2 = @base.Collection2
                    .Select(item => new Model2
                    {
                        Id = item.Id
                    })
                    .ToArray(),
                Class2 = @base.Class2,
                Dictionary1 = @base.Dictionary1
                    .Select(item => new KeyValuePair<Guid, Model1<string>>(
                        item.Key,
                        new Model1<string>
                        {
                            Prop = item.Value.Prop
                        }))
                    .ToArray()
            };
        }
    }
}