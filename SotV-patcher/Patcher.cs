using Mono.Cecil;
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

        public static void Initialize()
        {
            Logger.Info("Loaded");

            ReadCache();
            /*
            refMapper.Add("Assembly-CSharp", "RoR2");
            refMapper.Add("MMHOOK_Assembly-CSharp", "MMHOOK_RoR2");
            */
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
            //WriteCache();
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
            DateTime sotvUpdate = new(2022, 3, 1);
            var libraries = Directory.GetFiles(BepInEx.Paths.PluginPath, "*.dll", SearchOption.AllDirectories).Where(p => { string f = Path.GetFileNameWithoutExtension(p); return !f.Contains("SotVPatched") && !f.StartsWith("MMHOOK_") && !pluginsToSkip.Contains(f) && File.GetLastWriteTime(f) < sotvUpdate; });
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
                string outName = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)) + ".SotVPatched.dll";
                try
                {
                    assembly.Write(outName);
                    assembly.Dispose();
                    stage++;
                    //File.Move(filePath, filePath + ".sotvpatcher-backup");
                }
                catch
                {
                    
                    if(stage == 1)
                    {
                        Logger.Error($"Failed renaming old file \"{filePath}\".");
                    } else
                    {
                        Logger.Error($"Failed writing patched file \"{outName}\".");
                    }
                    throw;
                }
            }
        }

        private static void PatchModule(ModuleDefinition module)
        {
            List<AssemblyDefinition> scopesAdded = new();
            foreach (var type in module.Types)
            {
                //Logger.Debug($"Examing type: {type.Name} with basetype: {type.BaseType?.Log()}");
                var bType = type.BaseType;
                if (bType != null && refMapper.ContainsKey(bType.Scope.Name))
                {
                    var typedefs = refMapper[bType.Scope.Name].Modules.SelectMany((mod) => mod.GetTypes());
                    var newRef = typedefs.FirstOrDefault((tref) => tref.FullName == bType.FullName);
                    if(newRef != null)
                    {
                        /*
                        if (!scopesAdded.Contains(newRef.Module.Assembly))
                        {
                            scopesAdded.Add(newRef.Module.Assembly);
                            module.AssemblyReferences.Add(AssemblyNameReference.Parse(newRef.Module.Assembly.FullName));
                        }*/
                        Logger.Debug($"{type.Log()} : {bType.Log()} => {newRef.Log()}");

                        module.ImportReference(newRef);
                        
                        type.BaseType = newRef;                       
                        
                    }
                    else
                    {
                        Logger.Debug($"newRef for {bType.Log()} not found");
                    }
                }
            }
        }
    }
}
