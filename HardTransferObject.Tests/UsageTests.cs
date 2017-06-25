using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using GroBuf;
using GroBuf.DataMembersExtracters;
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

            foreach (var type in TypeExtensions.GetAll())
            {
                Console.WriteLine($"{type.Key.Name}:  {type.Value}");
            }

            foreach (var pair in proxyProvider.TypeMap)
            {
                Console.WriteLine($"{pair.Key.Name} -> {pair.Value.Name}");
            }

            foreach (var type in proxyProvider.TypeMap.Keys.Union(proxyProvider.TypeMap.Values))
            {
                Console.WriteLine(type.Name);
                foreach (var prop in type.GetProperties())
                {
                    Console.WriteLine($"  {prop.PropertyType.Name} {prop.Name}");
                }
            }

            Console.WriteLine("===================");
            Console.WriteLine($"{sampleType.ToString().Replace(sampleType.Namespace + ".", "")} -> {proxyProvider.TypeMap[sampleType].ToString().Replace(sampleType.Namespace + ".", "")}");

            Console.WriteLine("===================");
            Console.WriteLine($"{sampleType.ToString().Replace(sampleType.Namespace + ".", "")} -> {proxyProvider.GetMappingChain(sampleType).Last().ProxyType.ToString().Replace(sampleType.Namespace + ".", "")}");
        }
        
        [Test]
        public void TestConvertToBoth()
        {
            var sample = Samples.Nested;

            var senderProxySerializer = CreateProxySerializer("Sender");
            var recipientProxySerializer = CreateProxySerializer("Recipient");

            var serializedProxy = senderProxySerializer.Serialize(sample);
            Console.WriteLine(Encoding.UTF8.GetString(serializedProxy));

            var expected = recipientProxySerializer.Deserialize<IModel1<IModel3<IModel4<Guid>>>>(serializedProxy);

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

    public class GrobufSerializer : ISerializer
    {
        private readonly Serializer serializer = new Serializer(new PropertiesExtractor(), options: GroBufOptions.WriteEmptyObjects);

        public object Deserialize(byte[] data, Type type)
        {
            return serializer.Deserialize(type, data);
        }

        public byte[] Serialize(object data, Type type)
        {
            return serializer.Serialize(type, data);
        }
    }
}