using System;
using System.Linq;
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
        private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

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