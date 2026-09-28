using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProductionSummary
{
    /// <summary>
    /// The "Production" tab on the station docking screen: a button next to Lobby / Trade /
    /// Hangar / Crafting, and a scrollable panel listing every producing base in the sector.
    /// </summary>
    internal class ProductionTab : MonoBehaviour
    {
        // DockingUI.OpenPanel uses codes 1-4 for the built-in tabs.
        public const int PanelCode = 5;

        private const string PanelName = "ProductionSummaryPanel";
        private const string ButtonName = "BtnProduction";

        // Lang.Get(0, 501) is the game's own translated "Production", used by the station module UI.
        private const int ProductionLabelSection = 0;
        private const int ProductionLabelCode = 501;

        public static ProductionTab Instance;

        private DockingUI dockingUI;
        private Transform btnPanel;
        private GameObject button;
        private Text buttonLabel;
        private bool buttonUppercase;
        private Image buttonBG;
        private float maxButtonWidth = 245f;

        private Text titleText;
        private RectTransform content;
        private ScrollRect scrollRect;
        private VerticalLayoutGroup contentLayout;
        private Text hideMiningLabel;
        private Text compactLabel;
        private Font font;
        private readonly List<Text> cards = new List<Text>();
        private readonly List<CompactRow> rows = new List<CompactRow>();
        private static Sprite statusDot;

        /// <summary>One line of the compact view: a status icon and the text.</summary>
        private class CompactRow
        {
            public GameObject Root;
            public Image Icon;
            public Text Text;
        }
        private float nextRefresh;

        public static void Create(DockingUI dockingUI)
        {
            if (dockingUI.transform.Find(PanelName) != null)
            {
                return;
            }
            Transform lobbyPanel = dockingUI.transform.Find("LobbyPanel");
            var go = new GameObject(PanelName, typeof(RectTransform));
            go.transform.SetParent(dockingUI.transform, false);
            go.transform.SetSiblingIndex(lobbyPanel.GetSiblingIndex() + 1);
            Instance = go.AddComponent<ProductionTab>();
            Instance.Build(dockingUI, lobbyPanel);
            go.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Build(DockingUI dockingUI, Transform lobbyPanel)
        {
            this.dockingUI = dockingUI;
            btnPanel = dockingUI.dockingPanel.transform.Find("BtnPanel");
            Transform crafting = btnPanel.Find("BtnCrafting");
            Text referenceText = crafting.GetComponentInChildren<Text>(true);
            font = referenceText.font;

            BuildButton(crafting, referenceText);
            BuildPanel(lobbyPanel);
        }

        private void BuildButton(Transform template, Text templateText)
        {
            button = Instantiate(template.gameObject, btnPanel, false);
            button.name = ButtonName;
            button.transform.SetAsLastSibling();
            maxButtonWidth = ((RectTransform)template).sizeDelta.x;

            // Point the cloned LangText at the game's own "Production" string instead of "Crafting",
            // so the label is translated and follows language changes like the built-in tabs.
            LangText[] langs = button.GetComponentsInChildren<LangText>(true);
            for (int i = 1; i < langs.Length; i++)
            {
                DestroyImmediate(langs[i]);
            }
            if (langs.Length > 0)
            {
                LangText lang = langs[0];
                buttonLabel = lang.GetComponent<Text>();
                lang.textSection = ProductionLabelSection;
                lang.textCode = ProductionLabelCode;
                lang.complement = "";
                // Awake() registers the clone for language refreshes; it may not have run yet.
                lang.Refresh();
            }
            else
            {
                buttonLabel = button.GetComponentInChildren<Text>(true);
                buttonUppercase = templateText.text == templateText.text.ToUpper();
                SetButtonLabel();
            }

            Transform icon = button.transform.Find("Image");
            Sprite sprite = ObjManager.GetSprite("Sprites/welder");
            if (icon != null && sprite != null)
            {
                icon.GetComponent<Image>().sprite = sprite;
            }

            buttonBG = button.GetComponent<Image>();
            // Replace the persistent OpenPanel(4) listener copied from the Crafting button.
            Button btn = button.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(() => dockingUI.OpenPanel(PanelCode));
        }

        private void BuildPanel(Transform lobbyPanel)
        {
            var rt = (RectTransform)transform;
            var lobbyRT = (RectTransform)lobbyPanel;
            rt.anchorMin = lobbyRT.anchorMin;
            rt.anchorMax = lobbyRT.anchorMax;
            rt.pivot = lobbyRT.pivot;
            rt.anchoredPosition = lobbyRT.anchoredPosition;
            rt.sizeDelta = lobbyRT.sizeDelta;

            // Reuse the station-management background so the panel matches the game's look.
            var bg = gameObject.AddComponent<Image>();
            Image managementBG = dockingUI.transform.Find("ManagementPanel/Modules")?.GetComponent<Image>();
            if (managementBG != null)
            {
                bg.sprite = managementBG.sprite;
                bg.type = managementBG.type;
                bg.color = managementBG.color;
            }
            else
            {
                bg.color = new Color(0f, 0f, 0f, 0.8f);
            }

            Text lobbyTitle = lobbyPanel.Find("Tittle")?.GetComponent<Text>();
            titleText = CreateText("Title", transform, lobbyTitle != null ? lobbyTitle.fontSize : 18);
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            if (lobbyTitle != null)
            {
                titleText.color = lobbyTitle.color;
            }
            titleText.gameObject.AddComponent<Shadow>();
            var titleRT = titleText.rectTransform;
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -6f);
            titleRT.sizeDelta = new Vector2(-20f, 30f);

            BuildToggles();
            BuildScrollView();
        }

        /// <summary>
        /// The two checkboxes in the header: Compact view at the top left, Hide mining &amp; refineries
        /// at the top right.
        /// </summary>
        private void BuildToggles()
        {
            compactLabel = BuildToggle("CompactViewToggle", rightSide: false, Plugin.CompactView.Value, OnCompactViewChanged);
            compactLabel.text = Loc.Get(Loc.CompactView);
            hideMiningLabel = BuildToggle("HideMiningToggle", rightSide: true, Plugin.HideMining.Value, OnHideMiningChanged);
            hideMiningLabel.text = Loc.Get(Loc.HideMining);
        }

        /// <summary>A checkbox in a header corner, with its label on the inner side. Returns the label.</summary>
        private Text BuildToggle(string name, bool rightSide, bool isOn, UnityEngine.Events.UnityAction<bool> onChanged)
        {
            float side = rightSide ? 1f : 0f;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(side, 1f);
            rt.anchorMax = new Vector2(side, 1f);
            rt.pivot = new Vector2(side, 1f);
            rt.anchoredPosition = new Vector2(rightSide ? -14f : 14f, -10f);
            rt.sizeDelta = new Vector2(240f, 22f);

            var boxGO = new GameObject("Box", typeof(RectTransform));
            boxGO.transform.SetParent(go.transform, false);
            var boxRT = (RectTransform)boxGO.transform;
            boxRT.anchorMin = new Vector2(side, 0.5f);
            boxRT.anchorMax = new Vector2(side, 0.5f);
            boxRT.pivot = new Vector2(side, 0.5f);
            boxRT.sizeDelta = new Vector2(18f, 18f);
            var box = boxGO.AddComponent<Image>();
            box.color = new Color(1f, 1f, 1f, 0.15f);

            var checkGO = new GameObject("Check", typeof(RectTransform));
            checkGO.transform.SetParent(boxGO.transform, false);
            var checkRT = (RectTransform)checkGO.transform;
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(4f, 4f);
            checkRT.offsetMax = new Vector2(-4f, -4f);
            var check = checkGO.AddComponent<Image>();
            check.color = new Color(0.55f, 0.85f, 1f);
            check.raycastTarget = false;

            Text label = CreateText("Label", go.transform, 13);
            label.alignment = rightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            // Clicks on the label reach the Toggle on the parent, so the whole row is clickable.
            label.raycastTarget = true;
            var labelRT = label.rectTransform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(rightSide ? 0f : 26f, 0f);
            labelRT.offsetMax = new Vector2(rightSide ? -26f : 0f, 0f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(onChanged);
            return label;
        }

        private void OnCompactViewChanged(bool compact)
        {
            Plugin.CompactView.Value = compact;
            Refresh();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnHideMiningChanged(bool hide)
        {
            // BepInEx saves the config file whenever a setting changes.
            Plugin.HideMining.Value = hide;
            Refresh();
        }

        private void BuildScrollView()
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform));
            scrollGO.transform.SetParent(transform, false);
            var scrollRT = (RectTransform)scrollGO.transform;
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(10f, 10f);
            scrollRT.offsetMax = new Vector2(-10f, -42f);
            // Transparent graphic so the mouse wheel scrolls anywhere over the list.
            scrollGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = (RectTransform)viewportGO.transform;
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.pivot = new Vector2(0f, 1f);
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = new Vector2(-16f, 0f);
            viewportGO.AddComponent<RectMask2D>();

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            content = (RectTransform)contentGO.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var layout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout = layout;
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewportRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.verticalScrollbar = BuildScrollbar(scrollGO.transform);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        private static Scrollbar BuildScrollbar(Transform parent)
        {
            var barGO = new GameObject("Scrollbar", typeof(RectTransform));
            barGO.transform.SetParent(parent, false);
            var barRT = (RectTransform)barGO.transform;
            barRT.anchorMin = new Vector2(1f, 0f);
            barRT.anchorMax = new Vector2(1f, 1f);
            barRT.pivot = new Vector2(1f, 0.5f);
            barRT.sizeDelta = new Vector2(10f, 0f);
            barGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var areaGO = new GameObject("SlidingArea", typeof(RectTransform));
            areaGO.transform.SetParent(barGO.transform, false);
            var areaRT = (RectTransform)areaGO.transform;
            areaRT.anchorMin = Vector2.zero;
            areaRT.anchorMax = Vector2.one;
            areaRT.offsetMin = Vector2.zero;
            areaRT.offsetMax = Vector2.zero;

            var handleGO = new GameObject("Handle", typeof(RectTransform));
            handleGO.transform.SetParent(areaGO.transform, false);
            var handleRT = (RectTransform)handleGO.transform;
            handleRT.offsetMin = Vector2.zero;
            handleRT.offsetMax = Vector2.zero;
            var handleImage = handleGO.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.35f);

            var bar = barGO.AddComponent<Scrollbar>();
            bar.handleRect = handleRT;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            return bar;
        }

        private Text CreateText(string name, Transform parent, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.supportRichText = true;
            text.color = new Color(0.85f, 0.85f, 0.85f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }

        private Text GetCard(int index)
        {
            while (cards.Count <= index)
            {
                var cardGO = new GameObject("Station", typeof(RectTransform));
                cardGO.transform.SetParent(content, false);
                cardGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
                var layout = cardGO.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 6, 8);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                cards.Add(CreateText("Text", cardGO.transform, 14));
            }
            return cards[index];
        }

        private CompactRow GetRow(int index)
        {
            while (rows.Count <= index)
            {
                var rowGO = new GameObject("StationLine", typeof(RectTransform));
                rowGO.transform.SetParent(content, false);
                rowGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
                var layout = rowGO.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(8, 10, 4, 4);
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                var iconGO = new GameObject("Status", typeof(RectTransform));
                iconGO.transform.SetParent(rowGO.transform, false);
                var icon = iconGO.AddComponent<Image>();
                icon.sprite = StatusDot();
                icon.raycastTarget = false;
                var iconLayout = iconGO.AddComponent<LayoutElement>();
                iconLayout.minWidth = iconLayout.preferredWidth = 12f;
                iconLayout.minHeight = iconLayout.preferredHeight = 12f;

                Text text = CreateText("Text", rowGO.transform, 14);
                text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                rows.Add(new CompactRow { Root = rowGO, Icon = icon, Text = text });
            }
            return rows[index];
        }

        /// <summary>A small anti-aliased white circle, tinted per status. Unity's built-in UI sprites aren't available at runtime.</summary>
        private static Sprite StatusDot()
        {
            if (statusDot != null)
            {
                return statusDot;
            }
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size / 2f - 1f;
            var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), center) + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            statusDot = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return statusDot;
        }

        private static Color StatusColor(ProductionReport.StationReport report)
        {
            if (report.Inactive)
            {
                return TagColor(ColorSys.UITer, Color.gray);
            }
            switch (report.Status)
            {
                case ModuleStatus.Producing:
                    return TagColor(ColorSys.infoPos, Color.green);
                case ModuleStatus.LimitReached:
                    return TagColor(ColorSys.infoNeg2, new Color(1f, 0.65f, 0f));
                case ModuleStatus.Stalled:
                case ModuleStatus.Unpowered:
                    return TagColor(ColorSys.infoNeg, Color.red);
                default:
                    return TagColor(ColorSys.UITer, Color.gray);
            }
        }

        /// <summary>The colour in one of the game's "&lt;color=#rrggbb&gt;" rich-text tags, so icons match the text.</summary>
        private static Color TagColor(string tag, Color fallback)
        {
            int start = tag != null ? tag.IndexOf('#') : -1;
            int end = start >= 0 ? tag.IndexOf('>', start) : -1;
            if (end > start && ColorUtility.TryParseHtmlString(tag.Substring(start, end - start), out Color color))
            {
                return color;
            }
            return fallback;
        }

        public void Open()
        {
            if (dockingUI.station == null)
            {
                return;
            }
            gameObject.SetActive(true);
            Refresh();
            scrollRect.verticalNormalizedPosition = 1f;
            UpdateButtonColor();
        }

        public void Close()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void UpdateButtonColor()
        {
            if (buttonBG != null)
            {
                buttonBG.color = gameObject.activeSelf ? dockingUI.buttonBGColorOff : dockingUI.buttonBGColorOn;
            }
        }

        /// <summary>Called after the game shows/hides its own tab buttons for the docked station.</summary>
        public void OnDockingButtonsChanged(bool servicesMode)
        {
            button.SetActive(servicesMode);
            if (!servicesMode)
            {
                Close();
            }
            FitButtons();
            UpdateButtonColor();
        }

        /// <summary>The tab bar was laid out for four buttons; shrink them so a fifth still fits.</summary>
        private void FitButtons()
        {
            var active = new List<RectTransform>();
            foreach (Transform child in btnPanel)
            {
                if (child.gameObject.activeSelf)
                {
                    active.Add((RectTransform)child);
                }
            }
            if (active.Count == 0)
            {
                return;
            }
            float spacing = 0f;
            float padding = 0f;
            var group = btnPanel.GetComponent<HorizontalLayoutGroup>();
            if (group != null)
            {
                spacing = group.spacing;
                padding = group.padding.left + group.padding.right;
            }
            float available = ((RectTransform)btnPanel).rect.width - padding - spacing * (active.Count - 1);
            float width = Mathf.Min(maxButtonWidth, available / active.Count);
            foreach (RectTransform rt in active)
            {
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
                // Not '??': Unity objects override == null, which the null-coalescing operator ignores.
                LayoutElement element = rt.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = rt.gameObject.AddComponent<LayoutElement>();
                }
                element.preferredWidth = width;
            }
        }

        /// <summary>Called after the player changes the game language.</summary>
        public void RefreshLabels()
        {
            SetButtonLabel();
            hideMiningLabel.text = Loc.Get(Loc.HideMining);
            compactLabel.text = Loc.Get(Loc.CompactView);
            if (gameObject.activeSelf)
            {
                Refresh();
            }
        }

        private void SetButtonLabel()
        {
            // A LangText on the label refreshes itself when the language changes.
            if (buttonLabel != null && buttonLabel.GetComponent<LangText>() == null)
            {
                string text = Lang.Get(ProductionLabelSection, ProductionLabelCode);
                buttonLabel.text = buttonUppercase ? text.ToUpper() : text;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefresh)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            nextRefresh = Time.unscaledTime + Mathf.Max(0.25f, Plugin.RefreshSeconds.Value);
            TSector sector = GameData.data?.GetCurrentSector();
            bool compact = Plugin.CompactView.Value;
            List<ProductionReport.StationReport> reports = ProductionReport.Build(dockingUI.station, compact);

            titleText.text = Loc.Get(Loc.Title).ToUpper() + (sector != null ? "  " + sector.coords : "");
            contentLayout.spacing = compact ? 2f : 6f;

            int usedCards = 0;
            int usedRows = 0;
            string empty = reports.Count == 0 ? ColorSys.UITer + Loc.Get(Loc.NoBases) + "</color>" : null;
            if (compact)
            {
                if (empty != null)
                {
                    CompactRow row = GetRow(usedRows++);
                    row.Icon.gameObject.SetActive(false);
                    row.Text.text = empty;
                }
                foreach (ProductionReport.StationReport report in reports)
                {
                    CompactRow row = GetRow(usedRows++);
                    row.Icon.gameObject.SetActive(true);
                    row.Icon.color = StatusColor(report);
                    row.Text.text = report.Text;
                }
            }
            else
            {
                if (empty != null)
                {
                    GetCard(usedCards++).text = empty;
                }
                foreach (ProductionReport.StationReport report in reports)
                {
                    GetCard(usedCards++).text = report.Text;
                }
            }
            for (int j = 0; j < cards.Count; j++)
            {
                cards[j].transform.parent.gameObject.SetActive(j < usedCards);
            }
            for (int j = 0; j < rows.Count; j++)
            {
                rows[j].Root.SetActive(j < usedRows);
            }
        }
    }
}
