using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

//This file is NOT compiled!

namespace SotV_patcher
{
    internal class SotVCoreModulePatcher
    {
        public static IEnumerable<string> TargetDLLs { get; } = new string[] {"UnityEngine.CoreModule.dll"};

        public static void Patch(AssemblyDefinition assembly) {
            var inputAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(BepInEx.Paths.ManagedPath, "UnityEngine.InputLegacyModule.dll"));
            var inputTypeRef = assembly.MainModule.ImportReference(inputAssembly.MainModule.GetType("UnityEngine.Input"));

            var typeForwardRef = assembly.MainModule.ImportReference(typeof(TypeForwardedToAttribute).GetConstructors().First());
            var typeTypeRef = assembly.MainModule.ImportReference(typeof(System.Type));


            var ca = new CustomAttribute(typeForwardRef);

            var arg = new CustomAttributeArgument(typeTypeRef, inputTypeRef);
            ca.ConstructorArguments.Add(arg);

            assembly.CustomAttributes.Add(ca);
        }
    }
}
