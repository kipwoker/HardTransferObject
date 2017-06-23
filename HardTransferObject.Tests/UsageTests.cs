using System;
using System.Text;
using FluentAssertions;
using HardTransferObject.Tests.Cases;
using HardTransferObject.Tests.Helpers;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HardTransferObject.Tests
{
    [TestFixture]
    public class UsageTests
    {
        [Test]
        public void TestCreateProxyTypes()
        {
            var moduleBuilder = ModuleBuilderProvider.Get();

            var sampleType = typeof(IModel1<IModel3<IModel4<Guid>>>);

            var proxyProvider = new ProxyProvider(moduleBuilder);

            proxyProvider.Add(sampleType);

            foreach (var type in proxyProvider.TypeMap)
            {
                Console.WriteLine($"{type.Key.ToString().Replace(sampleType.Namespace + ".", "")} -> {type.Value.ToString().Replace(sampleType.Namespace + ".", "")}");
            }

            Console.WriteLine("===================");
            Console.WriteLine($"{sampleType.ToString().Replace(sampleType.Namespace + ".", "")} -> {proxyProvider.TypeMap[sampleType].ToString().Replace(sampleType.Namespace + ".", "")}");

            Console.WriteLine("===================");
            Console.WriteLine($"{sampleType.ToString().Replace(sampleType.Namespace + ".", "")} -> {proxyProvider.GetMappingChain(sampleType).ToString().Replace(sampleType.Namespace + ".", "")}");

            //var expectedKeys = new []
            //{
            //    typeof(IModel<IModel1<string>, IModel2>),
            //    typeof(Guid),
            //    typeof(string),
            //    typeof(int),
            //    typeof(IEnumerable<IModel1<string>>),
            //    typeof(IEnumerable<IModel2>),
            //    typeof(string[]),
            //    typeof(IModel1<IModel1<string>>[]),
            //    typeof(Dictionary<Guid, IModel1<string>>),
            //    typeof(List<IModel2>),
            //    typeof(Model1<IModel2>),
            //    typeof(Model2),
            //    typeof(IModel1<string>),
            //    typeof(IModel2),
            //};

            //mappings.Keys.ShouldAllBeEquivalentTo(expectedKeys);
        }

        [Test]
        public void TestConvertToBoth()
        {
            var sample = Samples.Model;

            var senderProxySerializer = CreateProxySerializer("Sender");
            var recipientProxySerializer = CreateProxySerializer("Recipient");

            var serializedProxy = senderProxySerializer.Serialize(sample);
            var expected = recipientProxySerializer.Deserialize<IModel<IModel1<string>, IModel2>>(serializedProxy);

            expected.ShouldBeEquivalentTo(sample);
        }

        private static ProxySerializer CreateProxySerializer(string suffix)
        {
            var moduleBuilder = ModuleBuilderProvider.Create(suffix);
            var proxyProvider = new ProxyProvider(moduleBuilder);
            var serializer = new JsonSerializer();
            var proxySerializer = new ProxySerializer(proxyProvider, serializer);
            return proxySerializer;
        }
    }

    public class JsonSerializer : ISerializer
    {
        public object Deserialize(byte[] data, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), type);
        }

        public byte[] Serialize(object data, Type type)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}