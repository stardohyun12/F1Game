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
        Image[] materialCardImages = new Image[3];
        Button goButton;
        TMP_Text widthLabel;
        WrapMaterialDef currentMaterial;

        // Show/Hide 시 커서 상태 복원용
        CursorLockMode previousCursorLock;
        bool previousCursorVisible;

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
            EnsurePackingCamera();
            BuildUIIfNeeded();
            ClearItems();
            int n = Mathf.Clamp(job.itemCount, 1, 3);
            for (int i = 0; i < n; i++) SpawnItem(i);
            ActivateItem(0);

            // 첫 재료 자동 선택 (안 그러면 페인트 안 됨)
            var firstMat = GetMaterials();
            if (firstMat != null && firstMat.Length > 0) SelectMaterial(firstMat[0]);

            // 포장 카메라 켜기 (depth=5라 메인 카메라 위에 그려짐)
            if (packingCamera != null) packingCamera.gameObject.SetActive(true);

            // 커서 해제
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (panelRoot != null) panelRoot.SetActive(true);

            // 디버그 — 큐브가 카메라 view 안에 있는지 확인용
            var item = ActiveItem;
            if (item != null)
                Debug.Log($"[PackingScreen] Show: cube={item.transform.position} cam={packingCamera.transform.position} active={item.activeSelf}");
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (packingCamera != null) packingCamera.gameObject.SetActive(false);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            ClearItems();
        }

        void EnsurePackingCamera()
        {
            if (packingCamera != null) return;
            var camGO = new GameObject("PackingCamera", typeof(Camera));
            // 월드에서 멀리 떨어진 공간(Y=200)을 작업대로 사용 — 메인 씬과 분리
            camGO.transform.position = new Vector3(0, 200, -3);
            camGO.transform.LookAt(new Vector3(0, 200, 0));
            packingCamera = camGO.GetComponent<Camera>();
            packingCamera.clearFlags = CameraClearFlags.SolidColor;
            packingCamera.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            packingCamera.depth = 5;
            packingCamera.cullingMask = ~0;
            packingCamera.fieldOfView = 35f;
            packingCamera.nearClipPlane = 0.1f;
            packingCamera.farClipPlane = 50f;
            camGO.SetActive(false);

            // cargoSpawnPoint도 같이 만들어 두기 (Y=200 작업대 위치)
            if (cargoSpawnPoint == null)
            {
                var spawnGO = new GameObject("PackingCargoSpawn");
                spawnGO.transform.position = new Vector3(0, 200, 0);
                cargoSpawnPoint = spawnGO.transform;
            }

            // 작업대 조명 (Y=200 공간이 어둡지 않게)
            var lightGO = new GameObject("PackingKeyLight");
            lightGO.transform.position = new Vector3(2, 202, -2);
            lightGO.transform.rotation = Quaternion.Euler(45, -30, 0);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.88f);
            // 카메라의 자식으로 두면 camGO.SetActive(true) 시 같이 켜짐
            lightGO.transform.SetParent(camGO.transform, true);
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
            if (idx >= 0 && idx < materialCardImages.Length) materialCardImages[idx] = card;

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
            // 패널 크기 키움 (550) + 안쪽 패딩 충분히
            var panel = MakePanel(canvas, "RightPanel",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(1, 0.5f),
                new Vector2(0, 0),
                new Vector2(300, 560));

            var mats = GetMaterials();
            var t = panel.transform;

            // 상단부터 아래로 한 줄씩 — 간격 충분히
            // 헤더
            MakeLabel(t, "CostHeader", "포장 비용",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(260, 40), 22, FontStyles.Bold,
                TextAlignmentOptions.Center);

            // 3 재료 비용 행 (y = -80, -130, -180)
            for (int i = 0; i < 3; i++)
            {
                float y = -(80f + i * 50f);
                string matName = i < mats.Length ? mats[i].displayName : kDefaultNames[i];

                MakeLabel(t, $"MatCostLabel_{i}", matName,
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(20, y), new Vector2(130, 40),
                    18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

                costPerMaterialText[i] = MakeLabel(t, $"MatCostVal_{i}", "₩0",
                    new Vector2(1, 1f), new Vector2(1, 1f),
                    new Vector2(-20, y), new Vector2(130, 40),
                    18, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            }

            // 구분선 (y = -240)
            MakePanel(t, "Divider",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -240), new Vector2(240, 2)).color = kMuted;

            // 합계 (y = -290)
            MakeLabel(t, "TotalLabel", "합계",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(20, -290), new Vector2(100, 44), 22, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            totalCostText = MakeLabel(t, "TotalVal", "₩0",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-20, -290), new Vector2(160, 44), 24, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            if (UITheme.Active != null) totalCostText.color = UITheme.Active.colorAccent;
            else totalCostText.color = kAccent;

            // 커버리지 헤더 (y = -370)
            MakeLabel(t, "CovLabel", "평균 커버",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(20, -370), new Vector2(140, 36), 18, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            coverageText = MakeLabel(t, "CovPercent", "0%",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-20, -370), new Vector2(100, 36), 20, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);

            // 커버리지 바 (y = -420, 두께 24)
            var barTrack = MakePanel(t, "CovBarTrack",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -420), new Vector2(260, 24));
            barTrack.color = new Color(0.18f, 0.18f, 0.20f, 1f);

            // 커버리지 바 필
            var fillGO = new GameObject("CovBarFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(barTrack.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            coverageBarFill = fillGO.GetComponent<Image>();
            coverageBarFill.color = new Color(0.30f, 0.85f, 0.45f, 1f);
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
            HighlightActiveCard(m);
        }

        void HighlightActiveCard(WrapMaterialDef m)
        {
            if (m == null) return;
            var idle = new Color(0.14f, 0.14f, 0.18f, 0.95f);
            var active = new Color(0.30f, 0.30f, 0.36f, 1.0f);
            int activeIdx = m.channelIndex;
            for (int i = 0; i < materialCardImages.Length; i++)
            {
                if (materialCardImages[i] == null) continue;
                materialCardImages[i].color = (i == activeIdx) ? active : idle;
            }
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
            AudioCueBus.Publish(AudioCueId.PackingConfirm);
            Debug.Log($"[PackingScreen] OnGoClicked — job={currentJob?.displayName} items={spawnedItems.Count}");
            // 아이템을 PackingScreen 컨텍스트에서 분리 — RunController로 인계
            foreach (var go in spawnedItems) if (go != null) go.transform.SetParent(null, true);
            var itemsCopy = new System.Collections.Generic.List<GameObject>(spawnedItems);
            spawnedItems.Clear(); // ClearItems in Hide()가 Destroy 안 하도록 먼저 비움
            Hide();
            if (Services.Run != null) Services.Run.BeginRun(currentJob, itemsCopy);
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
            // 큐브는 기본 URP/Lit 머티리얼 유지 — 3D 리본이 표면 위에 올라탐

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
            foreach (var m in mats) if (m.channelIndex == ch) { SelectMaterial(m); AudioCueBus.Publish(AudioCueId.PackingMaterialSelect, ch); break; }
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
            painter.OnStrokeEnded = OnStrokeCompleted;
        }

        void OnStrokeCompleted(WrapState state)
        {
            if (state == null) return;
            float cov = state.ComputeCoverage();
            if (coverageText != null) coverageText.text = $"평균 커버  {Mathf.RoundToInt(cov * 100)}%";
            if (coverageBarFill != null) coverageBarFill.fillAmount = cov;
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];

        // =========================================================
        //  LateUpdate — 실시간 비용 라벨 갱신
        // =========================================================
        void LateUpdate()
        {
            if (!IsVisible) return;
            var w = ActiveItem != null ? ActiveItem.GetComponent<WrapState>() : null;
            if (w == null) return;
            var mats = GetMaterials();
            for (int i = 0; i < 3; i++)
            {
                if (costPerMaterialText[i] == null) continue;
                var matName = i < mats.Length ? mats[i].displayName : (i < kDefaultNames.Length ? kDefaultNames[i] : $"M{i}");
                costPerMaterialText[i].text = $"{matName}  ₩{Mathf.RoundToInt(w.CostByChannel[i])}";
            }
            if (totalCostText != null) totalCostText.text = $"합계  ₩{Mathf.RoundToInt(w.TotalCost)}";
        }

        // =========================================================
        //  Accent color helper
        // =========================================================
        static Color Accent() =>
            UITheme.Active != null ? UITheme.Active.colorAccent : kAccent;
    }
}
