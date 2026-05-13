using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitCrew.Packing
{
    public sealed class PackingScreen : MonoBehaviour, IPackingScreen
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Camera packingCamera;
        [SerializeField] private Transform cargoSpawnPoint;
        [SerializeField] private WrapMaterialDef[] materials;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        OrderJob currentJob;
        List<GameObject> spawnedItems = new();
        int activeItemIndex;

        // Input handler (Task 10)
        PackingScreenInput input;

        // UI refs (filled in BuildSidePanels)
        TMP_Text titleText, totalCostText, coverageText;
        Image coverageBarFill;
        TMP_Text[] costPerMaterialText = new TMP_Text[3];
        Button goButton;
        TMP_Text widthLabel;
        WrapMaterialDef currentMaterial;

        // ── 테마 폴백 색상 ──────────────────────────────────────────────
        static readonly Color kBg     = new Color(0.08f, 0.08f, 0.10f, 0.95f);
        static readonly Color kText   = Color.white;
        static readonly Color kMuted  = new Color(0.72f, 0.74f, 0.78f, 1f);
        static readonly Color kAccent = new Color(0.88f, 0.04f, 0.00f, 1f); // F1 레드

        // ── 재료 이름 기본값 (WrapMaterialDef 없을 때) ──────────────────
        static readonly string[] kDefaultNames  = { "버블랩", "폼",     "하드케이스" };
        static readonly Color[]  kDefaultColors = {
            new Color(0.42f, 0.72f, 0.91f, 1f),
            new Color(0.31f, 0.85f, 0.45f, 1f),
            new Color(0.95f, 0.75f, 0.20f, 1f),
        };

        // =========================================================
        //  Public API
        // =========================================================
        public void Show(OrderJob job)
        {
            currentJob = job;
            BuildUIIfNeeded();
            ClearItems();
            int n = Mathf.Clamp(job.itemCount, 1, 3);
            for (int i = 0; i < n; i++) SpawnItem(i);
            ActivateItem(0);
            if (panelRoot != null) panelRoot.SetActive(true);
            if (packingCamera != null) packingCamera.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (packingCamera != null) packingCamera.gameObject.SetActive(false);
            ClearItems();
        }

        // =========================================================
        //  BuildUI
        // =========================================================
        void BuildUIIfNeeded()
        {
            if (panelRoot != null) return;

            var canvasGO = new GameObject("PackingCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            panelRoot = canvasGO;

            EnsureEventSystem();
            BuildSidePanels(panelRoot.transform);
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        void BuildSidePanels(Transform canvas)
        {
            BuildTopBar(canvas);
            BuildLeftPanel(canvas);
            BuildRightPanel(canvas);
        }

        // ── Top Bar ─────────────────────────────────────────────────────
        void BuildTopBar(Transform canvas)
        {
            // 패널: 화면 상단 가로 전체, 높이 80
            var panel = MakePanel(canvas, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1f),
                new Vector2(0, -0),
                new Vector2(0, 80));

            // ◀ 이전 버튼
            var prevBtn = MakeButton(panel.transform, "BtnPrev", "◀",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(60, 0), new Vector2(80, 60));
            prevBtn.onClick.AddListener(OnPrevItem);

            // 물건 N/M 타이틀
            titleText = MakeLabel(panel.transform, "TitleLabel", "물건 1/1",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 60), 28, FontStyles.Bold,
                TextAlignmentOptions.Center);

            // ▶ 다음 버튼
            var nextBtn = MakeButton(panel.transform, "BtnNext", "▶",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-60, 0), new Vector2(80, 60));
            nextBtn.onClick.AddListener(OnNextItem);

            // 출발하기 버튼 (우측)
            goButton = MakeButton(panel.transform, "BtnGo", "출발하기",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-160, 0), new Vector2(200, 56));
            goButton.onClick.AddListener(OnGoClicked);
            // 출발 버튼 강조색
            var goBg = goButton.GetComponent<Image>();
            if (goBg != null) goBg.color = Accent();
        }

        // ── Left Panel ───────────────────────────────────────────────────
        void BuildLeftPanel(Transform canvas)
        {
            // 패널: 화면 왼쪽 세로 중앙, 가로 240
            var panel = MakePanel(canvas, "LeftPanel",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0.5f),
                new Vector2(0, 0),
                new Vector2(240, 500));

            var mats = GetMaterials();

            // 재료 카드 3개 세로 스택
            float cardH = 120f;
            float startY = 220f;
            for (int i = 0; i < 3; i++)
            {
                float y = startY - i * (cardH + 8);
                string matName   = i < mats.Length ? mats[i].displayName : kDefaultNames[i];
                Color  matColor  = i < mats.Length ? mats[i].displayColor : kDefaultColors[i];
                string keyBadge  = (i + 1).ToString();

                BuildMaterialCard(panel.transform, i, matName, matColor, keyBadge, y, mats);
            }

            // 랩 폭 레이블
            widthLabel = MakeLabel(panel.transform, "WidthLabel", "랩 폭 ▬▬▬",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(200, 40), 20, FontStyles.Normal,
                TextAlignmentOptions.Center);
            if (UITheme.Active != null) widthLabel.color = UITheme.Active.colorTextMuted;
            else widthLabel.color = kMuted;
        }

        void BuildMaterialCard(Transform parent, int idx, string matName, Color swatchColor,
            string keyBadge, float anchoredY, WrapMaterialDef[] mats)
        {
            // 카드 배경
            var card = MakePanel(parent, $"MatCard_{idx}",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -anchoredY),
                new Vector2(220, 112));
            card.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            var t = card.transform;

            // 색상 스와치
            var swatch = MakePanel(t, "Swatch",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(24, 0),
                new Vector2(28, 60));
            swatch.color = swatchColor;

            // 이름 레이블
            MakeLabel(t, "MatName", matName,
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(60, 12), new Vector2(-80, 36), 22, FontStyles.Bold,
                TextAlignmentOptions.Left);

            // 비용 레이블 (costPerPixel × 1000 형식으로 표시)
            float costDisplay = idx < mats.Length ? mats[idx].costPerPixel * 1000f : (idx + 1) * 30f;
            var costLbl = MakeLabel(t, "MatCost", $"₩{costDisplay:0}/m",
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(60, -16), new Vector2(-80, 28), 16, FontStyles.Normal,
                TextAlignmentOptions.Left);
            if (UITheme.Active != null) costLbl.color = UITheme.Active.colorTextMuted;
            else costLbl.color = kMuted;

            // 키 배지 (1/2/3)
            var badge = MakeLabel(t, "KeyBadge", keyBadge,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-14, 0), new Vector2(40, 40), 20, FontStyles.Bold,
                TextAlignmentOptions.Center);
            badge.color = Accent();

            // 클릭 버튼 (카드 전체 영역)
            var btn = card.gameObject.AddComponent<Button>();
            int captured = idx;
            WrapMaterialDef[] capturedMats = mats;
            btn.onClick.AddListener(() => OnMaterialCardClick(captured, capturedMats));
        }

        // ── Right Panel ──────────────────────────────────────────────────
        void BuildRightPanel(Transform canvas)
        {
            var panel = MakePanel(canvas, "RightPanel",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f),
                new Vector2(0, 0),
                new Vector2(280, 500));

            var mats = GetMaterials();

            // 재료별 비용 행
            float rowY = 220f;
            float rowH = 40f;

            var header = MakeLabel(panel.transform, "CostHeader", "포장 비용",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -20), new Vector2(240, 40), 22, FontStyles.Bold,
                TextAlignmentOptions.Center);
            _ = header;

            for (int i = 0; i < 3; i++)
            {
                float y = rowY - i * (rowH + 4);
                string matName = i < mats.Length ? mats[i].displayName : kDefaultNames[i];

                MakeLabel(panel.transform, $"MatCostLabel_{i}", matName,
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(16, -(rowY - (rowH + 4) * i)), new Vector2(120, rowH),
                    18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

                costPerMaterialText[i] = MakeLabel(panel.transform, $"MatCostVal_{i}", "₩0",
                    new Vector2(1, 1f), new Vector2(1, 1f),
                    new Vector2(-16, -(rowY - (rowH + 4) * i)), new Vector2(120, rowH),
                    18, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            }

            // 구분선
            MakePanel(panel.transform, "Divider",
                new Vector2(0.1f, 1f), new Vector2(0.9f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -(rowY - 3 * (rowH + 4) + 8)),
                new Vector2(0, 2)).color = kMuted;

            // 합계
            float totalY = rowY - 3 * (rowH + 4) + 18;
            MakeLabel(panel.transform, "TotalLabel", "합계",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(16, -(totalY + 10)), new Vector2(100, 44), 20, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            totalCostText = MakeLabel(panel.transform, "TotalVal", "₩0",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-16, -(totalY + 10)), new Vector2(140, 44), 24, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            if (UITheme.Active != null) totalCostText.color = UITheme.Active.colorAccent;
            else totalCostText.color = kAccent;

            // 커버리지 레이블
            float covLabelY = totalY + 62;
            MakeLabel(panel.transform, "CovLabel", "평균 커버",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(16, -(covLabelY)), new Vector2(120, 36), 18, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            coverageText = MakeLabel(panel.transform, "CovPercent", "0%",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-16, -(covLabelY)), new Vector2(80, 36), 18, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);

            // 커버리지 바 트랙
            float barY = covLabelY + 42;
            var barTrack = MakePanel(panel.transform, "CovBarTrack",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -(barY)), new Vector2(240, 20));
            barTrack.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            // 커버리지 바 필 (Filled Image)
            var fillGO = new GameObject("CovBarFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barTrack.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            coverageBarFill = fillGO.GetComponent<Image>();
            coverageBarFill.color = new Color(0.3f, 0.9f, 0.5f, 1f);
            coverageBarFill.type = Image.Type.Filled;
            coverageBarFill.fillMethod = Image.FillMethod.Horizontal;
            coverageBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            coverageBarFill.fillAmount = 0f;
        }

        // =========================================================
        //  Helpers — UI 요소 생성
        // =========================================================

        /// <summary>RectTransform + Image 패널 생성.</summary>
        static Image MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin    = anchorMin;
            rt.anchorMax    = anchorMax;
            rt.pivot        = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta    = sizeDelta;
            var img = go.GetComponent<Image>();
            img.color = (UITheme.Active != null) ? UITheme.Active.colorPanel : kBg;
            return img;
        }

        /// <summary>TMP_Text 생성.</summary>
        static TMP_Text MakeLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta,
            float fontSize, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.raycastTarget = false;
            t.text      = text;
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var theme = UITheme.Active;
            if (theme != null)
            {
                if (theme.fontPrimary != null) t.font = theme.fontPrimary;
                t.color = theme.colorText;
            }
            else t.color = kText;
            return t;
        }

        /// <summary>Image + Button + 자식 Text 버튼 생성.</summary>
        static Button MakeButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            // 자식 텍스트
            var txtGO = new GameObject("Label", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<TextMeshProUGUI>();
            txt.raycastTarget = false;
            txt.text      = label;
            txt.fontSize  = 22;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            var theme = UITheme.Active;
            if (theme != null)
            {
                if (theme.fontPrimary != null) txt.font = theme.fontPrimary;
                txt.color = theme.colorText;
            }
            else txt.color = kText;

            return go.GetComponent<Button>();
        }

        // =========================================================
        //  Materials
        // =========================================================
        WrapMaterialDef[] GetMaterials()
        {
            if (materials == null || materials.Length == 0)
                materials = Resources.LoadAll<WrapMaterialDef>("WrapMaterials");
            return materials;
        }

        public void SelectMaterial(WrapMaterialDef m)
        {
            currentMaterial = m;
            var item = ActiveItem;
            if (item != null)
                foreach (var p in item.GetComponents<WrapPainter>())
                    p.SetMaterial(m);
        }

        // =========================================================
        //  Item navigation
        // =========================================================
        void OnPrevItem()
        {
            if (spawnedItems.Count == 0) return;
            int next = (activeItemIndex - 1 + spawnedItems.Count) % spawnedItems.Count;
            ActivateItem(next);
        }

        void OnNextItem()
        {
            if (spawnedItems.Count == 0) return;
            int next = (activeItemIndex + 1) % spawnedItems.Count;
            ActivateItem(next);
        }

        void RefreshTitle()
        {
            if (titleText == null || currentJob == null) return;
            int total = Mathf.Clamp(currentJob.itemCount, 1, 3);
            titleText.text = $"물건 {activeItemIndex + 1}/{total}";
        }

        void OnMaterialCardClick(int idx, WrapMaterialDef[] mats)
        {
            if (idx < mats.Length) SelectMaterial(mats[idx]);
        }

        // =========================================================
        //  Go button
        // =========================================================
        void OnGoClicked()
        {
            Hide();
            // Task 11: Services.Run.BeginRun(currentJob, spawnedItems)
        }

        // =========================================================
        //  Item spawning
        // =========================================================
        void ClearItems()
        {
            foreach (var go in spawnedItems) if (go) Destroy(go);
            spawnedItems.Clear();
        }

        void SpawnItem(int idx)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"PackingItem_{idx}";
            if (cargoSpawnPoint != null) go.transform.SetParent(cargoSpawnPoint, false);
            go.transform.localPosition = Vector3.zero;
            go.AddComponent<WrapState>();
            go.AddComponent<WrapPainter>();
            // painter.Init은 ActivateItem → WirePainterFor에서 처리
            go.SetActive(false);
            spawnedItems.Add(go);
        }

        void ActivateItem(int idx)
        {
            for (int i = 0; i < spawnedItems.Count; i++) spawnedItems[i].SetActive(i == idx);
            activeItemIndex = idx;
            RefreshTitle();

            EnsureInput();
            var item = ActiveItem;
            if (item != null)
            {
                input.RotateTarget = item.transform;
                input.Painter = item.GetComponent<WrapPainter>();
                WirePainterFor(item);
            }
        }

        void EnsureInput()
        {
            if (input == null) input = gameObject.AddComponent<PackingScreenInput>();
            input.Screen = this;
        }

        public void SelectMaterialByChannel(int ch)
        {
            var mats = GetMaterials();
            foreach (var m in mats) if (m.channelIndex == ch) { SelectMaterial(m); break; }
        }

        void WirePainterFor(GameObject item)
        {
            var state = item.GetComponent<WrapState>();
            var painter = item.GetComponent<WrapPainter>();
            if (state == null || painter == null) return;
            var box = item.GetComponent<BoxCollider>();
            if (box != null) DestroyImmediate(box);
            var col = item.GetComponent<MeshCollider>();
            if (col == null) col = item.AddComponent<MeshCollider>();
            painter.Init(state, packingCamera, col);
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];

        // =========================================================
        //  Accent color helper
        // =========================================================
        static Color Accent() =>
            UITheme.Active != null ? UITheme.Active.colorAccent : kAccent;
    }
}
