using System;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject.Tests.Helpers
{
    public static class ModuleBuilderProvider
    {
        private static readonly Lazy<ModuleBuilder> cache = new Lazy<ModuleBuilder>(() => Create(), true);

        public static ModuleBuilder Get() => cache.Value;

        public static ModuleBuilder Create(string suffix = null)
        {
            var assemblyName = new AssemblyName { Name = "TestAssembly" + suffix };
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var module = assemblyBuilder.DefineDynamicModule("TestModule" + suffix );
            return module;
        }
    }
}