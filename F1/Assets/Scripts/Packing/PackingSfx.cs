using System;
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>
    /// 포장 미니게임 효과음 (설계 2026-10-22 P1).
    ///
    /// 클립 매핑 현황: AudioCueBus 의 Packing cue 4종(MaterialSelect·StrokeStart·StrokeEnd·Confirm)은
    /// 발행되지만 AudioManager 쪽 핸들러가 전부 no-op 이다(W5 TODO). 그래서 이 컴포넌트가 같은 cue 를
    /// 구독해 직접 재생한다. 감기 틱·도장·면 완성은 cue enum 에 없어서 정적 메서드로 직접 부른다.
    /// ※ 나중에 AudioManager 에 Packing 클립을 붙이면 소리가 두 번 난다 — 그때는 여기 구독을 지운다.
    ///
    /// 클립 = 프로젝트에 이미 있는 Kenney interface sounds 를 Resources/WrapMaterials/Sfx 로 복사한 것.
    /// 포장 중엔 timeScale 0 → AudioSource.ignoreListenerPause, 시간은 unscaled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PackingSfx : MonoBehaviour
    {
        const string kDir = "WrapMaterials/Sfx/";

        static PackingSfx instance;

        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.5f;
        [Tooltip("틱 사이 최소 간격(초, unscaled) — 빠르게 끌어도 소리가 뭉개지지 않게.")]
        [SerializeField] private float minTickInterval = 0.045f;
        [SerializeField, Range(0f, 1f)] private float tickVolume = 0.35f;

        // 재료별 피치 — 버블 가볍게 / 폼 보통 / 하드케이스 묵직하게 (가설)
        static readonly float[] kMatPitch = { 1.15f, 1.0f, 0.8f };

        AudioSource[] pool;
        int next;
        float lastTickTime = -10f;

        AudioClip[] tickClips;
        AudioClip strokeStartClip, strokeEndClip, matSelectClip, confirmClip, stampClip, sideOkClip;

        Action<float> _matH, _startH, _endH, _confirmH;

        /// <summary>host 에 하나만 붙인다 (PackingScreen 이 Show 때 호출).</summary>
        public static PackingSfx Ensure(GameObject host)
        {
            if (instance != null) return instance;
            if (host == null) return null;
            var s = host.GetComponent<PackingSfx>();
            if (s == null) s = host.AddComponent<PackingSfx>();
            return s;
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;

            pool = new AudioSource[4];
            for (int i = 0; i < pool.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.ignoreListenerPause = true; // timeScale=0 무관
                src.spatialBlend = 0f;
                pool[i] = src;
            }

            tickClips = new[]
            {
                Load("scratch_001"), Load("scratch_002"), Load("scratch_003"),
            };
            strokeStartClip = Load("drop_002");
            strokeEndClip   = Load("tick_002");
            matSelectClip   = Load("switch_002");
            confirmClip     = Load("confirmation_002");
            stampClip       = Load("drop_004");
            sideOkClip      = Load("select_002");
        }

        static AudioClip Load(string name)
        {
            var c = Resources.Load<AudioClip>(kDir + name);
            if (c == null) Debug.LogWarning($"[PackingSfx] 클립 없음: Resources/{kDir}{name}");
            return c;
        }

        void OnEnable()
        {
            if (instance != this) return; // 중복 컴포넌트(Awake 에서 Destroy 예약)는 구독 안 함
            _matH     = _ => Play(matSelectClip, 0.6f, 1f);
            _startH   = _ => Play(strokeStartClip, 0.45f, UnityEngine.Random.Range(0.95f, 1.05f));
            _endH     = _ => Play(strokeEndClip, 0.5f, 1f);
            _confirmH = _ => Play(confirmClip, 0.7f, 1f);
            AudioCueBus.Subscribe(AudioCueId.PackingMaterialSelect, _matH);
            AudioCueBus.Subscribe(AudioCueId.PackingStrokeStart,    _startH);
            AudioCueBus.Subscribe(AudioCueId.PackingStrokeEnd,      _endH);
            AudioCueBus.Subscribe(AudioCueId.PackingConfirm,        _confirmH);
        }

        void OnDisable()
        {
            AudioCueBus.Unsubscribe(AudioCueId.PackingMaterialSelect, _matH);
            AudioCueBus.Unsubscribe(AudioCueId.PackingStrokeStart,    _startH);
            AudioCueBus.Unsubscribe(AudioCueId.PackingStrokeEnd,      _endH);
            AudioCueBus.Unsubscribe(AudioCueId.PackingConfirm,        _confirmH);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || pool == null) return;
            var src = pool[next];
            next = (next + 1) % pool.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume * masterVolume);
        }

        /// <summary>감기 틱. speed01 = 드래그 속도(0 느림 ~ 1 빠름) → 피치 0.9~1.2 배.</summary>
        public static void PlayTick(int channel, float speed01)
        {
            var s = instance;
            if (s == null || s.tickClips == null || s.tickClips.Length == 0) return;
            float now = Time.unscaledTime;
            if (now - s.lastTickTime < s.minTickInterval) return;
            s.lastTickTime = now;
            float pitch = kMatPitch[Mathf.Clamp(channel, 0, kMatPitch.Length - 1)]
                        * Mathf.Lerp(0.9f, 1.2f, Mathf.Clamp01(speed01));
            var clip = s.tickClips[UnityEngine.Random.Range(0, s.tickClips.Length)];
            s.Play(clip, s.tickVolume * Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(speed01)), pitch);
        }

        /// <summary>완료 도장 '쾅'.</summary>
        public static void PlayStamp()
        {
            if (instance != null) instance.Play(instance.stampClip, 0.9f, 0.85f);
        }

        /// <summary>면 하나가 '충분' 기준을 넘는 순간.</summary>
        public static void PlaySideOk()
        {
            if (instance != null) instance.Play(instance.sideOkClip, 0.55f, 1.1f);
        }
    }
}
