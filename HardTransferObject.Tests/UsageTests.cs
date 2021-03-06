﻿using System;
using System.Linq;
using System.Reflection.Emit;
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
        private ProxySerializer proxySerializer;
        private JsonSerializer jsonSerializer;
        private GrobufSerializer grobufSerializer;
        private ProxyProvider proxyProvider;
        private AssemblyBuilder assemblyBuilder;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            (var assemblyBuilder, var moduleBuilder) = ModuleBuilderProvider.Get();
            proxyProvider = new ProxyProvider(moduleBuilder);
            jsonSerializer = new JsonSerializer();
            grobufSerializer = new GrobufSerializer();
            proxySerializer = new ProxySerializer(proxyProvider, jsonSerializer);
            this.assemblyBuilder = assemblyBuilder;
        }

        [Test]
        public void TestCreateProxyTypes()
        {
            var sampleType = typeof(IModel<IModel1<string>, IModel2>);

            proxyProvider.Declare(sampleType);

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
                if (type.IsIEnumerableInterface() || type.IsIEnumerableInterfaceImplementation())
                {
                    continue;
                }

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
        public void TestConvertToBothForNestedLevel1()
        {
            IModel3<IModel2> sample = new Model3<IModel2>
            {
                Number = 123,
                Prop = new Model2
                {
                    Id = "strstr",
                    No = 3242423
                }
            };

            TestConvertToBoth(sample);
        }

        [Test]
        public void TestConvertToBothForNestedLevel2()
        {
            TestConvertToBoth(Samples.Nested.Prop);
        }

        [Test]
        public void TestConvertToBothForNestedLevel3()
        {
            TestConvertToBoth(Samples.Nested);
        }

        [Test]
        public void TestConvertToBothForEasyCollection()
        {
            TestConvertToBoth(Samples.EasyCollection);
        }

        [Test]
        public void TestConvertToBothForMediumCollection()
        {
            TestConvertToBoth(Samples.MediumCollection);
        }

        [Test]
        public void TestConvertToBothForSimpleCollection()
        {
            TestConvertToBoth(Samples.SimpleCollection);
        }

        [Test]
        public void TestConvertToBothForComplexModel()
        {
            TestConvertToBoth(Samples.Model);
        }

        private void TestConvertToBoth<T>(T sample)
        {
            var serializedProxy = proxySerializer.Serialize(sample);
            
            Console.WriteLine(Encoding.UTF8.GetString(serializedProxy));

            var expected = proxySerializer.Deserialize<T>(serializedProxy);
            //assemblyBuilder.Save("test.dll");

            expected.ShouldBeEquivalentTo(sample);
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