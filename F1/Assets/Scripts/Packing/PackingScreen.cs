using System.Collections;
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
        public Camera PackingCamera => packingCamera;
        [SerializeField] private Transform cargoSpawnPoint;
        [SerializeField] private WrapMaterialDef[] materials;

        // ── 아트 슬롯 (도현: 실제 아트는 나중에 Codex 로 제작) ─────────────────
        // 비워 두면(null) 지금의 코드 임시 모양을 쓴다. 스프라이트만 꽂으면 바뀐다.
        [Header("Art slots — null 이면 코드 임시 모양")]
        [Tooltip("완료 도장 테두리/바탕. null = 코드로 그린 빨간 이중 원. 꽂으면 색 틴트 없이(흰색) 그대로 그림 — 등급 글자는 위에 겹쳐 표시.")]
        [SerializeField] private Sprite stampSprite;
        [Tooltip("면별 보호 게이지 트랙(바탕). null = 단색 사각형. 9-slice 테두리가 있으면 Sliced 로 그림.")]
        [SerializeField] private Sprite gaugeTrackSprite;
        [Tooltip("면별 보호 게이지 채움. null = 단색 사각형(재료색). 길이는 anchorMax.x 로 조절하므로 Filled 타입 불필요.")]
        [SerializeField] private Sprite gaugeFillSprite;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        OrderJob currentJob;
        // 채널 8 정정: 한 런 = 여러 주문. currentJob은 primary(=orders[0]) — UI 호환.
        List<OrderJob> currentOrders = new List<OrderJob>();

        // B안 (2026-05-20) — 원물 모델 카탈로그. 미스/Box면 큐브+카드보드 폴백.
        CargoModelCatalog modelCatalog;
        CargoModelCatalog Catalog =>
            modelCatalog != null ? modelCatalog
            : (modelCatalog = Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog"));
        List<GameObject> spawnedItems = new();
        int activeItemIndex;

        // 미니뷰 폐기 (도현 요청) — 필드 제거

        // Input handler (Task 10)
        PackingScreenInput input;

        // UI refs (filled in BuildSidePanels)
        TMP_Text titleText, totalCostText;
        TMP_Text timeLeftText;   // 과제 1827: 포장 중에도 런 시계(주문판부터 흐름)가 보이게
        TMP_Text[] costPerMaterialText = new TMP_Text[3];
        TMP_Text[] cardCostText = new TMP_Text[3];   // 재료 카드의 ₩/m — 랩 폭 따라 갱신
        Image[] materialCardImages = new Image[3];
        Button goButton;
        TMP_Text widthLabel;
        WrapMaterialDef currentMaterial;
        float currentWidthUV = -1f;   // 휠로 고른 폭 — 물건을 바꿔도 유지
        float currentWidthT = 0.25f;  // 폭의 min~max 내 위치 (라벨 막대 수)

        // ── 보이는 보호 (설계 2026-10-22 §4 B2·B4) ─────────────────────
        // 면 = 화물 로컬 축. 배치 단계에 들어갈 때 회전이 identity 라 로컬 +Z = 트렁크 앞(제동 때 맞는 면).
        static readonly Vector3[] kSideDirs  = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        static readonly string[]  kSideNames = { "앞", "뒤", "좌", "우" };
        static readonly string[]  kSideHints = { "제동", "가속", "코너", "코너" };
        // 도장 등급 가중 (가설): 앞 40% · 뒤/좌/우 20%씩. A ≥ 60%, B ≥ 30% (흡수율 기준).
        static readonly float[]   kSideWeights = { 0.4f, 0.2f, 0.2f, 0.2f };
        const float kGradeA = 0.60f, kGradeB = 0.30f;
        // 면 하나가 '충분' — 흡수율 50%↑ (폼 0.55 / 하드 0.85 로 꽉 감으면 넘음, 버블만으론 안 넘음)
        const float kSideGoodAbsorption = 0.5f;
        const float kStampLeadSec = 0.35f;   // 도장 → 배치 화면 전환까지 (시계는 계속 흐름)

        TMP_Text[] sideValueText = new TMP_Text[4];
        RectTransform[] sideBarFill = new RectTransform[4];
        Image[] sideBarImage = new Image[4];
        TMP_Text gradePreviewText;
        // 물건별 면 흡수율 캐시 — 스트로크 끝날 때만 조회(비활성 물건은 raycast 불가라 캐시로 등급 계산)
        readonly Dictionary<GameObject, float[]> sideAbsByItem = new();
        readonly Dictionary<GameObject, int[]> sideChByItem = new();
        // 화물 '앞' 표식 — 화물 자식으로 두면 GetComponentInChildren<MeshRenderer> 가 표식을 잡을 수 있어 별도 오브젝트로 따라다니게 함
        readonly Dictionary<GameObject, TextMeshPro> frontMarkers = new();
        bool stamping;
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

            // 지난 런의 도장 코루틴 정리 (도장 중 Hide 됐을 수 있음)
            StopAllCoroutines();
            stamping = false;
            PackingSfx.Ensure(gameObject);

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
                titleText = totalCostText = widthLabel = null; timeLeftText = null;
                gradePreviewText = null;
                for (int i = 0; i < costPerMaterialText.Length; i++) costPerMaterialText[i] = null;
                for (int i = 0; i < cardCostText.Length; i++) cardCostText[i] = null;
                for (int i = 0; i < materialCardImages.Length; i++) materialCardImages[i] = null;
                for (int i = 0; i < 4; i++) { sideValueText[i] = null; sideBarFill[i] = null; sideBarImage[i] = null; }
                goButton = prevBtn = nextBtn = null;
                goButtonLabel = null;
                leftPanel = rightPanel = null;
            }
            draggedItem = null; // trunkRoot/trunkSim는 씬 객체 — 캐시 유지
            StopAllCoroutines(); // 도장 연출 — 패널이 사라졌으니 같이 정리
            stamping = false;
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
            timeLeftText = MakeLabel(panel.transform, "TimeLeftLabel", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -42), new Vector2(400, 30), 22, FontStyles.Bold,
                TextAlignmentOptions.Center);

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
                string matName   = i < mats.Length ? MatLabel(mats[i]) : kDefaultNames[i];
                Color  matColor  = i < mats.Length ? mats[i].displayColor : kDefaultColors[i];
                string keyBadge  = (i + 1).ToString();

                BuildMaterialCard(panel.transform, i, matName, matColor, keyBadge, y, mats);
            }

            // 랩 폭 레이블 — 휠로 바꿀 때마다 RefreshWidthUI 가 막대·cm 갱신
            widthLabel = MakeLabel(panel.transform, "WidthLabel", "랩 폭",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(220, 40), 20, FontStyles.Normal,
                TextAlignmentOptions.Center);
            widthLabel.enableAutoSizing = true; widthLabel.fontSizeMin = 12; widthLabel.fontSizeMax = 20;
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
                new Vector2(60, 20), new Vector2(-80, 36), 22, FontStyles.Bold,
                TextAlignmentOptions.Left);

            // 비용 레이블 — '다 감으면 약 ₩N / 보상의 N%' 두 줄(한 줄이면 키 배지를 덮음). 화물이 바뀌면 RefreshWidthUI 가 갱신.
            var costLbl = MakeLabel(t, "MatCost", idx < mats.Length ? FullWrapCostLabel(mats[idx]) : "",
                new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(60, -18), new Vector2(-80, 44), 15, FontStyles.Normal,
                TextAlignmentOptions.Left);
            if (UITheme.Active != null) costLbl.color = UITheme.Active.colorTextMuted;
            else costLbl.color = kMuted;
            if (idx >= 0 && idx < cardCostText.Length) cardCostText[idx] = costLbl;

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
            // 화면 우측 하단 — 위: 포장 비용, 아래: 면별 보호 게이지 (평균 커버 바 대체).
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
                new Vector2(0, -14), new Vector2(320, 40), 26, FontStyles.Bold,
                TextAlignmentOptions.Center);

            // 3 재료 비용 행 (y = -58, -98, -138)
            for (int i = 0; i < 3; i++)
            {
                float y = -(58f + i * 40f);
                string matName = i < mats.Length ? MatLabel(mats[i]) : kDefaultNames[i];

                MakeLabel(t, $"MatCostLabel_{i}", matName,
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(24, y), new Vector2(180, 40),
                    22, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

                costPerMaterialText[i] = MakeLabel(t, $"MatCostVal_{i}", "₩0",
                    new Vector2(1, 1f), new Vector2(1, 1f),
                    new Vector2(-24, y), new Vector2(180, 40),
                    24, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            }

            // 구분선
            MakePanel(t, "Divider",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -184), new Vector2(310, 2)).color = kMuted;

            // 합계
            MakeLabel(t, "TotalLabel", "합계",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(24, -192), new Vector2(150, 50), 26, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            totalCostText = MakeLabel(t, "TotalVal", "₩0",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-24, -192), new Vector2(200, 50), 32, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            if (UITheme.Active != null) totalCostText.color = UITheme.Active.colorAccent;
            else totalCostText.color = kAccent;

            MakePanel(t, "Divider2",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0, -250), new Vector2(310, 2)).color = kMuted;

            // ── 면별 보호 (흡수율) — 주행 중 벽 충돌 손상이 쓰는 값과 같은 함수(WrapState.QueryFaceAbsorption)
            MakeLabel(t, "ProtectHeader", "면별 보호",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(24, -258), new Vector2(160, 40), 22, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            gradePreviewText = MakeLabel(t, "GradePreview", "예상 C",
                new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-24, -258), new Vector2(150, 40), 22, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);

            var trackColor = new Color(0.18f, 0.18f, 0.20f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float y = -(304f + i * 52f);
                var nameLbl = MakeLabel(t, $"SideName_{i}",
                    $"{kSideNames[i]} <size=70%>{kSideHints[i]}</size>",
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(20, y), new Vector2(90, 44), 24, FontStyles.Bold,
                    TextAlignmentOptions.MidlineLeft);
                if (i == 0) nameLbl.color = kStampInk; // '앞' 은 화물 위 표식과 같은 빨강

                var track = MakePanel(t, $"SideTrack_{i}",
                    new Vector2(0, 1f), new Vector2(0, 1f),
                    new Vector2(0, 1f),
                    new Vector2(112, y - 12f), new Vector2(150, 20));
                track.color = trackColor;
                track.raycastTarget = false;
                ApplyArtSprite(track, gaugeTrackSprite);

                // 필 — Image.Filled 는 스프라이트가 없으면 fillAmount 를 무시하고 꽉 차게 그린다(0%인데 바 가득 버그).
                // 그래서 anchorMax.x 로 길이를 조절한다.
                var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(track.transform, false);
                var fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero;
                fillRT.anchorMax = new Vector2(0f, 1f);
                fillRT.offsetMin = Vector2.zero;
                fillRT.offsetMax = Vector2.zero;
                var fillImg = fillGO.GetComponent<Image>();
                fillImg.raycastTarget = false;
                fillImg.color = kDefaultColors[0];
                ApplyArtSprite(fillImg, gaugeFillSprite); // 재료색 틴트는 유지 (흰 스프라이트 권장)
                sideBarFill[i] = fillRT;
                sideBarImage[i] = fillImg;

                sideValueText[i] = MakeLabel(t, $"SideVal_{i}", "0%",
                    new Vector2(1, 1f), new Vector2(1, 1f),
                    new Vector2(-20, y), new Vector2(80, 44), 22, FontStyles.Bold,
                    TextAlignmentOptions.MidlineRight);
            }

            var foot = MakeLabel(t, "SideFoot", "앞 = 화물 위 빨간 '앞' 표식 쪽",
                new Vector2(0, 1f), new Vector2(0, 1f),
                new Vector2(20, -514), new Vector2(320, 30), 16, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            foot.color = UITheme.Active != null ? UITheme.Active.colorTextMuted : kMuted;
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
            // 진행 표시 — ●(현재) ○(미완) ✓(면별 보호 가중 점수가 B 이상)
            var sb = new System.Text.StringBuilder();
            sb.Append($"물건 {activeItemIndex + 1}/{total}   ");
            for (int i = 0; i < total && i < spawnedItems.Count; i++)
            {
                bool wrapped = spawnedItems[i] != null && WeightedScore(spawnedItems[i]) >= kGradeB;
                if (i == activeItemIndex) sb.Append("● ");
                else if (wrapped) sb.Append("<color=#88EE88>✓</color> ");
                else sb.Append("○ ");
            }
            titleText.text = sb.ToString();
        }

        void OnMaterialCardClick(int idx, WrapMaterialDef[] mats)
        {
            if (idx >= mats.Length) return;
            SelectMaterial(mats[idx]);
            AudioCueBus.Publish(AudioCueId.PackingMaterialSelect, mats[idx].channelIndex); // 키(1/2/3)와 같은 소리
        }

        // =========================================================
        //  Go button
        // =========================================================
        void OnGoClicked()
        {
            // 포장 단계 → 완료 도장(0.35s) → 배치 단계 진입. 도장 중 연타는 무시.
            if (stamping) return;
            if (phase == PackingPhase.Wrapping)
            {
                StartCoroutine(StampThenPlace());
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
            if (titleText != null) titleText.text = "트렁크에 배치 — 드래그로 옮기기";
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
            // '앞' 표식은 화물 자식이 아니라 따로 지운다 (출발 때 화물은 넘기고 표식만 정리).
            foreach (var kv in frontMarkers) if (kv.Value != null) Destroy(kv.Value.gameObject);
            frontMarkers.Clear();
            sideAbsByItem.Clear();
            sideChByItem.Clear();
        }

        /// <summary>포장비 상한 = 주문 baseReward × 이 값 (가설 0.4).</summary>
        const float PackCostCapOfReward = 0.4f;

        /// <summary>
        /// 겉면을 한 번 다 덮을 때 DrawStrip 이 세는 픽셀 면적. 리본 폭이 콜라이더 로컬 기준이라
        /// 덮는 끈 길이 = 월드 면적 ÷ (폭 × 스케일) → 픽셀 면적 = 로컬 메시 면적 × 스케일 × Size²/4 (리본 폭 = 폭UV × 4m). 폭은 약분된다.
        /// </summary>
        static float FullCoverPixelArea(GameObject go, int size)
        {
            Mesh mesh = null; Transform t = null;
            var mc = go.GetComponentInChildren<MeshCollider>();
            if (mc != null && mc.sharedMesh != null) { mesh = mc.sharedMesh; t = mc.transform; }
            else { var mf = go.GetComponentInChildren<MeshFilter>(); if (mf != null) { mesh = mf.sharedMesh; t = mf.transform; } }
            float area = 6f; // 1m 큐브
            if (mesh != null && mesh.isReadable)
            {
                var v = mesh.vertices; var tri = mesh.triangles; area = 0f;
                for (int i = 0; i < tri.Length; i += 3)
                    area += Vector3.Cross(v[tri[i + 1]] - v[tri[i]], v[tri[i + 2]] - v[tri[i]]).magnitude * 0.5f;
            }
            float s = t != null ? t.lossyScale.x : 1f;
            return Mathf.Max(1f, area * s * size * size / 4f);
        }

        public static readonly Quaternion FrontFacingRotation = Quaternion.Euler(0f, 180f, 0f);

        void SpawnItem(int idx)
        {
            var ctype = (currentOrders != null && idx < currentOrders.Count && currentOrders[idx] != null)
                ? currentOrders[idx].cargoType : CargoType.Box;

            GameObject go;
            if (Catalog != null && Catalog.TryGet(ctype, out var prefab, out var scale))
            {
                // 원물 모델 (B안) — 정규화 프리팹 인스턴스. 메시는 자식, 자체 머티리얼 보유.
                go = Object.Instantiate(prefab);
                go.transform.localScale = Vector3.one * scale;
            }
            else
            {
                // 폴백 — 회색 큐브 → 프로시저럴 카드보드 (Box / 카탈로그 미스).
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = CargoBoxAppearance.CardboardMaterial(ctype);
                // 기본 큐브는 6면이 같은 UV(0~1)를 공유 → 한 면만 감아도 6면이 다 보호로 잡힌다(설계 F2).
                // 칠하기·흡수 조회는 콜라이더 메시 UV 를 쓰므로, 콜라이더만 6면 아틀라스 UV 큐브로 바꾼다
                // (보이는 카드보드 메시·텍스처는 그대로). WirePainterFor 는 이 MeshCollider 를 그대로 재사용.
                var bc = go.GetComponent<BoxCollider>();
                if (bc != null) DestroyImmediate(bc);
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = AtlasCubeMesh();
            }

            go.name = $"PackingItem_{idx}";
            if (cargoSpawnPoint != null) go.transform.SetParent(cargoSpawnPoint, false);
            go.transform.localPosition = Vector3.zero;
            // 화물 앞 = +Z, 포장 카메라는 +Z 를 바라봄 → 180° 돌려 첫 화면에서 앞면이 카메라를 보게 한다.
            go.transform.localRotation = FrontFacingRotation;
            var wrapState = go.AddComponent<WrapState>();
            int reward = (currentOrders != null && idx < currentOrders.Count && currentOrders[idx] != null)
                ? currentOrders[idx].baseReward : 1000;
            wrapState.CostCap = reward * PackCostCapOfReward;
            // 실측 보정(PitCrew/Calibrate Wrap Cost)이 있으면 그것, 없으면 메시 겉면적 추정.
            var calib = WrapCostCalibration.Load();
            float fullPixels = calib != null && calib.TryGet(ctype, out var px) ? px : FullCoverPixelArea(go, wrapState.Size);
            wrapState.FullWrapPixels = fullPixels;
            wrapState.Reward = reward;
            wrapState.CostScale = reward / fullPixels;
            go.AddComponent<WrapPainter>();
            // painter.Init은 ActivateItem → WirePainterFor에서 처리 (3D 리본이 표면 위에 올라탐)

            go.SetActive(false);
            spawnedItems.Add(go);
            frontMarkers[go] = CreateFrontMarker(idx);
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
                // 랩 폭도 물건을 바꿔도 유지 (painter 마다 폭을 따로 가짐)
                var wp = item.GetComponent<WrapPainter>();
                if (wp != null)
                {
                    if (currentWidthUV > 0f) wp.StripWidthUV = currentWidthUV;
                    else
                    {
                        currentWidthUV = wp.StripWidthUV;
                        currentWidthT = Mathf.InverseLerp(input.MinWidthUV, input.MaxWidthUV, currentWidthUV);
                    }
                }
                RefreshWidthUI();
                UpdateSideGaugeUI(item);
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

            // 페인트 대상 = 실제 메시를 가진 GameObject (큐브=자기 자신, fbx 원물=자식).
            // WrapState.targetRenderer도 같은 GameObject(GetComponentInChildren<Renderer>)라
            // QueryAbsorption의 MeshCollider 조회와 일치한다.
            var rend = item.GetComponentInChildren<MeshRenderer>();
            var meshGO = rend != null ? rend.gameObject : item;

            var box = meshGO.GetComponent<BoxCollider>();
            if (box != null) DestroyImmediate(box);
            var col = meshGO.GetComponent<MeshCollider>();
            if (col == null) col = meshGO.AddComponent<MeshCollider>();

            painter.Init(state, packingCamera, col);
            painter.OnStrokeEnded = OnStrokeCompleted;
        }

        void OnStrokeCompleted(WrapState state)
        {
            if (state == null) return;
            RefreshSideGauges(state.gameObject);
            RefreshTitle();
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];

        // =========================================================
        //  LateUpdate — 실시간 비용 라벨 갱신
        // =========================================================
        // 2026-05-15: 키보드 좌/우 네비를 잠시 넣었으나 이 프로젝트는 Input System 패키지 모드 →
        // UnityEngine.Input API 호출이 InvalidOperationException. 일단 제거. ◀/▶ 버튼 명시화로 대체.
        // 추후 Input System actions 라우팅 필요 시 별도 작업.

        /// <summary>남은 시간 = RunController.TimeRemaining (프로토타입 모드면 주문판·포장·주행 공용 시계). 30초 아래면 빨강.</summary>
        void UpdateTimeLeft()
        {
            if (timeLeftText == null) return;
            var rc = Services.Run as RunController;
            if (rc == null || !rc.ClockRunning) { timeLeftText.text = ""; return; }
            float t = Mathf.Max(0f, rc.TimeRemaining);
            timeLeftText.text = $"남은 시간 {(int)t / 60}:{(int)t % 60:00}";
            timeLeftText.color = t < 30f ? new Color(1f, 0.42f, 0.36f) : (UITheme.Active != null ? UITheme.Active.colorText : kText);
        }

        void LateUpdate()
        {
            UpdateFrontMarkers();
            if (!IsVisible) return;
            UpdateTimeLeft();
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
        //  표시 정리 — 재료 이름·m당 비용·랩 폭 (설계 P7)
        // =========================================================
        // 도장·'앞' 표식 공통 인주 빨강 (D-121 종이 전표·도장 톤)
        static readonly Color kStampInk = new Color(0.80f, 0.09f, 0.10f, 0.92f);

        /// <summary>재료 표시 이름 — 에셋 displayName 이 영어(Bubble/Foam/Hardcase)면 채널별 한글로.</summary>
        static string MatLabel(WrapMaterialDef m)
        {
            if (m == null) return kDefaultNames[0];
            int ch = Mathf.Clamp(m.channelIndex, 0, kDefaultNames.Length - 1);
            string n = m.displayName;
            if (string.IsNullOrEmpty(n)) return kDefaultNames[ch];
            foreach (char c in n) if (c > 127) return n; // 이미 한글
            return kDefaultNames[ch];
        }

        float CurrentWidthUV => currentWidthUV > 0f ? currentWidthUV : 0.06f; // 0.06 = WrapPainter.stripWidthUV 기본값

        /// <summary>
        /// 재료 카드 비용 안내 — '다 감으면 약 ₩N (보상 N%)'. N = 보정된 다 감기 면적 × 단가(WrapState.FullWrapCost).
        /// m당 표시는 화물마다 6배씩 달라 버그처럼 읽혀서 바꿨다(QA 1536 #4).
        /// </summary>
        string FullWrapCostLabel(WrapMaterialDef m)
        {
            var ws = ActiveItem != null ? ActiveItem.GetComponent<WrapState>() : null;
            if (m == null || ws == null || ws.Reward <= 0) return "";
            float cost = ws.FullWrapCost(m);
            int won = Mathf.RoundToInt(cost / 10f) * 10;
            int pct = Mathf.RoundToInt(cost / ws.Reward * 100f);
            return $"다 감으면 약 ₩{won:N0}\n보상의 {pct}%";
        }

        /// <summary>PackingScreenInput 이 휠로 폭을 바꿀 때 호출. t = min~max 사이 위치(0~1).</summary>
        public void OnWrapWidthChanged(float widthUV, float t)
        {
            currentWidthUV = widthUV;
            currentWidthT = Mathf.Clamp01(t);
            RefreshWidthUI();
        }

        void RefreshWidthUI()
        {
            float w = CurrentWidthUV;
            if (widthLabel != null)
            {
                int bars = 1 + Mathf.RoundToInt(Mathf.Clamp01(currentWidthT) * 4f); // 1~5칸
                float cm = w * 4f * 100f; // 리본 폭 = stripWidthUV × 4 (m)
                // 기호(▬)는 D-121 정적 폰트 아틀라스에 없을 수 있어 숫자로만 표시
                widthLabel.text = $"랩 폭 {cm:0}cm <size=70%>(휠 {bars}/5)</size>";
            }
            var mats = GetMaterials();
            for (int i = 0; i < cardCostText.Length; i++)
            {
                if (cardCostText[i] == null || i >= mats.Length) continue;
                cardCostText[i].text = FullWrapCostLabel(mats[i]);
            }
        }

        // =========================================================
        //  면별 보호 게이지 (설계 M1 + P3)
        // =========================================================
        /// <summary>스트로크 끝 — 활성 물건의 4면 흡수율을 다시 조회해 캐시·게이지 갱신.</summary>
        void RefreshSideGauges(GameObject item)
        {
            if (item == null) return;
            var ws = item.GetComponent<WrapState>();
            if (ws == null) return;
            if (!sideAbsByItem.TryGetValue(item, out var abs)) { abs = new float[4]; sideAbsByItem[item] = abs; }
            if (!sideChByItem.TryGetValue(item, out var chs)) { chs = new[] { -1, -1, -1, -1 }; sideChByItem[item] = chs; }

            bool crossed = false;
            for (int i = 0; i < 4; i++)
            {
                float prev = abs[i];
                abs[i] = ws.QuerySideAbsorption(kSideDirs[i], out chs[i]);
                if (prev < kSideGoodAbsorption && abs[i] >= kSideGoodAbsorption) crossed = true;
            }
            if (crossed) PackingSfx.PlaySideOk();
            UpdateSideGaugeUI(item);
        }

        /// <summary>캐시 값으로 게이지만 다시 그림 (물건 전환 때).</summary>
        void UpdateSideGaugeUI(GameObject item)
        {
            float[] abs = null;
            int[] chs = null;
            if (item != null)
            {
                sideAbsByItem.TryGetValue(item, out abs);
                sideChByItem.TryGetValue(item, out chs);
            }
            var ws = item != null ? item.GetComponent<WrapState>() : null;
            float max = ws != null ? ws.MaxAbsorption : 0.85f;
            var mats = GetMaterials();
            var okColor = UITheme.Active != null ? UITheme.Active.colorOkText : new Color(0.55f, 0.95f, 0.60f, 1f);
            var txtColor = UITheme.Active != null ? UITheme.Active.colorText : kText;

            for (int i = 0; i < 4; i++)
            {
                float a = abs != null ? abs[i] : 0f;
                int ch = chs != null ? chs[i] : -1;
                if (sideBarFill[i] != null)
                    sideBarFill[i].anchorMax = new Vector2(Mathf.Clamp01(a / Mathf.Max(0.01f, max)), 1f);
                if (sideBarImage[i] != null)
                {
                    Color c = ch >= 0 ? ColorOfChannel(mats, ch) : kMuted;
                    c.a = 1f;
                    sideBarImage[i].color = c;
                }
                if (sideValueText[i] != null)
                {
                    int pct = Mathf.RoundToInt(a * 100f);
                    bool good = a >= kSideGoodAbsorption;
                    sideValueText[i].text = $"{pct}%"; // '충분'은 색으로 (✓ 기호는 폰트에 없을 수 있음)
                    sideValueText[i].color = good ? okColor : txtColor;
                }
            }

            if (gradePreviewText != null)
            {
                char g = ComputeGrade(out float score);
                gradePreviewText.text = $"예상 {g} <size=70%>({Mathf.RoundToInt(score * 100f)}%)</size>";
            }
        }

        static Color ColorOfChannel(WrapMaterialDef[] mats, int ch)
        {
            if (mats != null) foreach (var m in mats) if (m != null && m.channelIndex == ch) return m.displayColor;
            return kDefaultColors[Mathf.Clamp(ch, 0, kDefaultColors.Length - 1)];
        }

        /// <summary>물건 하나의 가중 보호 점수 (앞 40 · 뒤/좌/우 20, 흡수율 0~1).</summary>
        float WeightedScore(GameObject item)
        {
            if (item == null || !sideAbsByItem.TryGetValue(item, out var abs)) return 0f;
            float s = 0f;
            for (int i = 0; i < 4; i++) s += abs[i] * kSideWeights[i];
            return s;
        }

        /// <summary>도장 등급 — 모든 물건 가중 점수의 평균. A ≥ 60%, B ≥ 30%, 그 아래 C (가설).</summary>
        char ComputeGrade(out float score)
        {
            score = 0f;
            int n = 0;
            foreach (var go in spawnedItems) { if (go == null) continue; score += WeightedScore(go); n++; }
            score = n > 0 ? score / n : 0f;
            if (score >= kGradeA) return 'A';
            if (score >= kGradeB) return 'B';
            return 'C';
        }

        // =========================================================
        //  화물 '앞' 표식 — 제동 때 맞는 면
        // =========================================================
        TextMeshPro CreateFrontMarker(int idx)
        {
            var go = new GameObject($"PackingFrontMarker_{idx}", typeof(RectTransform));
            var tmp = go.AddComponent<TextMeshPro>();
            var theme = UITheme.Active;
            if (theme != null && theme.fontPrimary != null) tmp.font = theme.fontPrimary;
            tmp.text = "앞";
            tmp.fontSize = 2.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = kStampInk;
            tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.8f);
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            go.SetActive(false);
            return tmp;
        }

        /// <summary>
        /// 표식이 화물(로컬 +Z) 앞면 바깥·위쪽을 따라다님. 포장 = 카메라를 봄(빌보드),
        /// 배치 = 바닥에 누워 글자 위쪽이 화물 앞을 향함. 화살표 기호는 D-121 정적 폰트에 없을 수 있어 '앞' 글자만.
        /// </summary>
        void UpdateFrontMarkers()
        {
            if (frontMarkers.Count == 0) return;
            bool screenOn = IsVisible && packingCamera != null && packingCamera.gameObject.activeInHierarchy;
            foreach (var kv in frontMarkers)
            {
                var item = kv.Key;
                var mk = kv.Value;
                if (mk == null) continue;
                bool show = screenOn && item != null && item.activeInHierarchy;
                if (mk.gameObject.activeSelf != show) mk.gameObject.SetActive(show);
                if (!show) continue;

                var ws = item.GetComponent<WrapState>();
                Renderer rend = ws != null ? ws.TargetRenderer : null;
                Bounds b = rend != null ? rend.bounds : new Bounds(item.transform.position, Vector3.one * 0.5f);
                Vector3 e = b.extents;
                Vector3 f = item.transform.forward;
                float extF = Mathf.Abs(f.x) * e.x + Mathf.Abs(f.y) * e.y + Mathf.Abs(f.z) * e.z;
                var mt = mk.transform;

                if (phase == PackingPhase.Placement)
                {
                    Vector3 flat = Vector3.ProjectOnPlane(f, Vector3.up);
                    if (flat.sqrMagnitude > 1e-3f)
                    {
                        flat.Normalize();
                        mt.position = b.center + flat * (extF * 0.55f) + Vector3.up * (e.y + 0.05f);
                        mt.rotation = Quaternion.LookRotation(Vector3.down, flat); // 위에서 읽힘, 글자 위쪽 = 화물 앞
                    }
                    else
                    {
                        // 앞면이 위/아래를 봄 (W/S 로 세움) — 트렁크 벽에 안 닿는 면
                        mt.position = b.center + Vector3.up * (e.y + 0.05f);
                        mt.rotation = packingCamera.transform.rotation;
                    }
                    mt.localScale = Vector3.one * 1.1f;
                }
                else
                {
                    mt.position = b.center + f * (extF + 0.06f) + Vector3.up * (e.y * 0.6f + 0.08f);
                    mt.rotation = packingCamera.transform.rotation; // 빌보드
                    mt.localScale = Vector3.one;
                }
            }
        }

        // =========================================================
        //  완료 도장 (설계 P4) — 빨간 원형 인주, 1.4→1.0 오버슛, 시계는 계속 흐름
        // =========================================================
        IEnumerator StampThenPlace()
        {
            stamping = true;
            var item = ActiveItem;
            var painter = item != null ? item.GetComponent<WrapPainter>() : null;
            if (painter != null) painter.EndStroke(); // 감던 중이면 마무리 → 게이지·등급에 반영

            char grade = ComputeGrade(out float score);
            Debug.Log($"[PackingScreen] 포장 완료 도장 {grade} (가중 흡수 {score * 100f:F0}%)");
            var stamp = BuildStamp(grade, score);
            PackingSfx.PlayStamp();
            if (stamp != null) StartCoroutine(AnimateStamp(stamp));

            yield return new WaitForSecondsRealtime(kStampLeadSec);
            stamping = false;
            if (!IsVisible || phase != PackingPhase.Wrapping) yield break;
            EnterPlacement();
        }

        RectTransform BuildStamp(char grade, float score)
        {
            if (panelRoot == null) return null;
            var go = new GameObject("PackStamp", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(panelRoot.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.sizeDelta = new Vector2(300f, 300f);
            rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(-9f, -4f)); // 손으로 찍은 기울기
            rt.localScale = Vector3.one * 1.4f;
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false; // 배치 화면의 '출발' 버튼을 가리지 않게
            cg.interactable = false;

            var ringGO = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGO.transform.SetParent(rt, false);
            var ringRT = (RectTransform)ringGO.transform;
            ringRT.anchorMin = Vector2.zero; ringRT.anchorMax = Vector2.one;
            ringRT.offsetMin = Vector2.zero; ringRT.offsetMax = Vector2.zero;
            var ring = ringGO.GetComponent<Image>();
            // 아트 슬롯이 있으면 그 그림 그대로(흰색 = 틴트 없음), 없으면 코드 임시 원 + 인주색
            ring.sprite = stampSprite != null ? stampSprite : StampRingSprite();
            ring.color = stampSprite != null ? Color.white : kStampInk;
            ring.raycastTarget = false;
            ring.preserveAspect = true;

            var top = MakeLabel(rt, "StampTop", "포장 완료",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 72f), new Vector2(220f, 44f), 32, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var big = MakeLabel(rt, "StampGrade", grade.ToString(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f), new Vector2(200f, 140f), 130, FontStyles.Bold,
                TextAlignmentOptions.Center);
            var bottom = MakeLabel(rt, "StampScore", $"보호 {Mathf.RoundToInt(score * 100f)}%",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -88f), new Vector2(200f, 32f), 22, FontStyles.Bold,
                TextAlignmentOptions.Center);
            top.color = big.color = bottom.color = kStampInk;
            return rt;
        }

        IEnumerator AnimateStamp(RectTransform rt)
        {
            var cg = rt != null ? rt.GetComponent<CanvasGroup>() : null;
            const float slam = 0.18f, hold = 1.1f, fade = 0.4f;

            // 쾅 — 1.4 → 0.94 (가속) → 1.0 (되튐)
            float t = 0f;
            while (t < slam)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / slam);
                float sc = k < 0.7f
                    ? Mathf.Lerp(1.4f, 0.94f, (k / 0.7f) * (k / 0.7f))
                    : Mathf.Lerp(0.94f, 1f, (k - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * sc;
                if (cg != null) cg.alpha = Mathf.Clamp01(k * 3f);
                yield return null;
            }
            if (rt == null) yield break;
            rt.localScale = Vector3.one;
            if (cg != null) cg.alpha = 1f;

            t = 0f;
            while (t < hold) { if (rt == null) yield break; t += Time.unscaledDeltaTime; yield return null; }

            t = 0f;
            while (t < fade)
            {
                if (rt == null) yield break;
                t += Time.unscaledDeltaTime;
                if (cg != null) cg.alpha = 1f - Mathf.Clamp01(t / fade);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>아트 슬롯 스프라이트를 Image 에 꽂는다. null 이면 아무것도 안 함(임시 단색 유지).</summary>
        static void ApplyArtSprite(Image img, Sprite sp)
        {
            if (img == null || sp == null) return;
            img.sprite = sp;
            img.type = sp.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        }

        static Sprite s_stampRing;

        /// <summary>도장 테두리 — 코드로 만든 흰 이중 원(Image.color 로 인주색). 저주파 얼룩 + 점 빠짐으로 찍힌 느낌.</summary>
        static Sprite StampRingSprite()
        {
            if (s_stampRing != null) return s_stampRing;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false)
            {
                name = "PackStampRing",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[N * N];
            var rng = new System.Random(1021);
            float c = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / c; // 0(중심) ~ 1(가장자리)
                float a = Band(r, 0.85f, 0.97f) + Band(r, 0.75f, 0.785f);
                a *= Mathf.Lerp(0.6f, 1f, Mathf.PerlinNoise(x * 0.045f, y * 0.045f));
                if (rng.NextDouble() < 0.05) a *= 0.35f;
                px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s_stampRing = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return s_stampRing;
        }

        static float Band(float r, float inner, float outer)
        {
            const float aa = 0.012f;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner - aa, inner + aa, r));
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outer - aa, outer + aa, r));
            return rise * (1f - fall);
        }

        // =========================================================
        //  Box 폴백 — 6면 아틀라스 UV 큐브 (콜라이더 전용)
        // =========================================================
        static Mesh s_atlasCube;

        /// <summary>
        /// 단위 큐브(±0.5), 면마다 UV 3×2 칸 하나씩(칸 여백 0.02) — 면별 칠하기·흡수 조회가 서로 안 섞인다.
        /// 칸: +Z(앞) -Z(뒤) +X(우) / -X(좌) +Y(위) -Y(아래). 삼각형은 바깥에서 볼 때 시계방향(Unity 앞면).
        /// </summary>
        static Mesh AtlasCubeMesh()
        {
            if (s_atlasCube != null) return s_atlasCube;
            Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
            Vector3[] ups     = { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.forward, Vector3.back };
            const float pad = 0.02f;
            var v = new List<Vector3>(24);
            var n = new List<Vector3>(24);
            var uv = new List<Vector2>(24);
            var tris = new List<int>(36);
            for (int f = 0; f < 6; f++)
            {
                Vector3 nf = normals[f], b = ups[f];
                Vector3 a = Vector3.Cross(nf, b); // 바깥에서 본 오른쪽
                Vector3 c = nf * 0.5f;
                int col = f % 3, row = f / 3;
                float u0 = col / 3f + pad, u1 = (col + 1) / 3f - pad;
                float v0 = row / 2f + pad, v1 = (row + 1) / 2f - pad;
                int i0 = v.Count;
                v.Add(c - a * 0.5f - b * 0.5f); uv.Add(new Vector2(u0, v0)); // 00
                v.Add(c + a * 0.5f - b * 0.5f); uv.Add(new Vector2(u1, v0)); // 10
                v.Add(c + a * 0.5f + b * 0.5f); uv.Add(new Vector2(u1, v1)); // 11
                v.Add(c - a * 0.5f + b * 0.5f); uv.Add(new Vector2(u0, v1)); // 01
                for (int k = 0; k < 4; k++) n.Add(nf);
                tris.Add(i0); tris.Add(i0 + 3); tris.Add(i0 + 2);
                tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 1);
            }
            var mesh = new Mesh { name = "PackingAtlasCube" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            s_atlasCube = mesh;
            return s_atlasCube;
        }

        // =========================================================
        //  Accent color helper
        // =========================================================
        static Color Accent() =>
            UITheme.Active != null ? UITheme.Active.colorAccent : kAccent;
    }
}
