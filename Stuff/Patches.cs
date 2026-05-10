using HarmonyLib;
using Photon.Pun;

namespace MegamindPlugin
{
    [HarmonyPatch(typeof(VRRig), "OnDestroy")]
    internal static class VRRigDestroyPatch
    {
        static void Prefix(VRRig __instance)
        {
            PhotonView pv = __instance.GetComponent<PhotonView>();
            if (pv == null || pv.Owner == null) return;
            if (pv.IsMine) return;
        }
    }
}