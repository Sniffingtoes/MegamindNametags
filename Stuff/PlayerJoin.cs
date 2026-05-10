using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Nothing.Patches
{
    [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), "OnPlayerEnteredRoom")]
    public class JoinPatch : MonoBehaviour
    {
        private static void Prefix(Player newPlayer)
        {
            if (newPlayer != oldnewplayer)
            {
                oldnewplayer = newPlayer;
            }
        }

        private static Player oldnewplayer;
    }
}