using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace MegamindPlugin
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class MegamindNametags : BaseUnityPlugin
    {
        public static MegamindNametags Instance;
        private TMP_FontAsset motdFont;
        public Sprite spriteMeta, spriteSteam, spriteUnknown;

        public ConfigEntry<float> FontSize;
        public ConfigEntry<float> OffsetX;
        public ConfigEntry<float> OffsetY;
        public ConfigEntry<float> OffsetZ;
        public ConfigEntry<float> FpsOffsetX;
        public ConfigEntry<float> FpsOffsetY;
        public ConfigEntry<float> FpsOffsetZ;
        public ConfigEntry<float> FpsFontSize;
        public ConfigEntry<int> NametagFontStyleIndex;
        public ConfigEntry<int> FpsFontStyleIndex;
        public ConfigEntry<float> NametagOutlineWidth;
        public ConfigEntry<float> FpsOutlineWidth;
        public ConfigEntry<float> FpsColorR;
        public ConfigEntry<float> FpsColorG;
        public ConfigEntry<float> FpsColorB;
        public ConfigEntry<float> FpsColorA;
        public ConfigEntry<int> UpdateEveryNFrames;
        public ConfigEntry<bool> NametagsEnabled;
        public ConfigEntry<bool> ShowFpsAboveHead;
        public ConfigEntry<bool> CustomTextColor;
        public ConfigEntry<float> CustomColorR;
        public ConfigEntry<float> CustomColorG;
        public ConfigEntry<float> CustomColorB;
        public ConfigEntry<float> CustomColorA;

        private bool _lastNametagsEnabled = true;
        private bool _lastFpsAboveHead = true;
        private float _localFpsSmoothed = 60f;

        void Awake()
        {
            Instance = this;
            BindConfig();
            new Harmony(PluginInfo.GUID).PatchAll(Assembly.GetExecutingAssembly());

            _lastNametagsEnabled = NametagsEnabled != null && NametagsEnabled.Value;
            _lastFpsAboveHead = ShowFpsAboveHead != null && ShowFpsAboveHead.Value;

            var uiGo = new GameObject("MegamindNametags_SettingsUI");
            uiGo.transform.SetParent(null, false);
            UnityEngine.Object.DontDestroyOnLoad(uiGo);
            uiGo.hideFlags = HideFlags.HideAndDontSave;
            uiGo.AddComponent<NametagSettingsGui>();
        }

        void BindConfig()
        {
            FontSize = Config.Bind("Nametags", "FontSize", 1.4f, new ConfigDescription("World-space nametag text size.", new AcceptableValueRange<float>(0.35f, 5f)));
            OffsetX = Config.Bind("Nametags", "OffsetX", 0f, "Local X offset from head.");
            OffsetY = Config.Bind("Nametags", "OffsetY", 0.45f, "Local Y offset from head.");
            OffsetZ = Config.Bind("Nametags", "OffsetZ", 0f, "Local Z offset from head.");
            NametagFontStyleIndex = Config.Bind("Nametags", "FontStyle", 0, new ConfigDescription("0 Normal, 1 Bold, 2 Italic, 3 Bold+Italic.", new AcceptableValueRange<int>(0, 3)));
            NametagOutlineWidth = Config.Bind("Nametags", "OutlineWidth", 0f, new ConfigDescription("TMP outline width (0 = off).", new AcceptableValueRange<float>(0f, 0.4f)));

            FpsOffsetX = Config.Bind("Fps", "OffsetX", 0f, "FPS label local X on head.");
            FpsOffsetY = Config.Bind("Fps", "OffsetY", 0.59f, "FPS label local Y.");
            FpsOffsetZ = Config.Bind("Fps", "OffsetZ", 0f, "FPS label local Z on head.");
            FpsFontSize = Config.Bind("Fps", "FontSize", 0.6f, new ConfigDescription("World-space FPS text size.", new AcceptableValueRange<float>(0.15f, 3f)));
            FpsFontStyleIndex = Config.Bind("Fps", "FontStyle", 0, new ConfigDescription("0 Normal, 1 Bold, 2 Italic, 3 Bold+Italic.", new AcceptableValueRange<int>(0, 3)));
            FpsOutlineWidth = Config.Bind("Fps", "OutlineWidth", 0f, new ConfigDescription("TMP outline on FPS label.", new AcceptableValueRange<float>(0f, 0.4f)));
            FpsColorR = Config.Bind("Fps", "ColorR", 0.82f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            FpsColorG = Config.Bind("Fps", "ColorG", 0.88f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            FpsColorB = Config.Bind("Fps", "ColorB", 0.96f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            FpsColorA = Config.Bind("Fps", "ColorA", 0.95f, new ConfigDescription("", new AcceptableValueRange<float>(0.2f, 1f)));

            UpdateEveryNFrames = Config.Bind("Nametags", "UpdateEveryNFrames", 30, new ConfigDescription("Refresh other players' nametags every N frames (higher = less CPU).", new AcceptableValueRange<int>(1, 120)));
            NametagsEnabled = Config.Bind("Nametags", "Enabled", true, "Show custom nametags on other players.");
            ShowFpsAboveHead = Config.Bind("Nametags", "ShowFpsAboveHead", true, "FPS above heads reads Gorilla's private VRRig.fps when present; otherwise your rig uses smoothed local FPS.");
            CustomTextColor = Config.Bind("Nametags", "CustomTextColor", false, "Use custom color instead of each player's gorilla color.");
            CustomColorR = Config.Bind("Nametags", "CustomColorR", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            CustomColorG = Config.Bind("Nametags", "CustomColorG", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            CustomColorB = Config.Bind("Nametags", "CustomColorB", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            CustomColorA = Config.Bind("Nametags", "CustomColorA", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.2f, 1f)));
        }

        public static FontStyles FontStyleFromIndex(int index)
        {
            switch (Mathf.Clamp(index, 0, 3))
            {
                case 1: return FontStyles.Bold;
                case 2: return FontStyles.Italic;
                case 3: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        static void ApplyTmpOutline(TextMeshPro tmp, float width)
        {
            if (tmp == null) return;
            float w = Mathf.Max(0f, width);
            tmp.outlineWidth = w;
            if (w > 1e-4f)
                tmp.outlineColor = new Color(0f, 0f, 0f, 0.75f);
        }

        void LateUpdate()
        {
            if (NametagsEnabled != null && NametagsEnabled.Value != _lastNametagsEnabled)
            {
                _lastNametagsEnabled = NametagsEnabled.Value;
                SetAllNametagRootsActive(_lastNametagsEnabled);
            }

            if (ShowFpsAboveHead != null && ShowFpsAboveHead.Value != _lastFpsAboveHead)
            {
                _lastFpsAboveHead = ShowFpsAboveHead.Value;
                SetAllHeadFpsRootsActive(_lastFpsAboveHead);
            }

            ResolveMotdFont();
            TickLocalFpsSmooth();

            bool fpsOn = ShowFpsAboveHead != null && ShowFpsAboveHead.Value;
            if (fpsOn)
            {
                foreach (VRRig rig in Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
                    UpdateHeadFpsDisplay(rig);
            }

            if (NametagsEnabled != null && !NametagsEnabled.Value)
                return;

            int interval = UpdateEveryNFrames != null ? Mathf.Clamp(UpdateEveryNFrames.Value, 1, 120) : 30;
            if (Time.frameCount % interval != 0)
                return;

            foreach (VRRig rig in Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
            {
                if (rig == null || rig.isOfflineVRRig || rig.isMyPlayer) continue;
                if (string.IsNullOrEmpty(rig.playerNameVisible) || rig.playerNameVisible == "Gorilla") continue;

                if (rig.head == null || rig.head.rigTarget == null) continue;

                UpdateNametag(rig);
            }
        }

        void ResolveMotdFont()
        {
            if (motdFont != null) return;
            GameObject motd = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
            if (motd == null) return;
            TextMeshPro tmp = motd.GetComponent<TextMeshPro>();
            if (tmp != null) motdFont = tmp.font;
        }

        void TickLocalFpsSmooth()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 1e-4f)
            {
                float inst = 1f / dt;
                _localFpsSmoothed = Mathf.Lerp(_localFpsSmoothed, inst, dt * 10f);
            }
        }

        static bool TryGetVRRigGameFps(VRRig rig, out int fps)
        {
            fps = 0;
            if (rig == null) return false;
            try
            {
                fps = Traverse.Create(rig).Field("fps").GetValue<int>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void SetAllNametagRootsActive(bool active)
        {
            foreach (VRRig rig in Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
            {
                if (rig == null || rig.isOfflineVRRig || rig.isMyPlayer) continue;
                if (rig.head == null || rig.head.rigTarget == null) continue;
                Transform t = rig.head.rigTarget.transform.Find("Megamind_Nametag");
                if (t != null) t.gameObject.SetActive(active);
            }
        }

        static void SetAllHeadFpsRootsActive(bool active)
        {
            foreach (VRRig rig in Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
            {
                if (rig == null || rig.isOfflineVRRig) continue;
                if (rig.head == null || rig.head.rigTarget == null) continue;
                Transform t = rig.head.rigTarget.transform.Find("Megamind_HeadFps");
                if (t != null) t.gameObject.SetActive(active);
            }
        }

        void UpdateHeadFpsDisplay(VRRig rig)
        {
            if (rig == null || rig.isOfflineVRRig || rig.head == null || rig.head.rigTarget == null) return;
            if (ShowFpsAboveHead == null || !ShowFpsAboveHead.Value) return;

            Transform head = rig.head.rigTarget.transform;
            Transform fpsTransform = head.Find("Megamind_HeadFps");
            TextMeshPro text;

            float fx = FpsOffsetX != null ? FpsOffsetX.Value : 0f;
            float fy = FpsOffsetY != null ? FpsOffsetY.Value : 0.59f;
            float fz = FpsOffsetZ != null ? FpsOffsetZ.Value : 0f;
            float fSize = FpsFontSize != null ? FpsFontSize.Value : 0.6f;

            if (fpsTransform == null)
            {
                GameObject root = new GameObject("Megamind_HeadFps");
                root.transform.SetParent(head, false);
                root.transform.localPosition = new Vector3(fx, fy, fz);
                root.AddComponent<CameraFollower>();
                text = root.AddComponent<TextMeshPro>();
                text.fontSize = fSize;
                text.alignment = TextAlignmentOptions.Center;
                if (motdFont != null) text.font = motdFont;
                fpsTransform = root.transform;
            }
            else
            {
                text = fpsTransform.GetComponent<TextMeshPro>();
                if (text == null) return;
            }

            fpsTransform.localPosition = new Vector3(fx, fy, fz);
            text.fontSize = fSize;
            text.fontStyle = FontStyleFromIndex(FpsFontStyleIndex != null ? FpsFontStyleIndex.Value : 0);
            text.color = new Color(
                FpsColorR != null ? FpsColorR.Value : 0.82f,
                FpsColorG != null ? FpsColorG.Value : 0.88f,
                FpsColorB != null ? FpsColorB.Value : 0.96f,
                FpsColorA != null ? FpsColorA.Value : 0.95f);
            ApplyTmpOutline(text, FpsOutlineWidth != null ? FpsOutlineWidth.Value : 0f);
            if (motdFont != null) text.font = motdFont;

            int shown;
            if (TryGetVRRigGameFps(rig, out int gameFps))
                shown = Mathf.Clamp(gameFps, 0, 999);
            else if (rig.isMyPlayer)
                shown = Mathf.Clamp(Mathf.RoundToInt(_localFpsSmoothed), 0, 999);
            else
                shown = -1;

            text.text = shown >= 0 ? $"{shown} FPS" : "— FPS";

            fpsTransform.gameObject.SetActive(true);
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
                root.transform.localPosition = new Vector3(OffsetX != null ? OffsetX.Value : 0f, OffsetY != null ? OffsetY.Value : 0.45f, OffsetZ != null ? OffsetZ.Value : 0f);
                root.AddComponent<CameraFollower>();
                text = root.AddComponent<TextMeshPro>();
                text.fontSize = FontSize != null ? FontSize.Value : 1.4f;
                text.alignment = TextAlignmentOptions.Center;
                if (motdFont != null) text.font = motdFont;
                tagTransform = root.transform;
            }
            else
            {
                text = tagTransform.GetComponent<TextMeshPro>();
                if (text == null) return;
            }

            tagTransform.localPosition = new Vector3(OffsetX != null ? OffsetX.Value : 0f, OffsetY != null ? OffsetY.Value : 0.45f, OffsetZ != null ? OffsetZ.Value : 0f);
            text.fontSize = FontSize != null ? FontSize.Value : 1.4f;
            text.fontStyle = FontStyleFromIndex(NametagFontStyleIndex != null ? NametagFontStyleIndex.Value : 0);
            ApplyTmpOutline(text, NametagOutlineWidth != null ? NametagOutlineWidth.Value : 0f);
            if (motdFont != null) text.font = motdFont;

            text.text = rig.playerNameVisible;

            if (CustomTextColor != null && CustomTextColor.Value)
            {
                text.color = new Color(
                    CustomColorR != null ? CustomColorR.Value : 1f,
                    CustomColorG != null ? CustomColorG.Value : 1f,
                    CustomColorB != null ? CustomColorB.Value : 1f,
                    CustomColorA != null ? CustomColorA.Value : 1f);
            }
            else if (rig.colorInitialized)
            {
                text.color = rig.playerColor;
            }
            else if (rig.mainSkin != null && rig.mainSkin.material != null)
            {
                text.color = rig.mainSkin.material.color;
            }
        }

        public void ResetEntry(ConfigEntry<bool> entry)
        {
            if (entry == null) return;
            entry.Value = (bool)entry.DefaultValue;
            Config.Save();
        }

        public void ResetEntry(ConfigEntry<float> entry)
        {
            if (entry == null) return;
            entry.Value = (float)entry.DefaultValue;
            Config.Save();
        }

        public void ResetEntry(ConfigEntry<int> entry)
        {
            if (entry == null) return;
            entry.Value = (int)entry.DefaultValue;
            Config.Save();
        }

        public void ResetAllSettingsToDefault()
        {
            NametagsEnabled.Value = (bool)NametagsEnabled.DefaultValue;
            FontSize.Value = (float)FontSize.DefaultValue;
            OffsetX.Value = (float)OffsetX.DefaultValue;
            OffsetY.Value = (float)OffsetY.DefaultValue;
            OffsetZ.Value = (float)OffsetZ.DefaultValue;
            NametagFontStyleIndex.Value = (int)NametagFontStyleIndex.DefaultValue;
            NametagOutlineWidth.Value = (float)NametagOutlineWidth.DefaultValue;
            FpsOffsetX.Value = (float)FpsOffsetX.DefaultValue;
            FpsOffsetY.Value = (float)FpsOffsetY.DefaultValue;
            FpsOffsetZ.Value = (float)FpsOffsetZ.DefaultValue;
            FpsFontSize.Value = (float)FpsFontSize.DefaultValue;
            FpsFontStyleIndex.Value = (int)FpsFontStyleIndex.DefaultValue;
            FpsOutlineWidth.Value = (float)FpsOutlineWidth.DefaultValue;
            FpsColorR.Value = (float)FpsColorR.DefaultValue;
            FpsColorG.Value = (float)FpsColorG.DefaultValue;
            FpsColorB.Value = (float)FpsColorB.DefaultValue;
            FpsColorA.Value = (float)FpsColorA.DefaultValue;
            UpdateEveryNFrames.Value = (int)UpdateEveryNFrames.DefaultValue;
            ShowFpsAboveHead.Value = (bool)ShowFpsAboveHead.DefaultValue;
            CustomTextColor.Value = (bool)CustomTextColor.DefaultValue;
            CustomColorR.Value = (float)CustomColorR.DefaultValue;
            CustomColorG.Value = (float)CustomColorG.DefaultValue;
            CustomColorB.Value = (float)CustomColorB.DefaultValue;
            CustomColorA.Value = (float)CustomColorA.DefaultValue;
            Config.Save();

            _lastNametagsEnabled = NametagsEnabled.Value;
            _lastFpsAboveHead = ShowFpsAboveHead.Value;
            SetAllNametagRootsActive(_lastNametagsEnabled);
            SetAllHeadFpsRootsActive(_lastFpsAboveHead);
        }
    }
}
