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
        // 채널 8 정정: 한 런 = 여러 주문. currentJob은 primary(=orders[0]) — UI 호환.
        List<OrderJob> currentOrders = new List<OrderJob>();
        List<GameObject> spawnedItems = new();
        int activeItemIndex;

        // 미니뷰 폐기 (도현 요청) — 필드 제거

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
        // 다물건 nav — itemCount>1일 때만 표시 (현 데모는 다 1)
        Button prevBtn, nextBtn;

        // ── 채널 5 Plan 2 — 트렁크 배치 단계 ───────────────────────────
        // 흐름: 포장(Wrapping) → 배치(Placement) → 출발.
        enum PackingPhase { Wrapping, Placement }
        PackingPhase phase;
        GameObject leftPanel, rightPanel;   // 배치 단계에서 숨김
        TMP_Text goButtonLabel;             // "배치하기" ↔ "출발" 토글
        Transform trunkRoot;                // CargoSimRoot — 배치할 실제 트렁크
        CargoShakeSimulator trunkSim;
        float trunkFloorWorldY, trunkLim;   // 드래그 평면 / 클램프 한계
        GameObject draggedItem;
        Vector3 grabOffsetWorld;            // 잡은 지점 ↔ 큐브 중심 오프셋

        // Show/Hide 시 커서 상태 복원용
        CursorLockMode previousCursorLock;
        bool previousCursorVisible;

        // 포장 중 잠시 끄는 트렁크 시뮬 카메라
        GameObject trunkCamRig;
        bool trunkCamWasActive;
        // 트렁크 CCTV HUD (RawImage) — rig 꺼도 마지막 프레임이 남으므로 같이 숨김
        GameObject cargoViewHUD;
        bool cargoViewWasActive;
        // 운전용 배달 HUD — 포장/배치 중엔 무관 → 숨김 (배치 단계에 화살표가 비쳐 보임)
        GameObject deliveryHUD;
        bool deliveryHUDWasActive;

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
            Show(new List<OrderJob> { job });
        }

        /// <summary>채널 8 정정 — 여러 주문 동시 포장. 큐브 개수 = orders.Count.</summary>
        public void Show(List<OrderJob> orders)
        {
            if (orders == null || orders.Count == 0)
            {
                Debug.LogWarning("[PackingScreen] Show: orders 비어있음");
                return;
            }
            currentOrders = new List<OrderJob>(orders);
            currentJob = currentOrders[0]; // primary (UI 호환)

            EnsurePackingCamera();
            BuildUIIfNeeded();

            // 포장 단계로 리셋 — 카메라를 작업대로 복귀 (지난 런 배치 단계에서 트렁크로 옮겼을 수 있음)
            phase = PackingPhase.Wrapping;
            draggedItem = null;
            if (packingCamera != null)
            {
                packingCamera.transform.position = new Vector3(0, 200, -3);
                packingCamera.transform.LookAt(new Vector3(0, 200, 0));
            }

            ClearItems();
            int n = Mathf.Clamp(currentOrders.Count, 1, 3);
            for (int i = 0; i < n; i++) SpawnItem(i);
            ActivateItem(0);

            // 다물건 nav 토글 — n==1이면 ◀▶ + "1/1" 거짓말 제거
            bool multi = n > 1;
            if (prevBtn != null) prevBtn.gameObject.SetActive(multi);
            if (nextBtn != null) nextBtn.gameObject.SetActive(multi);
            // title은 ActivateItem→RefreshTitle이 진행표시 포함해 설정 — 여기서 덮어쓰지 않음.

            // 첫 재료 자동 선택 (안 그러면 페인트 안 됨)
            var firstMat = GetMaterials();
            if (firstMat != null && firstMat.Length > 0) SelectMaterial(firstMat[0]);

            // 포장 카메라 켜기 (depth=5라 메인 카메라 위에 그려짐)
            if (packingCamera != null) packingCamera.gameObject.SetActive(true);

            // 트렁크 시뮬 캠 잠시 끔 — 포장 중엔 차/박스 안 보임
            if (trunkCamRig == null) trunkCamRig = GameObject.Find("CargoSimCamRig");
            if (trunkCamRig != null)
            {
                trunkCamWasActive = trunkCamRig.activeSelf;
                trunkCamRig.SetActive(false);
            }
            // CCTV HUD RawImage도 같이 숨김 — 안 그러면 마지막 프레임이 남음
            if (cargoViewHUD == null)
            {
                var cv = Object.FindFirstObjectByType<CargoView>(FindObjectsInactive.Include);
                if (cv != null) cargoViewHUD = cv.gameObject;
            }
            if (cargoViewHUD != null)
            {
                cargoViewWasActive = cargoViewHUD.activeSelf;
                cargoViewHUD.SetActive(false);
            }
            // 운전용 배달 HUD 숨김 — 배치 단계에서 화살표가 트렁크 위로 비쳐 보임.
            if (deliveryHUD == null)
            {
                var dh = GameObject.Find("DeliveryHUD");
                if (dh != null) deliveryHUD = dh;
            }
            if (deliveryHUD != null)
            {
                deliveryHUDWasActive = deliveryHUD.activeSelf;
                deliveryHUD.SetActive(false);
            }

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
            // panelRoot를 Destroy해서 잔재 보장 차단 (도현 보고: 운전 중 포장 UI 잔재).
            // 다음 Show에서 BuildUIIfNeeded가 다시 만듦.
            if (panelRoot != null)
            {
                Destroy(panelRoot);
                panelRoot = null;
                // UI 캐시 참조 초기화 — 죽은 객체 참조 방지
                titleText = totalCostText = coverageText = widthLabel = null;
                coverageBarFill = null;
                for (int i = 0; i < costPerMaterialText.Length; i++) costPerMaterialText[i] = null;
                for (int i = 0; i < materialCardImages.Length; i++) materialCardImages[i] = null;
                goButton = prevBtn = nextBtn = null;
                goButtonLabel = null;
                leftPanel = rightPanel = null;
            }
            draggedItem = null; // trunkRoot/trunkSim는 씬 객체 — 캐시 유지
            if (packingCamera != null) packingCamera.gameObject.SetActive(false);
            if (trunkCamRig != null && trunkCamWasActive) trunkCamRig.SetActive(true);
            if (cargoViewHUD != null && cargoViewWasActive) cargoViewHUD.SetActive(true);
            if (deliveryHUD != null && deliveryHUDWasActive) deliveryHUD.SetActive(true);
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

            // EnsurePreviewCamera 폐기 — 다른 각도 미니뷰 제거 (도현 요청)

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

        // EnsurePreviewCamera/BuildPreviewPanel 폐기 (도현 요청 — 미니뷰 불필요)

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
            // 1280x720 = 1920x1080의 ≈1.5x 크기. matchHeight=1 (높이 우선) — 와이드 모니터에서도 일관.
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;
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
            // 미니뷰 폐기 (도현 요청, 다른 각도 필요 없음)
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

            // 레이아웃 (좌→우): ◀(좌) ··· 타이틀(중앙) ··· ▶(우측 안쪽) ··· 배치하기(우측 끝)
            // 폭/위치 충돌 회피 — 이전 배치는 ▶와 배치하기가 70px 겹쳤음.
            prevBtn = MakeButton(panel.transform, "BtnPrev", "◀ 이전 물건",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(110, 0), new Vector2(180, 60));
            prevBtn.onClick.AddListener(OnPrevItem);

            titleText = MakeLabel(panel.transform, "TitleLabel", "포장",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 60), 30, FontStyles.Bold,
                TextAlignmentOptions.Center);
            titleText.enableAutoSizing = true; titleText.fontSizeMin = 18; titleText.fontSizeMax = 32;

            // ▶ 다음 물건 — 배치하기 왼쪽으로 충분히 띄움 (-340 ± 90 → x: -430~-250)
            nextBtn = MakeButton(panel.transform, "BtnNext", "다음 물건 ▶",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-340, 0), new Vector2(180, 60));
            nextBtn.onClick.AddListener(OnNextItem);

            // 배치하기/출발 — 우측 끝 (-110 ± 100 → x: -210~-10)
            goButton = MakeButton(panel.transform, "BtnGo", "배치하기",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-110, 0), new Vector2(200, 60));
            goButton.onClick.AddListener(OnGoClicked);
            goButtonLabel = goButton.GetComponentInChildren<TMP_Text>();
            // 진행 버튼 강조색
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
            leftPanel = panel.gameObject;

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
            // 미니뷰가 우상단 (-100 ~ -420) 차지 → cost 패널은 화면 우측 하단 정렬.
            var panel = MakePanel(canvas, "RightPanel",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(1, 0),
                new Vector2(-20, 40),
                new Vector2(360, 580));
            rightPanel = panel.gameObject;

            var mats = GetMaterials();
            var t = panel.transform;

            // 헤더
            MakeLabel(t, "CostHeader", "포장 비용",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -40), new Vector2(320, 50), 30, FontStyles.Bold,
                TextAlignmentOptions.Center);

            // 3 재료 비용 행 (y = -110, -170, -230)
            for (int i = 0; i < 3; i++)
            {
                float y = -(110f + i * 60f);
                string matName = i < mats.Length ? mats[i].displayName : kDefaultNames[i];

                MakeLabel(t, $"MatCostLabel_{i}", matName,
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(24, y), new Vector2(180, 50),
                    24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

                costPerMaterialText[i] = MakeLabel(t, $"MatCostVal_{i}", "₩0",
                    new Vector2(1, 1f), new Vector2(1, 1f),
                    new Vector2(-24, y), new Vector2(180, 50),
                    26, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            }

            // 구분선
            MakePanel(t, "Divider",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -310), new Vector2(310, 2)).color = kMuted;

            // 합계
            MakeLabel(t, "TotalLabel", "합계",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(24, -370), new Vector2(150, 56), 30, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            totalCostText = MakeLabel(t, "TotalVal", "₩0",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-24, -370), new Vector2(200, 56), 38, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            if (UITheme.Active != null) totalCostText.color = UITheme.Active.colorAccent;
            else totalCostText.color = kAccent;

            // 커버리지 헤더
            MakeLabel(t, "CovLabel", "평균 커버",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(24, -450), new Vector2(180, 44), 24, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            coverageText = MakeLabel(t, "CovPercent", "0%",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-24, -450), new Vector2(140, 44), 28, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);

            // 커버리지 바
            var barTrack = MakePanel(t, "CovBarTrack",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -510), new Vector2(310, 32));
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
            // 라벨 자동 크기 + ellipsis — 버튼 폭에 맞춰 글자가 벗어나지 않게.
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 12;
            txt.fontSizeMax = 22;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Ellipsis;
            txt.margin = new Vector4(8, 4, 8, 4); // 좌우 패딩
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
            if (titleText == null) return;
            int total = Mathf.Clamp(currentOrders != null ? currentOrders.Count : 0, 1, 3);
            if (total <= 1) { titleText.text = "포장"; return; }
            // 진행 표시 — ●(현재) ○(미완) ✓(커버리지 50%+)
            var sb = new System.Text.StringBuilder();
            sb.Append($"물건 {activeItemIndex + 1}/{total}   ");
            for (int i = 0; i < total && i < spawnedItems.Count; i++)
            {
                var ws = spawnedItems[i] != null ? spawnedItems[i].GetComponent<WrapState>() : null;
                bool wrapped = ws != null && ws.ComputeCoverage() >= 0.5f;
                if (i == activeItemIndex) sb.Append("● ");
                else if (wrapped) sb.Append("<color=#88EE88>✓</color> ");
                else sb.Append("○ ");
            }
            titleText.text = sb.ToString();
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
            // 포장 단계 → 배치 단계 진입
            if (phase == PackingPhase.Wrapping)
            {
                EnterPlacement();
                return;
            }

            // 배치 단계 → 실제 출발. 큐브들은 이미 trunkRoot(CargoSimRoot) 자식 →
            // MountWrappedItem이 그 위치를 보존한다 (SetParent(null) 안 함).
            AudioCueBus.Publish(AudioCueId.PackingConfirm);
            Debug.Log($"[PackingScreen] OnGoClicked(출발) — orders={currentOrders.Count} items={spawnedItems.Count}");
            var itemsCopy = new System.Collections.Generic.List<GameObject>(spawnedItems);
            var ordersCopy = new System.Collections.Generic.List<OrderJob>(currentOrders);
            spawnedItems.Clear(); // ClearItems in Hide()가 Destroy 안 하도록 먼저 비움
            Hide();
            if (Services.Run != null) Services.Run.BeginRun(ordersCopy, itemsCopy);
        }

        // =========================================================
        //  채널 5 Plan 2 — 트렁크 배치 단계
        // =========================================================
        /// <summary>포장 끝 → 트렁크 위 탑다운 뷰로 전환, 큐브를 트렁크 자식으로 옮김.</summary>
        void EnterPlacement()
        {
            // 트렁크(CargoSimRoot) 찾기 — 1회 캐시
            if (trunkRoot == null)
            {
                var rootGO = GameObject.Find("CargoSimRoot");
                if (rootGO != null)
                {
                    trunkRoot = rootGO.transform;
                    trunkSim = rootGO.GetComponent<CargoShakeSimulator>();
                }
            }
            if (trunkRoot == null || trunkSim == null)
            {
                // 트렁크 없음(폴백 씬) — 배치 건너뛰고 바로 출발.
                Debug.LogWarning("[PackingScreen] CargoSimRoot 못 찾음 — 배치 단계 건너뜀, 바로 출발");
                phase = PackingPhase.Placement;
                OnGoClicked();
                return;
            }

            phase = PackingPhase.Placement;
            AudioCueBus.Publish(AudioCueId.PackingConfirm);

            trunkSim.HidePlaceholder();

            float scale = trunkSim.ItemScale;
            float floorY = trunkSim.FloorY;
            trunkLim = trunkSim.TrunkHalfExtent - scale * 0.5f;
            trunkFloorWorldY = trunkRoot.position.y + floorY; // CargoSimRoot scale=1 가정

            // 큐브들을 트렁크 자식으로 옮기고 기본 일렬 배치 (플레이어가 드래그로 조정).
            float[] slotX = { -0.5f, 0f, 0.5f };
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                var go = spawnedItems[i];
                if (go == null) continue;
                go.SetActive(true); // 포장 단계에서 비활성이던 아이템도 모두 표시
                go.transform.SetParent(trunkRoot, false);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * scale;
                float sx = i < slotX.Length ? slotX[i] : 0f;
                go.transform.localPosition = new Vector3(sx, floorY, 0f);
                var painter = go.GetComponent<WrapPainter>();
                if (painter != null) painter.enabled = false; // 배치 중엔 페인트 차단
            }

            // 카메라를 트렁크 위로 (위에서 내려다봄). 트렁크 yaw에 맞춰 축 정렬.
            float carYaw = trunkRoot.eulerAngles.y;
            packingCamera.transform.position = trunkRoot.position + new Vector3(0, 4.5f, 0);
            packingCamera.transform.rotation = Quaternion.Euler(90f, carYaw, 0f);

            // UI — 포장용 패널 숨기고 안내 갱신.
            if (leftPanel != null) leftPanel.SetActive(false);
            if (rightPanel != null) rightPanel.SetActive(false);
            if (prevBtn != null) prevBtn.gameObject.SetActive(false);
            if (nextBtn != null) nextBtn.gameObject.SetActive(false);
            if (titleText != null) titleText.text = "트렁크에 배치 — 드래그 · Q/E 회전";
            if (goButtonLabel != null) goButtonLabel.text = "출발";
        }

        /// <summary>배치 단계 여부 — PackingScreenInput이 입력 분기에 사용.</summary>
        public bool IsPlacing => phase == PackingPhase.Placement;

        /// <summary>마우스 ray가 큐브를 잡으면 드래그 시작.</summary>
        public void BeginTrunkDrag(Vector2 screenPos)
        {
            if (packingCamera == null) return;
            // EnterPlacement에서 큐브를 옮긴 직후 클릭하면 collider가 아직 stale —
            // raycast 전에 물리 표현을 transform과 동기화.
            Physics.SyncTransforms();
            var ray = packingCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 50f)) return;

            // hit이 어느 큐브인지 — 부모 체인 따라 확인 (리본 메시 자식 hit 대비).
            GameObject picked = null;
            for (var t = hit.collider.transform; t != null; t = t.parent)
                if (spawnedItems.Contains(t.gameObject)) { picked = t.gameObject; break; }
            if (picked == null) return;

            draggedItem = picked;
            // 잡은 지점 ↔ 큐브 중심 월드 오프셋 (잡은 느낌 유지).
            var plane = new Plane(Vector3.up, new Vector3(0, trunkFloorWorldY, 0));
            if (plane.Raycast(ray, out float enter))
            {
                grabOffsetWorld = draggedItem.transform.position - ray.GetPoint(enter);
                grabOffsetWorld.y = 0f;
            }
            else grabOffsetWorld = Vector3.zero;
        }

        /// <summary>드래그 중 — 마우스 ray를 트렁크 바닥 평면에 투영, 벽 안으로 클램프.</summary>
        public void UpdateTrunkDrag(Vector2 screenPos)
        {
            if (draggedItem == null || packingCamera == null || trunkRoot == null) return;
            var ray = packingCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0, trunkFloorWorldY, 0));
            if (!plane.Raycast(ray, out float enter)) return;

            Vector3 desiredWorld = ray.GetPoint(enter) + grabOffsetWorld;
            Vector3 local = trunkRoot.InverseTransformPoint(desiredWorld);
            local.x = Mathf.Clamp(local.x, -trunkLim, trunkLim);
            local.z = Mathf.Clamp(local.z, -trunkLim, trunkLim);
            local.y = trunkSim != null ? trunkSim.FloorY : local.y;
            draggedItem.transform.localPosition = local;
        }

        /// <summary>드래그 종료.</summary>
        public void EndTrunkDrag() => draggedItem = null;

        /// <summary>현재 드래그 중인 큐브 있는지 (Input이 회전 키 처리할 때 가드).</summary>
        public bool IsDraggingTrunkItem => draggedItem != null;

        /// <summary>채널 5 — 배치 단계 회전. Y축 90도 스냅이 적재함 정렬에 직관적.</summary>
        public void RotateDraggedItem(float deltaYDeg)
        {
            if (draggedItem == null) return;
            draggedItem.transform.Rotate(0f, deltaYDeg, 0f, Space.World);
        }

        /// <summary>배치 단계 — 임의 축 90도 스냅 회전 (W/S pitch용).</summary>
        public void RotateDraggedItemAxis(Vector3 axis, float deltaDeg)
        {
            if (draggedItem == null) return;
            draggedItem.transform.Rotate(axis, deltaDeg, Space.World);
        }

        /// <summary>배치 단계 — 마우스 우클릭 드래그 자유 회전 (3D 표준).</summary>
        public void RotateDraggedItemFree(float mouseDeltaX, float mouseDeltaY)
        {
            if (draggedItem == null) return;
            const float sensitivity = 0.5f; // deg per pixel delta
            draggedItem.transform.Rotate(Vector3.up, mouseDeltaX * sensitivity, Space.World);
            draggedItem.transform.Rotate(Vector3.right, -mouseDeltaY * sensitivity, Space.World);
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
                // 2026-05-15 도현 피드백 — ▶로 2번째 큐브 가면 포장지(material) 안 나옴.
                // SelectMaterial은 ActiveItem painter에만 박혀서, 새 active로 옮기면
                // 이 painter엔 material 없음. 현재 선택된 currentMaterial을 다시 적용.
                if (currentMaterial != null)
                {
                    var p = item.GetComponent<WrapPainter>();
                    if (p != null) p.SetMaterial(currentMaterial);
                }
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
            if (coverageText != null) coverageText.text = $"{Mathf.RoundToInt(cov * 100)}%";
            if (coverageBarFill != null) coverageBarFill.fillAmount = cov;
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];

        // =========================================================
        //  LateUpdate — 실시간 비용 라벨 갱신
        // =========================================================
        // 2026-05-15: 키보드 좌/우 네비를 잠시 넣었으나 이 프로젝트는 Input System 패키지 모드 →
        // UnityEngine.Input API 호출이 InvalidOperationException. 일단 제거. ◀/▶ 버튼 명시화로 대체.
        // 추후 Input System actions 라우팅 필요 시 별도 작업.

        void LateUpdate()
        {
            if (!IsVisible) return;
            var w = ActiveItem != null ? ActiveItem.GetComponent<WrapState>() : null;
            if (w == null) return;
            var mats = GetMaterials();
            for (int i = 0; i < 3; i++)
            {
                if (costPerMaterialText[i] == null) continue;
                costPerMaterialText[i].text = $"₩{Mathf.RoundToInt(w.CostByChannel[i])}";
            }
            if (totalCostText != null) totalCostText.text = $"₩{Mathf.RoundToInt(w.TotalCost)}";
        }

        // =========================================================
        //  Accent color helper
        // =========================================================
        static Color Accent() =>
            UITheme.Active != null ? UITheme.Active.colorAccent : kAccent;
    }
}
