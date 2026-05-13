using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전역 오디오 매니저 (Phase 4 — AudioCueBus 디커플링 적용).
///
/// 인프라만 + AudioCueBus 구독. 도현은 인스펙터에서 AudioClip 6종 할당하면 작동.
///
/// 후크 (AudioCueId → SFX):
/// - CargoImpact       → cargoImpactClip   (volume = clamp01(speedKmh / 50))
/// - CargoTotaled      → cargoTotaledClip
/// - DeliveryCompleted → deliveryClip
/// - UpgradePurchased  → upgradeBoughtClip
/// - RunResultSuccess  → resultSuccessClip
/// - RunResultFail     → resultFailClip
///
/// AudioManager는 시스템(CargoBox/Delivery/Upgrade)을 직접 알지 않는다.
/// 새 SFX = enum 항목 + 발행 한 줄 + Subscribe 한 줄.
///
/// timeScale=0(정산 화면)에서도 작동. AudioSource.ignoreListenerPause=true.
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips (도현 인스펙터 할당)")]
    [SerializeField] private AudioClip cargoImpactClip;
    [SerializeField] private AudioClip cargoTotaledClip;
    [SerializeField] private AudioClip deliveryClip;
    [SerializeField] private AudioClip upgradeBoughtClip;
    [SerializeField] private AudioClip resultSuccessClip;
    [SerializeField] private AudioClip resultFailClip;

    [Header("Music")]
    [SerializeField] private AudioClip runMusicClip;
    // 도현 #3-2 (세션 16): 소리 너무 큼 + 실제같음. 음악 0.4→0.18.
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.18f;

    [Header("Pool")]
    [Tooltip("동시에 재생 가능한 SFX 수.")]
    [SerializeField] private int sfxPoolSize = 6;
    // 도현 #3-2: SFX 1.0→0.55 (전반 인하). 카툰 톤이라 실제 음원 그대로면 위화감.
    [SerializeField, Range(0f, 1f)] private float sfxMasterVolume = 0.55f;

    AudioSource[] sfxSources;
    AudioSource musicSource;
    int sfxIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // SFX 풀
        sfxSources = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.ignoreListenerPause = true; // timeScale=0 무관
            sfxSources[i] = src;
        }

        // Music
        var mgo = new GameObject("Music");
        mgo.transform.SetParent(transform);
        musicSource = mgo.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        // AudioCueBus 구독
        SubscribeCues();
    }

    void OnDestroy()
    {
        UnsubscribeCues();
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (runMusicClip != null) PlayMusic(runMusicClip);
    }

    // cue 핸들러는 Unsubscribe를 위해 필드로 보존(람다 캡처 문제 회피).
    Action<float> _cargoImpactH, _cargoTotaledH, _deliveryH, _upgradeH, _resultSuccessH, _resultFailH;
    // Packing cue 핸들러 (W5 폴리시 때 clip 할당 예정 — 현재 no-op)
    Action<float> _packingMaterialH, _packingStrokeStartH, _packingStrokeEndH, _packingConfirmH;

    void SubscribeCues()
    {
        _cargoImpactH    = v => PlayClip(cargoImpactClip, Mathf.Clamp01(v / 50f));
        _cargoTotaledH   = _ => PlayClip(cargoTotaledClip);
        _deliveryH       = _ => PlayClip(deliveryClip);
        _upgradeH        = _ => PlayClip(upgradeBoughtClip);
        _resultSuccessH  = _ => PlayClip(resultSuccessClip);
        _resultFailH     = _ => PlayClip(resultFailClip);

        // TODO W5 polish: 클립 할당 후 아래 no-op 교체
        _packingMaterialH    = _ => { /* TODO W5: assign material-select click clip */ };
        _packingStrokeStartH = _ => { /* TODO W5: assign stroke-start click clip */ };
        _packingStrokeEndH   = _ => { /* TODO W5: assign stroke-end click clip */ };
        _packingConfirmH     = _ => { /* TODO W5: assign confirm clip */ };

        AudioCueBus.Subscribe(AudioCueId.CargoImpact,          _cargoImpactH);
        AudioCueBus.Subscribe(AudioCueId.CargoTotaled,         _cargoTotaledH);
        AudioCueBus.Subscribe(AudioCueId.DeliveryCompleted,    _deliveryH);
        AudioCueBus.Subscribe(AudioCueId.UpgradePurchased,     _upgradeH);
        AudioCueBus.Subscribe(AudioCueId.RunResultSuccess,     _resultSuccessH);
        AudioCueBus.Subscribe(AudioCueId.RunResultFail,        _resultFailH);
        AudioCueBus.Subscribe(AudioCueId.PackingMaterialSelect, _packingMaterialH);
        AudioCueBus.Subscribe(AudioCueId.PackingStrokeStart,   _packingStrokeStartH);
        AudioCueBus.Subscribe(AudioCueId.PackingStrokeEnd,     _packingStrokeEndH);
        AudioCueBus.Subscribe(AudioCueId.PackingConfirm,       _packingConfirmH);
    }

    void UnsubscribeCues()
    {
        AudioCueBus.Unsubscribe(AudioCueId.CargoImpact,          _cargoImpactH);
        AudioCueBus.Unsubscribe(AudioCueId.CargoTotaled,         _cargoTotaledH);
        AudioCueBus.Unsubscribe(AudioCueId.DeliveryCompleted,    _deliveryH);
        AudioCueBus.Unsubscribe(AudioCueId.UpgradePurchased,     _upgradeH);
        AudioCueBus.Unsubscribe(AudioCueId.RunResultSuccess,     _resultSuccessH);
        AudioCueBus.Unsubscribe(AudioCueId.RunResultFail,        _resultFailH);
        AudioCueBus.Unsubscribe(AudioCueId.PackingMaterialSelect, _packingMaterialH);
        AudioCueBus.Unsubscribe(AudioCueId.PackingStrokeStart,   _packingStrokeStartH);
        AudioCueBus.Unsubscribe(AudioCueId.PackingStrokeEnd,     _packingStrokeEndH);
        AudioCueBus.Unsubscribe(AudioCueId.PackingConfirm,       _packingConfirmH);
    }

    public void PlayResultSfx(bool success)
    {
        AudioCueBus.Publish(success ? AudioCueId.RunResultSuccess : AudioCueId.RunResultFail);
    }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSources == null || sfxSources.Length == 0) return;
        var src = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        src.volume = volume * sfxMasterVolume;
        src.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = false)
    {
        if (clip == null) { StopMusic(); return; }
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}
