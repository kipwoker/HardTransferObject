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

            var sampleType = typeof(IModel<IModel1<string>, IModel2>);

            var proxyConverterFactory = new ProxyConverterFactory(moduleBuilder);
            var proxyProvider = new ProxyProvider(moduleBuilder, proxyConverterFactory);

            var proxyMapping = proxyProvider.GetOrCreate(sampleType);

            foreach (var type in proxyProvider.SelectAll())
            {
                Console.WriteLine($"{type.Key} -> {type.Value}");
            }

            Console.WriteLine("===================");
            Console.WriteLine($"{sampleType} -> {proxyMapping.ProxyType}");
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
            var proxyConverterFactory = new ProxyConverterFactory(moduleBuilder);
            var proxyProvider = new ProxyProvider(moduleBuilder, proxyConverterFactory);
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