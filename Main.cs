using BepInEx;
using HarmonyLib;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using Photon.Pun;

namespace MegamindPlugin
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class MegamindNametags : BaseUnityPlugin
    {
        public static MegamindNametags Instance;
        private TMP_FontAsset motdFont;
        public Sprite spriteMeta, spriteSteam, spriteUnknown;

        void Awake()
        {
            Instance = this;
            new Harmony(PluginInfo.GUID).PatchAll(Assembly.GetExecutingAssembly());
        }

        void LateUpdate()
        {
            if (Time.frameCount % 30 == 0)
            {
                if (motdFont == null)
                {
                    GameObject motd = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
                    if (motd != null)
                    {
                        TextMeshPro tmp = motd.GetComponent<TextMeshPro>();
                        if (tmp != null) motdFont = tmp.font;
                    }
                }

                foreach (VRRig rig in GameObject.FindObjectsOfType<VRRig>())
                {
                    if (rig == null || rig.isOfflineVRRig || rig.isMyPlayer) continue;
                    if (string.IsNullOrEmpty(rig.playerNameVisible) || rig.playerNameVisible == "Gorilla") continue;

                    if (rig.head == null || rig.head.rigTarget == null) continue;

                    UpdateNametag(rig);
                }
            }
        }

        public void UpdateNametag(VRRig rig)
        {
            if (rig == null || rig.head == null || rig.head.rigTarget == null) return;

            Transform head = rig.head.rigTarget.transform;
            Transform tagTransform = head.Find("Megamind_Nametag");
            TextMeshPro text;

            if (tagTransform == null)
            {
                GameObject root = new GameObject("Megamind_Nametag");
                root.transform.SetParent(head, false);
                root.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                root.AddComponent<CameraFollower>();
                text = root.AddComponent<TextMeshPro>();
                text.fontSize = 1.4f;
                text.alignment = TextAlignmentOptions.Center;
                if (motdFont != null) text.font = motdFont;
                tagTransform = root.transform;
            }
            else
            {
                text = tagTransform.GetComponent<TextMeshPro>();
                if (text == null) return;
            }

            text.text = rig.playerNameVisible;

            if (rig.colorInitialized)
            {
                text.color = rig.playerColor;
            }
            else if (rig.mainSkin != null && rig.mainSkin.material != null)
            {
                text.color = rig.mainSkin.material.color;
            }
        }
    }
}