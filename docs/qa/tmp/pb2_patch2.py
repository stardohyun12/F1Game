p='F1/Assets/Scripts/Packing/Bench/PackBench.cs'
s=open(p,encoding='utf-8').read()
def rep(a,b,cnt=1):
    global s
    assert s.count(a)==cnt, (s.count(a), a[:80])
    s=s.replace(a,b)

# 헤더 주석
rep("""    /// 조작: 시선 = 마우스(조준점 = 커서 하나) · 집기 = 왼쪽 클릭 유지, 놓기 = 떼기 · 돌리기 = 휠(15°)·R(옆으로 90°)""",
"""    /// 조작(2판, D-253): 시선 = 마우스(조준점 = 커서 하나) · 집기 = 왼쪽 클릭 유지, 놓기 = 떼기
    ///   · 휠 = 든 물건을 앞으로 멀리 보내기/당겨 오기(잡은 거리) · 돌리기 = 오른쪽 버튼을 누른 채 마우스(Shift = 15° 씩) · R = 옆으로 90°
    ///   · 든 물건은 물리로 끌려온다(속도 추종 + 힘 상한) — 벽·다른 물건에 막히면 멈추고, 너무 멀리 끌면 손에서 빠진다.
    ///   · 뽁뽁이 = 롤을 클릭한 채 끌면 끈 만큼 풀려 나오고 떼면 끊긴다 → 시트를 집어 깔기·덮기, 물건 위에 얹고 E = 감싸기.""")

# 손 값
rep("""        [SerializeField] float holdLift = 0.05f;
        [SerializeField] float holdMaxSpeed = 2.5f;""",
"""        [SerializeField] float holdLift = 0.01f;
        [SerializeField] float holdMaxSpeed = 2.5f;
        [Tooltip("목표점까지 거리 1 m 당 따라오는 속도(1/s).")]
        [SerializeField] float holdGain = 14f;
        [Tooltip("손 힘 상한(N) — 무거운 판일수록 느리게 가속된다. 달걀판 2 kg → 최대 20 m/s².")]
        [SerializeField] float holdMaxForce = 40f;
        [SerializeField] float holdMaxAccel = 40f;
        [Tooltip("목표점과 이만큼(m) 넘게 0.3초 벌어지면 손에서 빠진다(벽에 막혀 끌 때).")]
        [SerializeField] float holdBreakDist = 0.22f;
        [Tooltip("휠 한 칸에 잡은 거리 변화(m).")]
        [SerializeField] float wheelStep = 0.03f;
        [Tooltip("오른쪽 버튼 돌리기: 마우스 1 픽셀당 도.")]
        [SerializeField] float rotateDegPerPixel = 0.35f;""")

rep("""        public bool HandBusy => held != null || tool != Tool.None || lifted != null;""",
"""        public bool HandBusy => held != null || tool != Tool.None || lifted != null || heldSheet != null || pullSheet != null;
        /// <summary>QA: 1판처럼 든 물건을 키네마틱으로 바로 옮긴다(벽 통과 비교용).</summary>
        public static bool LegacyHold;
        /// <summary>QA: 손 목표점을 직접 준다(마우스 대신).</summary>
        public Vector3? SimTarget;
        public PackItem Held => held;
        public BubbleSheet HeldSheet => heldSheet;
        public float HoldDist => holdDist;""")

rep("""        PackItem held; Quaternion heldRot; float heldHalfH;""",
"""        PackItem held; Quaternion heldRot; float heldHalfH;
        float holdDist = 0.8f, breakT; Vector2 rotAcc;
        BubbleSheet heldSheet, pullSheet; Vector3 sheetCross = Vector3.right, rollExit, rollCross; float pullTick;
        bool rotating;""")

# 입력
rep("""            if (!SimActive && !ui && m != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var d = m.delta.ReadValue() * mouseSensitivity;
                    yaw += d.x; pitch -= d.y;
                }""",
"""            rotating = !SimActive && !ui && m != null && Cursor.lockState == CursorLockMode.Locked && m.rightButton.isPressed && (held != null || heldSheet != null);
            if (!SimActive && !ui && m != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var raw = m.delta.ReadValue();
                    // 오른쪽 버튼을 누르고 있는 동안은 시선 대신 든 물건을 돌린다
                    if (rotating) RotateHeld(raw * rotateDegPerPixel, k != null && k.shiftKey.isPressed);
                    else { var d = raw * mouseSensitivity; yaw += d.x; pitch -= d.y; }
                }""")

rep("""            if (held != null)
            {
                if (wheel > 0.01f) heldRot = Quaternion.AngleAxis(15f, Vector3.up) * heldRot;
                if (wheel < -0.01f) heldRot = Quaternion.AngleAxis(-15f, Vector3.up) * heldRot;
                if (rKey) RotateSide();
            }""",
"""            if (held != null || heldSheet != null || pullSheet != null)
            {
                // 휠 = 잡은 거리(앞으로 보내기 +, 당겨 오기 −). 노치가 ±120 으로 오는 장치도 있어 맞춘다.
                if (Mathf.Abs(wheel) > 0.01f) SimWheel(Mathf.Abs(wheel) > 20f ? wheel / 120f : wheel);
                if (rKey && held != null) RotateSide();
            }""")

rep("""        void RotateSide()
        {""",
"""        /// <summary>휠 notches 칸만큼 잡은 거리를 바꾼다(봇·QA 도 이걸 부른다).</summary>
        public void SimWheel(float notches) => holdDist = Mathf.Clamp(holdDist + notches * wheelStep, 0.25f, reach);

        /// <summary>오른쪽 버튼 + 마우스: 가로 = 세로축으로 돌리기, 세로 = 화면 가로축으로 굴리기. snap 이면 15° 씩 끊어서.</summary>
        void RotateHeld(Vector2 deg, bool snap)
        {
            Vector2 apply;
            if (snap)
            {
                rotAcc += deg;
                apply = new Vector2(Mathf.Round(rotAcc.x / 15f) * 15f, Mathf.Round(rotAcc.y / 15f) * 15f);
                rotAcc -= apply;
            }
            else { apply = deg; rotAcc = Vector2.zero; }
            if (apply == Vector2.zero) return;
            Vector3 r = cam.transform.right; r.y = 0f; r = r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            if (held != null) heldRot = Quaternion.AngleAxis(apply.x, Vector3.up) * Quaternion.AngleAxis(-apply.y, r) * heldRot;
            if (heldSheet != null) sheetCross = Quaternion.AngleAxis(apply.x, Vector3.up) * sheetCross;
        }

        void RotateSide()
        {""")

# E: 감싸기
rep("""        void PressE()
        {
            if (held != null || lifted != null || moving != null) return;""",
"""        void PressE()
        {
            if (TryWrapLook()) return;
            if (held != null || lifted != null || moving != null || heldSheet != null) return;""")

# 클릭: 시트·롤
rep("""            var item = LookAt<PackItem>();
            if (item != null) { Touch(); Grab(item); return; }""",
"""            var item = LookAt<PackItem>();
            if (item != null && item.sheet != null) { Touch(); GrabSheet(item.sheet, lookHit.point); return; }
            if (item != null) { Touch(); Grab(item); return; }""")
rep("""                    case PackSupplyKind.Bubble: Grab(SpawnBubble(lookHit.point + Vector3.up * 0.1f)); Sfx(sfxBubble, 0.5f, 1f); break;""",
"""                    case PackSupplyKind.Bubble: StartPull(sup); break;""")

rep("""        void HoldLeft()
        {""",
"""        void HoldLeft()
        {
            if (pullSheet != null)
            {
                Vector3 hand = HoldTargetFree();
                float before = pullSheet.Length;
                pullSheet.Pull(rollExit, hand, rollCross);
                // 풀릴 때 롤이 도는 소리(길이 4 cm 마다 작게)
                pullTick += Mathf.Max(0f, pullSheet.Length - before);
                if (pullTick > 0.04f) { pullTick = 0f; Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], 0.18f, Random.Range(0.7f, 0.85f)); }
            }""")

rep("""            if (crumpling) FinishCrumple();""",
"""            if (pullSheet != null) FinishPull();
            if (heldSheet != null) { heldSheet.Release(); heldSheet = null; }
            if (crumpling) FinishCrumple();""")

# 집기·놓기(물리)
rep("""            held = it;
            heldRot = it.Body.rotation;
            var b = Bounds(it);
            heldHalfH = b.extents.y;
            it.Body.isKinematic = true;
            SetLayer(it.gameObject, 2);""",
"""            held = it;
            heldRot = it.Body.rotation;
            var b = Bounds(it);
            heldHalfH = b.extents.y;
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, it.Body.position), 0.25f, reach);
            breakT = 0f;
            WakeSheetsNear(b);
            if (LegacyHold) it.Body.isKinematic = true;
            else
            {
                // 2판: 동적 그대로 — 손은 속도로 끌어당긴다(FixedUpdate). 들고 있는 동안만 중력 끔(손이 받쳐 줌)
                it.Body.isKinematic = false;
                it.Body.useGravity = false;
                it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                it.Body.maxAngularVelocity = 20f;
                it.Body.WakeUp();
            }
            SetLayer(it.gameObject, 2);""")

rep("""            var it = held; held = null; crumpling = false;
            it.Body.isKinematic = false;
            it.Body.linearVelocity = Vector3.zero; it.Body.angularVelocity = Vector3.zero;""",
"""            var it = held; held = null; crumpling = false;
            it.Body.isKinematic = false;
            it.Body.useGravity = true;
            it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // 손을 떼면 손 속도가 조금 남는다(던지기는 없음 — 최대 0.6 m/s)
            it.Body.linearVelocity = Vector3.ClampMagnitude(it.Body.linearVelocity, 0.6f);
            it.Body.angularVelocity = Vector3.ClampMagnitude(it.Body.angularVelocity, 3f);""")

rep("""        Vector3 HoldTarget()
        {
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            Vector3 p = hasHit ? lookHit.point + Vector3.up * (heldHalfH + holdLift) : o + f * 0.8f;
            Vector3 d = p - o;
            if (d.magnitude > reach) p = o + d.normalized * reach;
            return p;
        }""",
"""        /// <summary>손 목표점 — 시선 위 잡은 거리(휠). 그 앞에 면이 있으면 면 위에 얹는다(물건 두께만큼 띄움).</summary>
        Vector3 HoldTarget()
        {
            if (SimTarget.HasValue) return SimTarget.Value;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            Vector3 p = o + f * holdDist;
            if (hasHit && holdDist >= lookHit.distance - 0.01f)
            {
                Vector3 n = lookHit.normal;
                float ext = heldHalfH;
                if (held != null)
                {
                    var e = Bounds(held).extents;
                    ext = Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;
                }
                else if (heldSheet != null) ext = 0.02f;
                p = lookHit.point + n * (ext + holdLift);
            }
            return p;
        }

        /// <summary>면에 얹지 않은 목표점(뽁뽁이 풀기 — 끈 거리 그대로).</summary>
        Vector3 HoldTargetFree() => SimTarget ?? (hasHit && holdDist >= lookHit.distance ? lookHit.point + Vector3.up * 0.02f : cam.transform.position + cam.transform.forward * holdDist);

        void WakeSheetsNear(Bounds b)
        {
            b.Expand(0.04f);
            foreach (var sh in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (sh != heldSheet && sh.Settled && sh.WorldBounds.Intersects(b)) sh.Wake();
        }

        // ── 뽁뽁이(2판) ──
        void StartPull(PackSupply roll)
        {
            // 롤 = 눕힌 원기둥(축 = 롤의 up). 시트는 롤 윗면에서 손 쪽으로 풀려 나온다.
            var t = roll.transform;
            rollCross = t.up;
            float rad = t.lossyScale.x * 0.5f;
            Vector3 toCam = cam.transform.position - t.position; toCam = Vector3.ProjectOnPlane(toCam, rollCross).normalized;
            rollExit = t.position + Vector3.up * rad * 0.7f + toCam * rad * 0.7f;
            pullSheet = BubbleSheet.Create(bubbleMat);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, rollExit), 0.25f, reach);
            pullSheet.Pull(rollExit, rollExit + toCam * 0.02f, rollCross);
            pullTick = 0f;
            Sfx(sfxBubble, 0.45f, 0.9f);
        }

        void FinishPull()
        {
            var sh = pullSheet; pullSheet = null;
            if (sh.Length < BubbleSheet.MinLength) { Destroy(sh.gameObject); return; } // 거의 안 끌었으면 롤에 도로 감김
            sh.Tear();
            // 찢는 소리 — 짧은 긁힘 셋을 높게 겹친다
            Sfx(sfxScratch[0], 0.55f, 1.55f); Sfx(sfxScratch[1], 0.45f, 1.8f); Sfx(sfxScratch[2], 0.35f, 2.1f);
            Debug.Log($"[PackBench] 뽁뽁이 {sh.Length:F2} m 뜯음");
        }

        void GrabSheet(BubbleSheet sh, Vector3 at)
        {
            heldSheet = sh;
            sh.Grab(at);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, at), 0.25f, reach);
            Vector3 r = cam.transform.right; r.y = 0f;
            sheetCross = r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            Sfx(sfxBubble, 0.35f, 1.1f);
        }

        /// <summary>E = 감싸기: 시트를 든 채 물건을 보거나, 물건 위에 얹힌 시트(또는 시트가 얹힌 물건)를 보고.</summary>
        bool TryWrapLook()
        {
            var look = LookAt<PackItem>();
            BubbleSheet sh = null; PackItem target = null;
            if (heldSheet != null && look != null && look.sheet == null) { sh = heldSheet; target = look; }
            else if (held == null && heldSheet == null && look != null) FindWrapPair(look, out sh, out target);
            if (sh == null || target == null) return false;
            return Wrap(sh, target);
        }

        /// <summary>물건 + 그 위에 얹힌 시트 짝 찾기(시트를 보든 물건을 보든).</summary>
        public static void FindWrapPair(PackItem look, out BubbleSheet sh, out PackItem target)
        {
            sh = null; target = null;
            if (look.sheet != null)
            {
                var sb = look.sheet.WorldBounds; sb.Expand(0.03f);
                foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None))
                    if (it.sheet == null && !it.Wrapped && it.GetComponent<BoxCollider>() != null && sb.Intersects(Bounds(it))) { sh = look.sheet; target = it; return; }
                return;
            }
            if (look.Wrapped || look.GetComponent<BoxCollider>() == null) return;
            var b = Bounds(look); b.Expand(0.03f);
            foreach (var s in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (s.Settled && s.WorldBounds.Intersects(b) && s.WorldBounds.max.y > b.center.y) { sh = s; target = look; return; }
        }

        public bool Wrap(BubbleSheet sh, PackItem target)
        {
            float need = BubbleSheet.WrapNeed(target);
            if (sh.Length < need * 0.9f) { Sfx(sfxDeny, 0.6f, 0.7f); Debug.Log($"[PackBench] 감싸기 실패 — 뽁뽁이 {sh.Length:F2} m < 한 바퀴 {need:F2} m"); return true; }
            if (heldSheet == sh) heldSheet = null;
            target.Wrap(bubbleMat, 0.005f);
            Destroy(sh.gameObject);
            Sfx(sfxBubble, 0.6f, 0.8f); Sfx(sfxScratch[1], 0.4f, 1.3f);
            Debug.Log($"[PackBench] 감쌈 {target.name} (뽁뽁이 {sh.Length:F2} m / 한 바퀴 {need:F2} m)");
            return true;
        }

        // ── 놓일 자리 그림자(점선 윤곽) ──
        Transform[] dots; Material dotMat;
        const int DotCount = 36;

        void LateUpdate()
        {
            bool show = (held != null || heldSheet != null) && !SimHideDots;
            if (dots == null) { if (!show) return; MakeDots(); }
            if (!show) { foreach (var d in dots) d.gameObject.SetActive(false); return; }
            // 윤곽(바닥에 비친 네 귀퉁이) — 상자형은 자기 축, 시트는 경계 상자
            Vector3 c0, ax, az;
            if (held != null)
            {
                var bc = held.GetComponent<BoxCollider>();
                var t = held.transform;
                if (bc != null)
                {
                    Vector3 hx = t.TransformVector(new Vector3(bc.size.x * 0.5f, 0f, 0f)), hy = t.TransformVector(new Vector3(0f, bc.size.y * 0.5f, 0f)), hz = t.TransformVector(new Vector3(0f, 0f, bc.size.z * 0.5f));
                    // 바닥에 비친 넓은 두 축(제일 위를 향한 축을 뺀다)
                    float ux = Mathf.Abs(hx.normalized.y), uy = Mathf.Abs(hy.normalized.y), uz = Mathf.Abs(hz.normalized.y);
                    if (ux >= uy && ux >= uz) { ax = hy; az = hz; } else if (uy >= uz) { ax = hx; az = hz; } else { ax = hx; az = hy; }
                    c0 = t.TransformPoint(bc.center);
                }
                else { var b = Bounds(held); c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            }
            else { var b = heldSheet.WorldBounds; c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            ax.y = 0f; az.y = 0f;
            for (int i = 0; i < DotCount; i++)
            {
                float u = i / (float)DotCount * 4f; int side = (int)u; float f = u - side;
                Vector3 off = side == 0 ? Vector3.Lerp(-ax - az, ax - az, f) : side == 1 ? Vector3.Lerp(ax - az, ax + az, f) : side == 2 ? Vector3.Lerp(ax + az, -ax + az, f) : Vector3.Lerp(-ax + az, -ax - az, f);
                Vector3 o = c0 + off;
                bool hit = Physics.Raycast(o, Vector3.down, out var h, 1.2f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                var d = dots[i];
                d.gameObject.SetActive(hit);
                if (hit) d.SetPositionAndRotation(h.point + h.normal * 0.0015f, Quaternion.LookRotation(-h.normal));
            }
        }

        public static bool SimHideDots;

        void MakeDots()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            dotMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "PB_Dot" };
            var col = new Color(0.08f, 0.08f, 0.09f);
            dotMat.SetColor("_BaseColor", col); dotMat.color = col;
            var root = new GameObject("PlaceDots").transform;
            dots = new Transform[DotCount];
            for (int i = 0; i < DotCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root, false);
                q.transform.localScale = Vector3.one * 0.009f;
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = dotMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dots[i] = q.transform;
                q.SetActive(false);
            }
        }""")

rep("""        public static void Setup(Rigidbody rb)""",
"""        public static PhysicsMaterial SoftMaterial => SoftMat;

        public static void Setup(Rigidbody rb)""")

# FixedUpdate 손
rep("""            if (held != null)
            {
                var rb = held.Body;
                rb.MovePosition(Vector3.MoveTowards(rb.position, HoldTarget(), holdMaxSpeed * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, heldRot, 720f * dt));
            }""",
"""            if (held != null && LegacyHold)
            {
                var rb = held.Body;
                rb.MovePosition(Vector3.MoveTowards(rb.position, HoldTarget(), holdMaxSpeed * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, heldRot, 720f * dt));
            }
            else if (held != null) DriveHeld(dt);
            if (heldSheet != null) heldSheet.Hold(HoldTarget(), sheetCross, holdMaxSpeed, dt);""")

rep("""        // ── [E] 칩 ──""",
"""        /// <summary>든 물건을 목표점까지 속도로 끌어당긴다 — 힘 상한이 있어서 벽·물건에 막히면 멈춘다(통과 없음).</summary>
        void DriveHeld(float dt)
        {
            var rb = held.Body;
            Vector3 err = HoldTarget() - rb.position;
            Vector3 vDes = Vector3.ClampMagnitude(err * holdGain, holdMaxSpeed);
            Vector3 dv = vDes - rb.linearVelocity;
            float maxDv = Mathf.Min(holdMaxForce / Mathf.Max(0.05f, rb.mass), holdMaxAccel) * dt;
            rb.AddForce(Vector3.ClampMagnitude(dv, maxDv), ForceMode.VelocityChange);
            // 돌리기도 같은 식(각속도 추종, 상한)
            Quaternion dq = heldRot * Quaternion.Inverse(rb.rotation);
            dq.ToAngleAxis(out float ang, out Vector3 axis);
            if (ang > 180f) ang -= 360f;
            Vector3 wDes = Mathf.Abs(ang) < 0.01f || float.IsNaN(axis.x) ? Vector3.zero : axis.normalized * (ang * Mathf.Deg2Rad * 12f);
            wDes = Vector3.ClampMagnitude(wDes, 10f);
            rb.AddTorque(Vector3.ClampMagnitude(wDes - rb.angularVelocity, 150f * dt), ForceMode.VelocityChange);
            // 손에서 빠짐: 막힌 채 너무 멀리 끌면
            if (err.magnitude > holdBreakDist) { breakT += dt; if (breakT > 0.3f) { Drop(); Sfx(sfxLight, 0.5f, 0.7f); SlipCount++; } }
            else breakT = 0f;
        }
        public static int SlipCount;

        // ── [E] 칩 ──""")

# 프롬프트: 감싸기
rep("""            Prompt = ""; PromptAnchor = null;
            if (held != null || lifted != null || moving != null || tool != Tool.None) return;""",
"""            Prompt = ""; PromptAnchor = null;
            if (lifted == null && moving == null && tool == Tool.None && held == null)
            {
                var look = LookAt<PackItem>();
                BubbleSheet sh = null; PackItem target = null;
                if (look != null)
                {
                    if (heldSheet != null && look.sheet == null && !look.Wrapped && look.GetComponent<BoxCollider>() != null) { sh = heldSheet; target = look; }
                    else if (heldSheet == null) FindWrapPair(look, out sh, out target);
                }
                if (sh != null && target != null) { Prompt = "E 감싸기"; PromptAnchor = Bounds(target).center + Vector3.up * (Bounds(target).extents.y + 0.03f); return; }
            }
            if (held != null || lifted != null || moving != null || tool != Tool.None || heldSheet != null || pullSheet != null) return;""")

# 소리: 툭
rep("""        /// <summary>흔드는 중 부딪힘 한 번 → 덜그럭.""",
"""        /// <summary>흔드는 중이 아닐 때 닿음(내려놓기·벽에 부딪힘) → 살짝 툭. 무른 쪽 소리.</summary>
        public static void PlayTouch(PackItem it, PackItem other, float speed)
        {
            float w = other != null ? Mathf.Min(it.SoundWeight, other.SoundWeight) : it.SoundWeight;
            float vol = Mathf.Clamp01((speed - 0.12f) / 0.9f) * (0.25f + 0.75f * w) * 0.55f;
            if (vol < 0.02f) return;
            TouchPlayed++;
            if (w >= 0.9f) Sfx(sfxHard, vol, Random.Range(0.95f, 1.05f));
            else Sfx(sfxLight, vol, Random.Range(0.75f, 0.9f));
        }
        public static int TouchPlayed;

        /// <summary>흔드는 중 부딪힘 한 번 → 덜그럭.""")

open(p,'w',encoding='utf-8').write(s)
print("ok")
