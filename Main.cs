using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace MegamindPlugin
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class MegamindNametags : BaseUnityPlugin
    {
        public static MegamindNametags Instance;
        private TMP_FontAsset motdFont;
        public Sprite spriteMeta, spritePc, spriteSteam, spriteUnknown;

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
        public ConfigEntry<bool> ShowPlatformText;
        public ConfigEntry<bool> ShowPlatformIcons;
        public ConfigEntry<int> PlatformIconLocation;
        public ConfigEntry<float> PlatformIconSize;
        public ConfigEntry<float> PlatformIconSpacing;
        public ConfigEntry<bool> CustomTextColor;
        public ConfigEntry<float> CustomColorR;
        public ConfigEntry<float> CustomColorG;
        public ConfigEntry<float> CustomColorB;
        public ConfigEntry<float> CustomColorA;

        private bool _lastNametagsEnabled = true;
        private bool _lastFpsAboveHead = true;
        private float _localFpsSmoothed = 60f;
        private VRRig[] _cachedRigs = new VRRig[0];
        private static FieldInfo _fpsField;
        private static bool _fpsFieldSearched;
        private static readonly DateTime SteamPaidLaunchDate = new DateTime(2023, 02, 06);
        private static readonly Dictionary<string, DateTime> AccountCreationCache = new Dictionary<string, DateTime>();
        private static readonly HashSet<string> AccountCreationPending = new HashSet<string>();
        private System.Collections.Generic.Dictionary<VRRig, TextMeshPro> _fpsTexts = new System.Collections.Generic.Dictionary<VRRig, TextMeshPro>();
        private System.Collections.Generic.Dictionary<VRRig, Transform> _fpsTransforms = new System.Collections.Generic.Dictionary<VRRig, Transform>();

        void Awake()
        {
            Instance = this;
            BindConfig();
            LoadPlatformSprites();
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
            ShowPlatformText = Config.Bind("Nametags", "ShowPlatformText", false, "Show a small platform text line on nametags.");
            ShowPlatformIcons = Config.Bind("Nametags", "ShowPlatformIcons", true, "Show platform icons to the left of nametags.");
            PlatformIconLocation = Config.Bind("Platform", "IconLocation", 1, new ConfigDescription("0 Top, 1 Left, 2 Right.", new AcceptableValueRange<int>(0, 2)));
            PlatformIconSize = Config.Bind("Platform", "IconSize", 0.05f, new ConfigDescription("World-space platform icon size.", new AcceptableValueRange<float>(0.02f, 0.8f)));
            PlatformIconSpacing = Config.Bind("Platform", "IconSpacing", 0.1f, new ConfigDescription("Space between platform icon and nametag.", new AcceptableValueRange<float>(0f, 1f)));
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
            Material material = tmp.fontMaterial;
            if (material == null || !material.HasProperty("_OutlineWidth"))
                return;

            float w = Mathf.Max(0f, width);
            if (tmp.outlineWidth != w)
            {
                tmp.outlineWidth = w;
                if (w > 1e-4f)
                    tmp.outlineColor = new Color(0f, 0f, 0f, 0.75f);
            }
        }

        void LoadPlatformSprites()
        {
            spriteMeta = LoadEmbeddedSprite("meta.png");
            spritePc = LoadEmbeddedSprite("pc.png");
            spriteSteam = LoadEmbeddedSprite("steam.png");
            spriteUnknown = LoadEmbeddedSprite("unknown.png");
        }

        static Sprite LoadEmbeddedSprite(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = null;
                string suffix = ".Resources." + fileName;
                string[] names = assembly.GetManifestResourceNames();

                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = names[i];
                        break;
                    }
                }

                if (resourceName == null)
                    return null;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;

                    byte[] data = new byte[stream.Length];
                    int read = stream.Read(data, 0, data.Length);
                    if (read <= 0)
                        return null;

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, data))
                    {
                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            catch
            {
                return null;
            }
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

            int interval = UpdateEveryNFrames != null ? Mathf.Clamp(UpdateEveryNFrames.Value, 1, 120) : 30;
            if (Time.frameCount % interval == 0 || _cachedRigs == null || _cachedRigs.Length == 0)
            {
                _cachedRigs = UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None);
            }

            bool fpsOn = ShowFpsAboveHead != null && ShowFpsAboveHead.Value;
            if (fpsOn)
            {
                for (int i = 0; i < _cachedRigs.Length; i++)
                    UpdateHeadFpsDisplay(_cachedRigs[i]);
            }

            if (NametagsEnabled != null && !NametagsEnabled.Value)
                return;

            if (Time.frameCount % interval != 0)
                return;

            for (int i = 0; i < _cachedRigs.Length; i++)
            {
                VRRig rig = _cachedRigs[i];
                if (rig == null || rig.isOfflineVRRig || rig.isMyPlayer) continue;
                if (string.IsNullOrEmpty(rig.playerNameVisible) || rig.playerNameVisible == "Gorilla") continue;

                if (rig.head == null || rig.head.rigTarget == null || rig.Creator == null) continue;

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
            if (!_fpsFieldSearched)
            {
                _fpsField = typeof(VRRig).GetField("fps", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _fpsFieldSearched = true;
            }
            if (_fpsField != null)
            {
                try
                {
                    fps = (int)_fpsField.GetValue(rig);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        static string GetOwnedCosmetics(VRRig rig)
        {
            try
            {
                object cosmetics = GetRigMemberValue(rig, "_playerOwnedCosmetics", "playerOwnedCosmetics", "concatStringOfCosmeticsAllowed");
                if (cosmetics == null)
                    return string.Empty;

                string text = cosmetics as string;
                if (text != null)
                    return text;

                IEnumerable enumerable = cosmetics as IEnumerable;
                if (enumerable == null)
                    return cosmetics.ToString();

                var parts = new List<string>();
                foreach (object item in enumerable)
                    if (item != null)
                        parts.Add(item.ToString());

                return string.Join("", parts.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        static object GetRigMemberValue(VRRig rig, params string[] names)
        {
            if (rig == null || names == null)
                return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = rig.GetType();

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], flags);
                if (field != null)
                    return field.GetValue(rig);

                PropertyInfo property = type.GetProperty(names[i], flags);
                if (property != null)
                    return property.GetValue(rig, null);
            }

            return null;
        }

        static bool RigBool(VRRig rig, bool fallback, params string[] names)
        {
            object value = GetRigMemberValue(rig, names);
            return value is bool ? (bool)value : fallback;
        }

        static int RigInt(VRRig rig, params string[] names)
        {
            object value = GetRigMemberValue(rig, names);
            if (value == null)
                return 0;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        static int GetCustomPropertyCount(VRRig rig)
        {
            try
            {
                return rig != null && rig.Creator != null
                    ? rig.Creator.GetPlayerRef().CustomProperties.Count
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        static DateTime? GetCachedCreationDate(string playFabId)
        {
            if (string.IsNullOrEmpty(playFabId)) return null;

            if (AccountCreationCache.TryGetValue(playFabId, out DateTime created))
                return created;

            if (!AccountCreationPending.Contains(playFabId))
            {
                AccountCreationPending.Add(playFabId);
                _ = FetchCreationDate(playFabId);
            }

            return null;
        }

        static async Task FetchCreationDate(string playFabId)
        {
            var tcs = new TaskCompletionSource<GetAccountInfoResult>();

            PlayFabClientAPI.GetAccountInfo(
                new GetAccountInfoRequest { PlayFabId = playFabId },
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.ErrorMessage)));

            try
            {
                GetAccountInfoResult result = await tcs.Task;
                if (result != null && result.AccountInfo != null)
                    AccountCreationCache[playFabId] = result.AccountInfo.Created;
            }
            catch
            {
            }
            finally
            {
                AccountCreationPending.Remove(playFabId);
            }
        }

        static string DetectPlatform(VRRig rig)
        {
            if (rig == null || rig.Creator == null)
                return "Unknown";

            string cosmetics = GetOwnedCosmetics(rig);
            int propCount = GetCustomPropertyCount(rig);

            if (!RigBool(rig, true, "initializedCosmetics", "cosmeticsInitialized"))
                return "Loading";

            if (cosmetics.Contains("S. FIRST LOGIN"))
                return "Steam";

            if (cosmetics.Contains("FIRST LOGIN") || cosmetics.Contains("game-purchase-bundle"))
                return "Meta PC";

            if (propCount > 1 || RigInt(rig, "currentRankedSubTierPC") > 0)
                return "PCVR";

            if (RigInt(rig, "currentRankedSubTierQuest") > 0)
                return "Meta";

            DateTime? created = GetCachedCreationDate(rig.Creator.UserId);
            if (created.HasValue && created.Value > SteamPaidLaunchDate)
                return "Meta";

            return "Unknown";
        }

        static string PlatformColor(string platform)
        {
            switch (platform)
            {
                case "Steam": return "#ffff00";
                case "Meta PC": return "#ffaa00";
                case "PCVR": return "#ff5555";
                case "Meta": return "#00ff66";
                case "Loading": return "#b0b0b0";
                default: return "#808080";
            }
        }

        Sprite PlatformSprite(string platform)
        {
            switch (platform)
            {
                case "Steam": return spriteSteam != null ? spriteSteam : spriteUnknown;
                case "PCVR": return spritePc != null ? spritePc : spriteUnknown;
                case "Meta":
                case "Meta PC": return spriteMeta != null ? spriteMeta : spriteUnknown;
                default: return spriteUnknown;
            }
        }

        string BuildNametagText(VRRig rig)
        {
            return rig != null ? rig.playerNameVisible : string.Empty;
        }

        Vector3 PlatformIconPosition(TextMeshPro text)
        {
            int location = PlatformIconLocation != null ? Mathf.Clamp(PlatformIconLocation.Value, 0, 2) : 1;
            float iconSize = PlatformIconSize != null ? Mathf.Clamp(PlatformIconSize.Value, 0.02f, 0.8f) : 0.05f;
            float spacing = PlatformIconSpacing != null ? Mathf.Clamp(PlatformIconSpacing.Value, 0f, 1f) : 0.1f;
            float halfTextWidth = 0.34f;

            if (text != null)
            {
                text.ForceMeshUpdate();
                halfTextWidth = Mathf.Max(0.02f, text.textBounds.size.x * 0.5f);
            }

            float sideOffset = halfTextWidth + (iconSize * 0.5f) + spacing;

            switch (location)
            {
                case 0:
                    return new Vector3(0f, TopPlatformIconLocalY(iconSize, spacing), 0f);
                case 2: return new Vector3(sideOffset, 0f, 0f);
                default: return new Vector3(-sideOffset, 0f, 0f);
            }
        }

        float TopPlatformIconLocalY(float iconSize, float spacing)
        {
            return (iconSize * 0.5f) + spacing;
        }

        float AdjustFpsYForTopPlatformIcon(float requestedFpsY)
        {
            bool nametagsOn = NametagsEnabled == null || NametagsEnabled.Value;
            bool platformIconsOn = ShowPlatformIcons != null && ShowPlatformIcons.Value;
            bool topIcon = PlatformIconLocation != null && Mathf.Clamp(PlatformIconLocation.Value, 0, 2) == 0;
            if (!nametagsOn || !platformIconsOn || !topIcon)
                return requestedFpsY;

            float nametagY = OffsetY != null ? OffsetY.Value : 0.45f;
            float iconSize = PlatformIconSize != null ? Mathf.Clamp(PlatformIconSize.Value, 0.02f, 0.8f) : 0.05f;
            float spacing = PlatformIconSpacing != null ? Mathf.Clamp(PlatformIconSpacing.Value, 0f, 1f) : 0.1f;
            float iconTopY = nametagY + TopPlatformIconLocalY(iconSize, spacing) + (iconSize * 0.5f);
            return Mathf.Max(requestedFpsY, iconTopY + spacing);
        }

        void UpdatePlatformIcon(VRRig rig, Transform tagTransform, TextMeshPro text, string platform)
        {
            if (tagTransform == null)
                return;

            Transform iconTransform = tagTransform.Find("Megamind_PlatformIcon");
            SpriteRenderer renderer = null;

            if (iconTransform == null)
            {
                GameObject icon = new GameObject("Megamind_PlatformIcon");
                icon.transform.SetParent(tagTransform, false);
                renderer = icon.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 20;
                iconTransform = icon.transform;
            }
            else
            {
                renderer = iconTransform.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = iconTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            if (ShowPlatformIcons == null || !ShowPlatformIcons.Value)
            {
                if (iconTransform.gameObject.activeSelf)
                    iconTransform.gameObject.SetActive(false);
                return;
            }

            Sprite sprite = PlatformSprite(platform);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            iconTransform.localPosition = PlatformIconPosition(text);
            iconTransform.localRotation = Quaternion.identity;
            iconTransform.localScale = Vector3.one * (PlatformIconSize != null ? Mathf.Clamp(PlatformIconSize.Value, 0.02f, 0.8f) : 0.05f);
            iconTransform.gameObject.SetActive(sprite != null);
        }

        static void SetAllNametagRootsActive(bool active)
        {
            foreach (VRRig rig in UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
            {
                if (rig == null || rig.isOfflineVRRig || rig.isMyPlayer) continue;
                if (rig.head == null || rig.head.rigTarget == null) continue;
                Transform t = rig.head.rigTarget.transform.Find("Megamind_Nametag");
                if (t != null) t.gameObject.SetActive(active);
            }
        }

        static void SetAllHeadFpsRootsActive(bool active)
        {
            foreach (VRRig rig in UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
            {
                if (rig == null || rig.isOfflineVRRig) continue;
                if (rig.head == null || rig.head.rigTarget == null) continue;
                Transform head = rig.head.rigTarget.transform;
                Transform t = head.Find("Megamind_HeadFps");
                if (t != null) t.gameObject.SetActive(active);

                Transform tag = head.Find("Megamind_Nametag");
                if (tag == null) continue;

                t = tag.Find("Megamind_HeadFps");
                if (t != null) t.gameObject.SetActive(active);
            }
        }

        void UpdateHeadFpsDisplay(VRRig rig)
        {
            if (rig == null || rig.isOfflineVRRig || rig.head == null || rig.head.rigTarget == null) return;
            if (ShowFpsAboveHead == null || !ShowFpsAboveHead.Value) return;

            Transform head = rig.head.rigTarget.transform;
            Transform tagTransform = head.Find("Megamind_Nametag");
            if (tagTransform == null)
                return;

            Transform fpsTransform;
            TextMeshPro text;

            if (!_fpsTransforms.TryGetValue(rig, out fpsTransform) || fpsTransform == null || fpsTransform.parent != tagTransform)
            {
                fpsTransform = tagTransform.Find("Megamind_HeadFps");
                if (fpsTransform == null)
                {
                    Transform oldHeadFps = head.Find("Megamind_HeadFps");
                    if (oldHeadFps != null)
                    {
                        oldHeadFps.SetParent(tagTransform, false);
                        fpsTransform = oldHeadFps;
                    }
                }
                
                float fx = FpsOffsetX != null ? FpsOffsetX.Value : 0f;
                float fy = FpsOffsetY != null ? FpsOffsetY.Value : 0.59f;
                float fz = FpsOffsetZ != null ? FpsOffsetZ.Value : 0f;
                float fSize = FpsFontSize != null ? FpsFontSize.Value : 0.6f;

                if (fpsTransform == null)
                {
                    GameObject root = new GameObject("Megamind_HeadFps");
                    root.transform.SetParent(tagTransform, false);
                    root.transform.localPosition = new Vector3(fx, fy, fz);
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

                CameraFollower follower = fpsTransform.GetComponent<CameraFollower>();
                if (follower != null)
                    UnityEngine.Object.Destroy(follower);

                _fpsTransforms[rig] = fpsTransform;
                _fpsTexts[rig] = text;
            }
            else
            {
                text = _fpsTexts[rig];
                if (text == null) return;
            }

            float fx2 = FpsOffsetX != null ? FpsOffsetX.Value : 0f;
            float fy2 = FpsOffsetY != null ? FpsOffsetY.Value : 0.59f;
            float fz2 = FpsOffsetZ != null ? FpsOffsetZ.Value : 0f;
            float fSize2 = FpsFontSize != null ? FpsFontSize.Value : 0.6f;
            float tagX = OffsetX != null ? OffsetX.Value : 0f;
            float tagY = OffsetY != null ? OffsetY.Value : 0.45f;
            float tagZ = OffsetZ != null ? OffsetZ.Value : 0f;

            fy2 = AdjustFpsYForTopPlatformIcon(fy2);
            fpsTransform.localPosition = new Vector3(fx2 - tagX, fy2 - tagY, fz2 - tagZ);
            text.fontSize = fSize2;
            text.fontStyle = FontStyleFromIndex(FpsFontStyleIndex != null ? FpsFontStyleIndex.Value : 0);
            text.color = new Color(
                FpsColorR != null ? FpsColorR.Value : 0.82f,
                FpsColorG != null ? FpsColorG.Value : 0.88f,
                FpsColorB != null ? FpsColorB.Value : 0.96f,
                FpsColorA != null ? FpsColorA.Value : 0.95f);
            ApplyTmpOutline(text, FpsOutlineWidth != null ? FpsOutlineWidth.Value : 0f);
            if (motdFont != null && text.font != motdFont) text.font = motdFont;

            int shown;
            if (TryGetVRRigGameFps(rig, out int gameFps))
                shown = Mathf.Clamp(gameFps, 0, 999);
            else if (rig.isMyPlayer)
                shown = Mathf.Clamp(Mathf.RoundToInt(_localFpsSmoothed), 0, 999);
            else
                shown = -1;

            string newText = shown >= 0 ? $"{shown} FPS" : "— FPS";
            if (text.text != newText) text.text = newText;

            if (!fpsTransform.gameObject.activeSelf) fpsTransform.gameObject.SetActive(true);
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
                text.richText = true;
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

            string platform = ShowPlatformIcons != null && ShowPlatformIcons.Value ? DetectPlatform(rig) : string.Empty;
            text.richText = true;
            text.text = BuildNametagText(rig);
            UpdatePlatformIcon(rig, tagTransform, text, platform);

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
            ShowPlatformText.Value = (bool)ShowPlatformText.DefaultValue;
            ShowPlatformIcons.Value = (bool)ShowPlatformIcons.DefaultValue;
            PlatformIconLocation.Value = (int)PlatformIconLocation.DefaultValue;
            PlatformIconSize.Value = (float)PlatformIconSize.DefaultValue;
            PlatformIconSpacing.Value = (float)PlatformIconSpacing.DefaultValue;
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
