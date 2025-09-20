using UnityEngine;
using HarmonyLib;

namespace fragmentMod
{
    public class ModLink : PhantomBrigade.Mods.ModLink
    {
        internal static string modId;
        internal static string modPath;
        public override void OnLoad(Harmony harmonyInstance)
        {
            modId = metadata.id;
            modPath = metadata.path;
            var patchAssembly = typeof(ModLink).Assembly;

            harmonyInstance.PatchAll(patchAssembly);
            Debug.Log($"Mod Loaded: " + modId + modPath);
        }
    }
}