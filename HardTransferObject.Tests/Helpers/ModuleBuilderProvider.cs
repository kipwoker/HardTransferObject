using System;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject.Tests.Helpers
{
    public static class ModuleBuilderProvider
    {
        private static readonly Lazy<(AssemblyBuilder, ModuleBuilder)> cache = new Lazy<(AssemblyBuilder, ModuleBuilder)>(() => Create(), true);

        public static (AssemblyBuilder, ModuleBuilder) Get() => cache.Value;

        private static (AssemblyBuilder, ModuleBuilder) Create(string suffix = null)
        {
            var assemblyName = new AssemblyName { Name = "TestAssembly" + suffix };
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var module = assemblyBuilder.DefineDynamicModule("TestModule" + suffix, "test.dll");
            return (assemblyBuilder, module);
        }
    }
}