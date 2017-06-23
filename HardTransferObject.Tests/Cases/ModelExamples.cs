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
    }

    public interface IModel3<out T>
    {
        int Number { get; }
        T Prop { get; }
    }

    public interface IModel4<out T>
    {
        string Str { get; }
        T Prop { get; }
    }

    public class Model1<T> : IModel1<T>
    {
        public T Prop { get; set; }
    }

    public interface IModel2
    {
        string Id { get; }
    }

    public class Model2 : IModel2
    {
        public string Id { get; set; }
    }

    public static class Samples
    {
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