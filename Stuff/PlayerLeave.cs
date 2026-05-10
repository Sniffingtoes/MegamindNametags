using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Nothing.Patches
{
    [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), "OnPlayerLeftRoom")]
    public class LeavePatch : MonoBehaviour
    {
        private static void Prefix(Player otherPlayer)
        {
            if (otherPlayer != PhotonNetwork.LocalPlayer && otherPlayer != a)
            {
                a = otherPlayer;
            }
        }

        private static Player a;
    }
}