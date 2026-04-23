using UnityEngine;

/// <summary>
/// UI SFX 재생 헬퍼. 씬에 1개 자동 생성 (lazy). UITheme의 클립 사용.
/// </summary>
public class UISounds : MonoBehaviour
{
    AudioSource src;
    static UISounds _inst;

    public static UISounds Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("UISounds");
                _inst = go.AddComponent<UISounds>();
                DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.volume = 0.7f;
    }

    public static void Click() => PlayClip(UITheme.Active?.sfxClick);
    public static void Back() => PlayClip(UITheme.Active?.sfxBack);
    public static void Confirm() => PlayClip(UITheme.Active?.sfxConfirm);
    public static void Shift() => PlayClip(UITheme.Active?.sfxShift);

    static void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        Instance.src.PlayOneShot(clip);
    }
}
