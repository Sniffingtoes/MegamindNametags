using HarmonyLib;

namespace MegamindPlugin
{
    [HarmonyPatch(typeof(VRRig), "OnDestroy")]
    internal static class VRRigDestroyPatch
    {
        static void Prefix(VRRig __instance)
        {
            if (__instance == null) return;
        }
    }
}
