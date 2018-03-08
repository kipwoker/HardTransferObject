using System;
using System.Collections.Generic;
using System.Linq;

namespace HardTransferObject.Tests.Cases
{
    public interface IModel<T1, T2>
    {
        Guid Id { get; }
        string Str { get; }
        int Number { get; }
        IEnumerable<T1> Collection1 { get; }
        IEnumerable<T2> Collection2 { get; }
        string[] Array1 { get; }
        IModel1<T1>[] Array2 { get; }
        Dictionary<Guid, T1> Dictionary1 { get; }
        List<T2> List1 { get; }
        Model1<T2> Class1 { get; }
        Model2 Class2 { get; }
    }

    public class Model<T1, T2> : IModel<T1, T2>
    {
        public Guid Id { get; set; }
        public string Str { get; set; }
        public int Number { get; set; }
        public IEnumerable<T1> Collection1 { get; set; }
        public IEnumerable<T2> Collection2 { get; set; }
        public string[] Array1 { get; set; }
        public IModel1<T1>[] Array2 { get; set; }
        public Dictionary<Guid, T1> Dictionary1 { get; set; }
        public List<T2> List1 { get; set; }
        public Model1<T2> Class1 { get; set; }
        public Model2 Class2 { get; set; }
    }

    public interface IModelCollection<T1, T2>
    {
        IEnumerable<T1> Collection1 { get; }
        IEnumerable<T2> Collection2 { get; }
        Dictionary<Guid, T1> Dictionary1 { get; }
        List<T2> List1 { get; }
    }

    public class ModelCollection<T1, T2> : IModelCollection<T1, T2>
    {
        public IEnumerable<T1> Collection1 { get; set; }
        public IEnumerable<T2> Collection2 { get; set; }
        public Dictionary<Guid, T1> Dictionary1 { get; set; }
        public List<T2> List1 { get; set; }
    }

    public class ModelProxy<T1, T2>
    {
        public Guid Id { get; set; }
        public string Str { get; set; }
        public int Number { get; set; }
        public T1[] Collection1 { get; set; }
        public T2[] Collection2 { get; set; }
        public string[] Array1 { get; set; }
        public Model1<T1>[] Array2 { get; set; }
        public KeyValuePair<Guid, T1>[] Dictionary1 { get; set; }
        public T2[] List1 { get; set; }
        public Model1<T2> Class1 { get; set; }
        public Model2 Class2 { get; set; }
    }

    public interface IModel1<out T>
    {
        T Prop { get; }
        int Number { get; }
        IModel2 Model2 { get; }
    }

    public interface IModel3<out T>
    {
        int Number { get; }
        T Prop { get; }
    }

    public class Model3<T> : IModel3<T>
    {
        public int Number { get; set; }
        public T Prop { get; set; }
    }

    public interface IModel4<out T>
    {
        string Str { get; }
        T Prop { get; }
    }

    public class Model4<T> : IModel4<T>
    {
        public string Str { get; set; }
        public T Prop { get; set; }
    }

    public class C4<T> : IModel4<T>
    {
        public string Str { get; set; }
        public T Prop { get; set; }
    }


    public class Model1<T> : IModel1<T>
    {
        public T Prop { get; set; }
        public int Number { get; set; }
        public IModel2 Model2 { get; set; }
    }

    public interface IModel11<out T>
    {
        T Prop { get; }
        int Number { get; }
        Model2 Model2 { get; set; }
    }

    public class Model11<T> : IModel11<T>
    {
        public T Prop { get; set; }
        public int Number { get; set; }
        public Model2 Model2 { get; set; }
    }


    public interface IModel2
    {
        string Id { get; }
    }

    public class Model2 : IModel2
    {
        public string Id { get; set; }
        public int No;
    }

    public struct MyGenericStruct<T1, T2, T3>
    {
        public T1 P1 { get; }
        public T2 P2 { get; }
        public T3 P3 { get; }

        public MyGenericStruct(T1 p1, T2 p2, T3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

    }

    public static class Samples
    {
        public static readonly IModel1<IModel3<IModel4<Guid>>> Nested = new Model1<IModel3<IModel4<Guid>>>
        {
            Number = 111,
            Model2 = new Model2
            {
                Id = "m2"
            },
            Prop = new Model3<IModel4<Guid>>
            {
                Number = 333,
                Prop = new Model4<Guid>
                {
                    Str = "444",
                    Prop = Guid.NewGuid()
                }
            }
        };

        public static readonly IModel<IModel1<string>, IModel2> Model = new Model<IModel1<string>, IModel2>
        {
            Id = Guid.NewGuid(),
            Str = "String",
            Number = 2355982,
            Array1 = new[]
            {
                "arr1_1",
                "arr1_2",
                ""
            },
            Array2 = new IModel1<IModel1<string>>[]
            {
                new Model1<IModel1<string>> { Prop = new Model1<string> { Prop = "arr2_1"} },
                new Model1<IModel1<string>> { Prop = new Model1<string> { Prop = "arr2_2"} }
            },
            Class1 = new Model1<IModel2>
            {
                Prop = new Model2
                {
                    Id = "class1"
                }
            },
            Class2 = new Model2
            {
                Id = "class2"
            },
            Collection1 = new HashSet<IModel1<string>>(new[]
            {
                new Model1<string> { Prop = "coll1_1" },
                new Model1<string> { Prop = "coll1_2" }
            }),
            Collection2 = new Queue<IModel2>(new[]
            {
                new Model2 { Id = "coll2_1" },
                new Model2 { Id = "coll2_2" }
            }),
            Dictionary1 = new Dictionary<Guid, IModel1<string>>
            {
                { Guid.NewGuid(), new Model1<string> { Prop = "map1_1"} },
                { Guid.NewGuid(), new Model1<string> { Prop = "map1_2"} }
            },
            List1 = new List<IModel2>(new[]
            {
                new Model2 { Id = "list1" },
                new Model2 { Id = "list2" }
            })
        };

        public static readonly IModelCollection<int, string> EasyCollection = new ModelCollection<int, string>
        {
            Collection1 = new []{ 2, 3, 4 },
            Collection2 = new [] {"a", "db", "dc"},
            Dictionary1 = new Dictionary<Guid, int> {{Guid.NewGuid(), 77}, { Guid.NewGuid(), 78 } },
            List1 = new List<string>(new [] { "l1", "l2", "l3" })
        };

        public static readonly IModelCollection<int, IModel2> MediumCollection = new ModelCollection<int, IModel2>
        {
            Collection1 = new[] { 2, 3, 4 },
            Collection2 = new[] 
            {
                new Model2 { Id = "coll2_1" },
                new Model2 { Id = "coll2_2" }
            },
            Dictionary1 = new Dictionary<Guid, int> { { Guid.NewGuid(), 77 }, { Guid.NewGuid(), 78 } },
            List1 = new List<IModel2>(new[]
            {
                new Model2 { Id = "l1" },
                new Model2 { Id = "l2" }
            })
        };

        public static readonly ModelProxy<Model1<string>, Model2> Proxy = new ModelProxy<Model1<string>, Model2>
        {
            Id = Model.Id,
            Str = Model.Str,
            Number = Model.Number,
            Array1 = Model.Array1,
            Array2 = Model.Array2
                .Select(item =>
                    new Model1<Model1<string>>
                    {
                        Prop = new Model1<string> { Prop = item.Prop.Prop }
                    })
                .ToArray(),
            Collection1 = Model.Collection1.Select(item => new Model1<string> { Prop = item.Prop }).ToArray(),
            List1 = new List<Model2>(Model.List1.Select(item => new Model2 { Id = item.Id })).ToArray(),
            Class1 = new Model1<Model2> { Prop = new Model2 { Id = Model.Class1.Prop.Id } },
            Collection2 = Model.Collection2.Select(item => new Model2 { Id = item.Id }).ToArray(),
            Class2 = Model.Class2,
            Dictionary1 = Model.Dictionary1
            .ToDictionary(item => item.Key, item => new Model1<string> { Prop = item.Value.Prop }).ToArray()
        };
    }
}