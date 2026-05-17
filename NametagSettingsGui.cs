using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegamindPlugin
{
    public class NametagSettingsGui : MonoBehaviour
    {
        private const int WindowId = 0x4d454741;
        private const float HeaderH = 84f;
        private const float NavW = 196f;
        private Rect _windowRect = new Rect(40f, 40f, 820f, 620f);
        private Vector2 _scrollPos;
        private int _category;
        private int _settingsSub;
        private bool _showMenu;
        private bool _stylesInitialized;
        private GUIStyle _titleStyle, _subtleStyle, _hintStyle, _navStyle, _activeNavStyle, _rowStyle, _headerBtnStyle, _tinyBtnStyle, _segmentStyle;
        private GUIStyle _scrollbarStyle, _scrollThumbStyle, _hSliderStyle, _hThumbStyle;
        private Texture2D _windowTex, _panelTex, _headerTex, _contentWellTex, _activeNavTex, _accentTex, _accentSoftTex;
        private Texture2D _hoverTex, _lineTex, _borderTex, _scrollTrackTex, _scrollThumbTex, _scrollThumbHoverTex;
        private Texture2D _sliderTrackTex, _sliderThumbTex, _sliderThumbHoverTex;

        static MegamindNametags P => MegamindNametags.Instance;

        void Update()
        {
            if (P == null) return;

            if (WasMenuTogglePressed())
                ToggleMenu();
        }

        static bool WasMenuTogglePressed()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            return k.f6Key.wasPressedThisFrame
                || k.backslashKey.wasPressedThisFrame
                || k.backquoteKey.wasPressedThisFrame
                || k.insertKey.wasPressedThisFrame;
        }

        void ToggleMenu()
        {
            _showMenu = !_showMenu;
            Cursor.visible = _showMenu;
            Cursor.lockState = _showMenu ? CursorLockMode.None : CursorLockMode.Locked;
        }

        void OnGUI()
        {
            if (P == null) return;
            if (!_showMenu) return;

            InitializeStyles();
            GUI.depth = 100;
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "", GUIStyle.none);
        }

        void InitializeStyles()
        {
            if (_stylesInitialized) return;

            Color voidBlack = new Color(0.02f, 0.02f, 0.025f, 0.985f);
            Color panel = new Color(0.055f, 0.055f, 0.058f, 1f);
            Color header = new Color(0.065f, 0.065f, 0.068f, 1f);
            Color well = new Color(0.048f, 0.048f, 0.052f, 1f);
            Color activeNav = new Color(0.1f, 0.1f, 0.105f, 1f);
            Color accent = new Color(0.93f, 0.93f, 0.96f, 0.88f);
            Color accentSoft = new Color(0.88f, 0.88f, 0.92f, 0.45f);
            Color text = new Color(0.98f, 0.98f, 0.99f, 1f);
            Color muted = new Color(0.48f, 0.48f, 0.52f, 1f);
            Color line = new Color(1f, 1f, 1f, 0.055f);
            Color border = new Color(1f, 1f, 1f, 0.07f);

            _windowTex = MakeTex(voidBlack);
            _panelTex = MakeTex(panel);
            _headerTex = MakeTex(header);
            _contentWellTex = MakeTex(well);
            _activeNavTex = MakeTex(activeNav);
            _accentTex = MakeTex(accent);
            _accentSoftTex = MakeTex(accentSoft);
            _hoverTex = MakeTex(new Color(1f, 1f, 1f, 0.04f));
            _lineTex = MakeTex(line);
            _borderTex = MakeTex(border);
            _scrollTrackTex = MakeTex(new Color(1f, 1f, 1f, 0.03f));
            _scrollThumbTex = MakeTex(new Color(0.42f, 0.42f, 0.46f, 0.95f));
            _scrollThumbHoverTex = MakeTex(new Color(0.58f, 0.58f, 0.62f, 1f));
            _sliderTrackTex = MakeTex(new Color(0.12f, 0.12f, 0.14f, 1f));
            _sliderThumbTex = MakeTex(new Color(0.5f, 0.5f, 0.54f, 1f));
            _sliderThumbHoverTex = MakeTex(new Color(0.68f, 0.68f, 0.72f, 1f));

            _titleStyle = new GUIStyle
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 2, 0),
                normal = { textColor = text }
            };
            _subtleStyle = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = muted }
            };
            _hintStyle = new GUIStyle(_subtleStyle)
            {
                fontSize = 14,
                wordWrap = true,
                padding = new RectOffset(0, 0, 4, 8)
            };
            _navStyle = new GUIStyle
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = muted },
                hover = { textColor = text, background = _hoverTex },
                padding = new RectOffset(18, 12, 8, 8),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _activeNavStyle = new GUIStyle(_navStyle)
            {
                normal = { textColor = text, background = _activeNavTex },
                hover = { textColor = text, background = _activeNavTex }
            };
            _rowStyle = new GUIStyle
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = text },
                padding = new RectOffset(2, 8, 10, 10)
            };
            _headerBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = text, background = _accentTex },
                hover = { textColor = text, background = _scrollThumbHoverTex },
                active = { textColor = text, background = _scrollThumbTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 6, 6)
            };
            _tinyBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = muted, background = _panelTex },
                hover = { textColor = text, background = _activeNavTex },
                active = { textColor = text, background = _scrollThumbTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(6, 6, 4, 4)
            };
            _segmentStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 36f,
                normal = { textColor = muted, background = _panelTex },
                onNormal = { textColor = text, background = _activeNavTex },
                hover = { textColor = text, background = _activeNavTex },
                active = { textColor = text, background = _accentTex },
                onActive = { textColor = text, background = _accentTex },
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(10, 10, 8, 8)
            };

            _scrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                fixedWidth = 8,
                margin = new RectOffset(8, 0, 0, 0),
                padding = new RectOffset(1, 1, 4, 4)
            };
            _scrollbarStyle.normal.background = _scrollTrackTex;
            _scrollbarStyle.hover.background = _scrollTrackTex;
            _scrollbarStyle.active.background = _scrollTrackTex;

            _scrollThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb) { fixedWidth = 6 };
            _scrollThumbStyle.normal.background = _scrollThumbTex;
            _scrollThumbStyle.hover.background = _scrollThumbHoverTex;
            _scrollThumbStyle.active.background = _scrollThumbHoverTex;

            _hSliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 8,
                margin = new RectOffset(0, 0, 10, 4)
            };
            _hSliderStyle.normal.background = _sliderTrackTex;

            _hThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = 16,
                fixedWidth = 12
            };
            _hThumbStyle.normal.background = _sliderThumbTex;
            _hThumbStyle.hover.background = _sliderThumbHoverTex;
            _hThumbStyle.active.background = _sliderThumbHoverTex;

            _stylesInitialized = true;
        }

        static Texture2D MakeTex(Color col)
        {
            var pix = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pix.wrapMode = TextureWrapMode.Clamp;
            pix.filterMode = FilterMode.Bilinear;
            pix.SetPixel(0, 0, col);
            pix.Apply();
            return pix;
        }

        void DrawBorderFrame(Rect outer, Texture2D edge)
        {
            float x = outer.x, y = outer.y, w = outer.width, h = outer.height;
            DrawRect(new Rect(x, y, w, 1f), edge);
            DrawRect(new Rect(x, y + h - 1f, w, 1f), edge);
            DrawRect(new Rect(x, y, 1f, h), edge);
            DrawRect(new Rect(x + w - 1f, y, 1f, h), edge);
        }

        void DrawWindow(int id)
        {
            if (_category > 2) _category = 0;
            if (_settingsSub < 0 || _settingsSub > 2) _settingsSub = 0;

            GUI.depth = 100;
            Rect win = new Rect(0f, 0f, _windowRect.width, _windowRect.height);

            DrawRect(win, _windowTex);
            DrawBorderFrame(win, _borderTex);

            DrawRect(new Rect(1f, 1f, win.width - 2f, HeaderH), _headerTex);
            DrawRect(new Rect(1f, HeaderH, win.width - 2f, 1f), _lineTex);
            DrawRect(new Rect(1f, 1f, 3f, win.height - 2f), _accentSoftTex);

            GUI.Label(new Rect(22f, 16f, win.width - 220f, 36f), PluginInfo.Name.ToUpperInvariant(), _titleStyle);
            GUI.Label(new Rect(23f, 52f, win.width - 220f, 26f), "F6 to toggle gui", _subtleStyle);

            if (GUI.Button(new Rect(win.width - 148f, 22f, 128f, 38f), "RESET ALL", _headerBtnStyle))
                P.ResetAllSettingsToDefault();

            DrawNavigation();
            DrawContent();

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, HeaderH));
        }

        void DrawNavigation()
        {
            float navTop = HeaderH + 18f;
            Rect navRect = new Rect(18f, navTop, NavW, _windowRect.height - navTop - 22f);
            DrawRect(navRect, _panelTex);
            DrawBorderFrame(navRect, _borderTex);
            DrawRect(new Rect(navRect.xMax, navRect.y + 6f, 1f, navRect.height - 12f), _lineTex);

            GUILayout.BeginArea(new Rect(navRect.x + 10f, navRect.y + 14f, navRect.width - 20f, navRect.height - 28f));
            DrawNavRow("Main", 0);
            GUILayout.Space(4f);
            DrawNavRow("Settings", 1);
            GUILayout.Space(4f);
            DrawNavRow("Colors", 2);
            GUILayout.EndArea();
        }

        void DrawNavRow(string label, int category)
        {
            bool isActive = _category == category;
            Rect row = GUILayoutUtility.GetRect(140f, 44f);
            if (GUI.Button(row, label, isActive ? _activeNavStyle : _navStyle))
            {
                _category = category;
                _scrollPos = Vector2.zero;
            }

            if (isActive)
                DrawRect(new Rect(row.x, row.y, 3f, row.height), _accentTex);
        }

        void DrawContent()
        {
            float navTop = HeaderH + 18f;
            float contentX = 18f + NavW + 18f;
            Rect contentRect = new Rect(contentX, navTop, _windowRect.width - contentX - 18f, _windowRect.height - navTop - 22f);
            DrawRect(contentRect, _panelTex);
            DrawBorderFrame(contentRect, _borderTex);

            Rect well = new Rect(contentRect.x + 10f, contentRect.y + 10f, contentRect.width - 20f, contentRect.height - 20f);
            DrawRect(well, _contentWellTex);
            DrawBorderFrame(well, _borderTex);

            string title = _category == 0 ? "Main" : _category == 1 ? "Settings" : "Colors";
            GUI.Label(new Rect(well.x + 16f, well.y + 14f, 280f, 26f), title, _titleStyle);
            DrawRect(new Rect(well.x + 16f, well.y + 44f, 48f, 2f), _accentTex);

            Rect body = new Rect(well.x + 14f, well.y + 56f, well.width - 28f, well.height - 72f);
            GUIStyle prevThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = _scrollThumbStyle;
            float innerH = _category == 0 ? 240f : _category == 1 ? 620f : 320f;
            _scrollPos = GUI.BeginScrollView(body, _scrollPos, new Rect(0f, 0f, body.width - 16f, innerH), false, true, GUIStyle.none, _scrollbarStyle);

            GUILayout.BeginVertical(GUILayout.Width(body.width - 20f));
            GUILayout.Space(4f);
            if (_category == 0) DrawMainTab();
            else if (_category == 1) DrawSettingsTab();
            else DrawColorsTab();
            GUILayout.EndVertical();

            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = prevThumb;
        }

        void DrawMainTab()
        {
            ToggleRow("Nametags enabled", P.NametagsEnabled);
            ToggleRow("FPS above heads", P.ShowFpsAboveHead);
            ToggleRow("Platform detection", P.ShowPlatformIcons);
        }

        void DrawSettingsTab()
        {
            GUILayout.Label("Settings", _subtleStyle);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = _settingsSub == 0
                ? new Color(0.2f, 0.2f, 0.22f)
                : new Color(0.08f, 0.08f, 0.09f);

            if (GUILayout.Button("Nametag", _segmentStyle, GUILayout.Height(42)))
                _settingsSub = 0;

            GUI.backgroundColor = _settingsSub == 1
                ? new Color(0.2f, 0.2f, 0.22f)
                : new Color(0.08f, 0.08f, 0.09f);

            if (GUILayout.Button("FPS", _segmentStyle, GUILayout.Height(42)))
                _settingsSub = 1;

            GUI.backgroundColor = _settingsSub == 2
                ? new Color(0.2f, 0.2f, 0.22f)
                : new Color(0.08f, 0.08f, 0.09f);

            if (GUILayout.Button("Platform", _segmentStyle, GUILayout.Height(42)))
                _settingsSub = 2;

            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();

            GUILayout.Space(16f);

            if (_settingsSub == 0)
            {
                GUILayout.Label("Nametag Settings", _rowStyle);
                GUILayout.Space(4f);

                SliderRow("Offset X", P.OffsetX, -0.5f, 0.5f);
                SliderRow("Offset Y", P.OffsetY, -0.2f, 1.2f);
                SliderRow("Offset Z", P.OffsetZ, -0.5f, 0.5f);

                GUILayout.Space(10f);

                SliderRow("Font Size", P.FontSize, 0.35f, 5f);
            }
            else if (_settingsSub == 1)
            {
                GUILayout.Label("FPS label Settings", _rowStyle);
                GUILayout.Space(4f);

                SliderRow("Offset X", P.FpsOffsetX, -0.5f, 0.5f);
                SliderRow("Offset Y", P.FpsOffsetY, -0.2f, 1.4f);
                SliderRow("Offset Z", P.FpsOffsetZ, -0.5f, 0.5f);

                GUILayout.Space(10f);

                SliderRow("Font Size", P.FpsFontSize, 0.15f, 3f);

                GUILayout.Space(10f);

                GUILayout.Label("FPS Color", _rowStyle);

                SliderRow("R", P.FpsColorR, 0f, 1f);
                SliderRow("G", P.FpsColorG, 0f, 1f);
                SliderRow("B", P.FpsColorB, 0f, 1f);
                SliderRow("A", P.FpsColorA, 0.2f, 1f);
            }
            else
            {
                DrawPlatformSettings();
            }
        }

        void DrawColorsTab()
        {
            ToggleRow("Custom player text color", P.CustomTextColor);
            GUILayout.Space(6f);
            SliderRow("Custom R", P.CustomColorR, 0f, 1f);
            SliderRow("Custom G", P.CustomColorG, 0f, 1f);
            SliderRow("Custom B", P.CustomColorB, 0f, 1f);
            SliderRow("Custom A", P.CustomColorA, 0.2f, 1f);
        }

        void DrawPlatformSettings()
        {
            ToggleRow("Platform detection", P.ShowPlatformIcons);
            GUILayout.Space(10f);

            GUILayout.Label("Icon Position", _rowStyle);
            SegmentedIntRow(P.PlatformIconLocation, new[] { "Top", "Left", "Right" });

            GUILayout.Space(14f);
            SliderRow("Icon Size", P.PlatformIconSize, 0.02f, 0.8f);
            SliderRow("Icon Spacing", P.PlatformIconSpacing, 0f, 1f);
        }

        void SegmentedIntRow(ConfigEntry<int> entry, string[] labels)
        {
            if (entry == null || labels == null || labels.Length == 0) return;

            GUILayout.BeginHorizontal();
            for (int i = 0; i < labels.Length; i++)
            {
                GUI.backgroundColor = entry.Value == i
                    ? new Color(0.2f, 0.2f, 0.22f)
                    : new Color(0.08f, 0.08f, 0.09f);

                if (GUILayout.Button(labels[i], _segmentStyle, GUILayout.Height(42)))
                    entry.Value = i;
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Reset", _tinyBtnStyle, GUILayout.Width(60f), GUILayout.Height(34f)))
                P.ResetEntry(entry);

            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        void ToggleRow(string label, ConfigEntry<bool> entry)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _rowStyle, GUILayout.ExpandWidth(true));
            bool v = GUILayout.Toggle(entry.Value, entry.Value ? "ON" : "OFF", GUILayout.Width(58f));
            if (v != entry.Value) entry.Value = v;
            if (GUILayout.Button("Reset", _tinyBtnStyle, GUILayout.Width(60f), GUILayout.Height(30f)))
                P.ResetEntry(entry);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        void SliderRow(string label, ConfigEntry<float> entry, float min, float max)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _subtleStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Reset", _tinyBtnStyle, GUILayout.Width(60f), GUILayout.Height(28f)))
                P.ResetEntry(entry);
            GUILayout.EndHorizontal();
            float v = GUILayout.HorizontalSlider(entry.Value, min, max, _hSliderStyle, _hThumbStyle);
            if (Mathf.Abs(v - entry.Value) > 1e-5f) entry.Value = v;
            GUILayout.Label($"{entry.Value:0.###}", _rowStyle);
            GUILayout.Space(10f);
        }

        void SliderRow(string label, ConfigEntry<int> entry, int min, int max)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _subtleStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Reset", _tinyBtnStyle, GUILayout.Width(60f), GUILayout.Height(28f)))
                P.ResetEntry(entry);
            GUILayout.EndHorizontal();
            float v = GUILayout.HorizontalSlider(entry.Value, min, max, _hSliderStyle, _hThumbStyle);
            int iv = Mathf.RoundToInt(v);
            if (iv != entry.Value) entry.Value = Mathf.Clamp(iv, min, max);
            GUILayout.Label($"{entry.Value}", _rowStyle);
            GUILayout.Space(10f);
        }

        static void DrawRect(Rect rect, Texture2D texture) => GUI.DrawTexture(rect, texture);
    }
}
