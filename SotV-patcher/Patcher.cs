using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SotV_patcher
{
    public class Patcher
    {
        /* useless stuff, because patchers aren't supposed to meta plugins in bep 5 */
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Patcher Contract")]
        public static void Patch(AssemblyDefinition assembly) { }

        private static string[] pluginsToSkip =  new string[0];
        private static readonly string cachePath = Path.Combine(BepInEx.Paths.CachePath, "SotVPatcher.dat");

        private static readonly Dictionary<string, AssemblyDefinition> refMapper = new();
        private static readonly Dictionary<string, AssemblyDefinition> toPatch = new();
        private static readonly List<string> toCache = new();

        public const int version = 1;

        private static bool disableWrite = false;
        private static bool dumpDebugWrite = false;
        private static bool justFuckingYeetWhenDone = false;

        public static void Initialize()
        {
            Logger.Info("Loaded");

            ReadCache();
            refMapper.Add("Assembly-CSharp", AssemblyDefinition.ReadAssembly(Path.Combine(BepInEx.Paths.ManagedPath, "RoR2.dll")));
            var mmHookPath = Directory.EnumerateFiles(BepInEx.Paths.PluginPath, "MMHOOK_RoR2.dll", SearchOption.AllDirectories).FirstOrDefault();
            if (mmHookPath != null) {
                refMapper.Add("MMHOOK_Assembly-CSharp", AssemblyDefinition.ReadAssembly(mmHookPath));
            }
            refMapper.Add("UnityEngine.Networking", AssemblyDefinition.ReadAssembly(Path.Combine(BepInEx.Paths.ManagedPath, "com.unity.multiplayer-hlapi.Runtime.dll")));
            refMapper.Add("UnityEngine.Input", AssemblyDefinition.ReadAssembly(Path.Combine(BepInEx.Paths.ManagedPath, "UnityEngine.InputLegacyModule.dll")));
            GatherToBePatched();
            if (toPatch.Count > 0)
            {
                PatchAll();
            }
            else
            {
                Logger.Debug("Nothing to patch, all good!");
            }
        }

        public static void Finish()
        {
            if (toCache.Count() > 0) {
                toCache.InsertRange(0, pluginsToSkip);
                File.Delete(cachePath);
                File.WriteAllLines(cachePath, toCache);
            }

            if(justFuckingYeetWhenDone)
            {
                Environment.Exit(0);
            }
        }
        

        private static void ReadCache()
        {
            if (File.Exists(cachePath))
            {
                try
                {
                    pluginsToSkip = File.ReadAllLines(cachePath);
                }
                catch {
                    Logger.Warn("Couldn't read cache file, deleting it.");
                    try
                    {
                        File.Delete(cachePath);
                    } catch
                    {
                        Logger.Error("Couldn't delete unreadable cache file!");
                        throw;
                    }
                }
            }
        } 

        private static void GatherToBePatched()
        {
            var libraries = Directory.GetFiles(BepInEx.Paths.PluginPath, "*.dll", SearchOption.AllDirectories).Where(p => { string f = Path.GetFileNameWithoutExtension(p); return !f.Contains("SotVPatched"+version) && !f.StartsWith("MMHOOK_") && !pluginsToSkip.Any(p => p== f); });
            foreach (var lib in libraries)
            {
                var assembly = AssemblyDefinition.ReadAssembly(lib);
                bool hasToPatched = false;
                foreach (var m in assembly.Modules)
                {
                    if (m.AssemblyReferences.Select((r)=> r.Name).Any(refMapper.Keys.Contains)) {
                        
                        hasToPatched = true;
                        break;
                    }
                }

                if (hasToPatched)
                {
                    Logger.Debug($"\"{lib}\" contains patchable references.");
                    toPatch.Add(lib, assembly);
                } else
                {
                    Logger.Debug($"\"{lib}\" does not seem to contain anything. Adding to cache.");
                    toCache.Add(lib);
                }
            }   
        }

        private static void PatchAll()
        {
            foreach ((string filePath, AssemblyDefinition assembly) in toPatch)
            {

                foreach (var module in assembly.Modules)
                {
                    if (module == null) continue;
                    PatchModule(module);
                }


                
                int stage = 0;
                string outName = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".SotVPatched" + version + ".dll";
                bool deleteOut = false;
                try
                {
                    if (!disableWrite)
                        assembly.Write(outName);

                    if (dumpDebugWrite)
                    {
                        File.Delete(filePath + ".dump");
                        assembly.Write(Path.Combine(BepInEx.Paths.PluginPath, filePath + ".dump"));
                    }

                    assembly.Dispose();
                    stage++;
                    if (!disableWrite)
                        File.Move(filePath, filePath + ".sotvpatcher-backup");
                }
                catch
                {
                    
                    if(stage == 1)
                    {
                        Logger.Error($"Failed renaming old file \"{filePath}\".");
                    } else
                    {
                        Logger.Error($"Failed writing patched file \"{outName}\".");
                        deleteOut = true;
                    }
                    throw;
                }
                finally
                {
                    if (deleteOut)
                    {
                        try
                        {
                            File.Delete(outName);
                        }
                        catch
                        {
                            Logger.Error($"Couldn't even delete the failed file!");
                            throw;
                        }
                    }
                }
            }
        }

        private static void PatchModule(ModuleDefinition module)
        {
            Queue<TypeDefinition> Types = new(module.Types);
            TypeDefinition type = null;
            while(Types.Count > 0)
            {
                type = Types.Dequeue();
                
                foreach (var nestedType in type.NestedTypes)
                {
                    Types.Enqueue(nestedType);
                }

                List<CustomAttribute> customAttributes = new(type.CustomAttributes);

                var newBaseType = GetNewReference(type.BaseType);
                if(newBaseType != null)
                {
                    newBaseType = module.ImportReference(newBaseType);
                        
                    type.BaseType = newBaseType;
                }

                foreach (var @interface in type.Interfaces)
                {
                    var newInterfaceType = GetNewReference(@interface.InterfaceType);
                    if(newInterfaceType != null)
                    {
                        newInterfaceType = module.ImportReference(newInterfaceType);
                        @interface.InterfaceType = newInterfaceType;
                    }
                }

                foreach (var method in type.Methods)
                {
                    customAttributes.AddRange(method.CustomAttributes);

                    var newMethodReturnType = GetNewReference(method.ReturnType);
                    if (newMethodReturnType != null)
                    {
                        newMethodReturnType = module.ImportReference(newMethodReturnType);
                        method.ReturnType = newMethodReturnType;
                    }


                    foreach (var parameter in method.Parameters)
                    {
                        var newParameterType = GetNewReference(parameter.ParameterType);
                        if (newParameterType != null)
                        {
                            newParameterType = module.ImportReference(newParameterType);
                            parameter.ParameterType = newParameterType;
                        }
                    }

                    if (method.HasBody)
                    {
                        foreach (var instruction in method.Body.Instructions.Where(instruction => 
                            instruction.OpCode == OpCodes.Call ||
                            instruction.OpCode == OpCodes.Calli ||
                            instruction.OpCode == OpCodes.Callvirt ||
                            instruction.OpCode == OpCodes.Newobj ||
                            instruction.OpCode == OpCodes.Jmp
                            ))
                        {
                            if (instruction.Operand is MethodReference mRef)
                            {
                                var newMRef = GetNewMethodReference(mRef);
                                if (newMRef != null)
                                {
                                    newMRef = module.ImportReference(newMRef);
                                    instruction.Operand = newMRef;
                                }
                            }
                        }

                        foreach (var instruction in method.Body.Instructions.Where(instruction =>
                            instruction.OpCode == OpCodes.Ldfld ||
                            instruction.OpCode == OpCodes.Ldflda ||
                            instruction.OpCode == OpCodes.Ldsfld ||
                            instruction.OpCode == OpCodes.Ldsflda ||
                            instruction.OpCode == OpCodes.Stsfld ||
                            instruction.OpCode == OpCodes.Stfld
                            ))
                        {
                            if (instruction.Operand is FieldReference fRef)
                            {
                                var newfRef = GetNewFieldReference(fRef);
                                if (newfRef != null)
                                {
                                    newfRef = module.ImportReference(newfRef);
                                    instruction.Operand = newfRef;
                                }
                            }
                        }

                        foreach (var variable in method.Body.Variables)
                        {
                            var newVariableType = GetNewReference(variable.VariableType);
                            if (newVariableType != null)
                            {
                                newVariableType = module.ImportReference(newVariableType);
                                variable.VariableType = newVariableType;
                            }
                        }
                    }
                }

                foreach (var field in type.Fields)
                {
                    customAttributes.AddRange(field.CustomAttributes);

                    var newFieldType = GetNewReference(field.FieldType);
                    if (newFieldType != null)
                    {
                        newFieldType = module.ImportReference(newFieldType);
                        field.FieldType = newFieldType;
                    }
                }

                foreach (var @event in type.Events) 
                {
                    customAttributes.AddRange(@event.CustomAttributes);

                    var newEventType = GetNewReference(@event.EventType);
                    if (newEventType != null)
                    {
                        newEventType = module.ImportReference(newEventType);
                        @event.EventType = newEventType;
                    }
                }

                foreach (var property in type.Properties)
                {
                    customAttributes.AddRange(property.CustomAttributes);

                    var newPropType = GetNewReference(property.PropertyType);
                    if (newPropType != null)
                    {
                        newPropType = module.ImportReference(newPropType);
                        property.PropertyType = newPropType;
                    }
                }

                foreach (var customAttribute in customAttributes)
                {
                    var CAType = GetNewReference(customAttribute.AttributeType);
                    if (CAType != null)
                    {
                        var newCActor = GetNewMethodReference(customAttribute.Constructor);
                        if (newCActor != null)
                        {
                            newCActor = module.ImportReference(newCActor);
                            customAttribute.Constructor = newCActor;
                        }
                    }

                }
            }
        }

        private static TypeReference GetNewReference(TypeReference old)
        {
            if (old == null)
            {
                return null;
            }

            string scope = old.Scope.Name;

            if (old.FullName == "UnityEngine.Input")
            {
                scope = old.FullName;
            }

            if (refMapper.ContainsKey(scope))
            {
                var types = refMapper[scope].Modules.SelectMany((mod) => mod.GetTypes());
                var debugTypes = types.ToList();
                return types.FirstOrDefault((tref) => tref.FullName == old.FullName);
            }

            return null;
        }

        private static MethodReference GetNewMethodReference(MethodReference old)
        {
            if (old == null)
            {
                return null;
            }

            var newRef = GetNewReference(old.DeclaringType);
            if (newRef != null)
            {
                var methods = newRef.Resolve().Methods.Where(md => 
                    md.FullName == old.FullName &&
                    md.ReturnType.Name == old.ReturnType.Name &&
                    md.Parameters.Count() == old.Parameters.Count());

                var pNames = old.Parameters.Select(p => p.ParameterType.Name);

                return methods.FirstOrDefault(md => md.Parameters.Select(p => p.ParameterType.Name).IsEqual(pNames));

            }
            return null;
        }

        private static FieldReference GetNewFieldReference(FieldReference old)
        {
            if (old == null)
            {
                return null;
            }

            var newRef = GetNewReference(old.DeclaringType);
            if (newRef != null)
            {
                var newField = newRef.Resolve().Fields.FirstOrDefault(fd => fd.Name == old.Name);
                return newField;
            }
            return null;
        }
    }
}
