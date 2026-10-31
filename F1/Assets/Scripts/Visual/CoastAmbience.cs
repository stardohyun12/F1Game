using UnityEngine;

/// <summary>
/// D-227 해안 소리: 파도(계속·3D, 물가에서 커짐) + 갈매기(가끔 — 20~40초에 한 번, 작게. 도현 "조금"). 바람은 WindAmbience(Amb_Wind)가 따로.
/// 소리 파일은 F1/Assets/Audio/Ambience/(research 팀 CC0) 이 들어오면 슬롯에 넣는다 — 비면 조용하다(코드로 가짜 파도·갈매기를 만들지 않음).
/// </summary>
public class CoastAmbience : MonoBehaviour
{
    public AudioSource waves;          // Amb_Waves: loop, 3D
    public AudioSource gulls;          // Amb_Gulls: 한 번씩, 3D(머리 위 먼 데)
    public AudioClip[] gullClips;
    public Vector2 gullInterval = new Vector2(20f, 40f);
    [Range(0f, 1f)] public float gullVolume = 0.22f;
    [Tooltip("갈매기가 우는 자리: 이 오브젝트 둘레 반경(m)·높이(m).")]
    public float gullRadius = 25f, gullHeight = 12f;

    float next;
    public int GullCalls { get; private set; }   // 검증용
    public float LastCallTime { get; private set; } = -1f;   // 검증용
    public Vector3 LastCallPos { get; private set; }
    // D-236 9판: 이게 켜져 있으면 울음 시각·자리를 그쪽(노을 갈매기 무리)이 정한다 — 혼자 울지 않음
    [System.NonSerialized] public Behaviour leader;

    void Start()
    {
        next = Time.time + Random.Range(gullInterval.x * 0.4f, gullInterval.y * 0.6f);   // 첫 울음은 조금 일찍
        if (waves != null && waves.clip != null) { waves.loop = true; if (!waves.isPlaying) waves.Play(); }
    }

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + Random.Range(gullInterval.x, gullInterval.y);
        if (leader != null && leader.isActiveAndEnabled) return;
        Vector2 o = Random.insideUnitCircle.normalized * gullRadius * Random.Range(0.6f, 1f);
        CallAt(transform.position + new Vector3(o.x, gullHeight, o.y));
    }

    /// <summary>그 자리에서 갈매기 한 번 울음(3D). 노을 갈매기 무리가 화면을 지날 때 부른다.</summary>
    public void CallAt(Vector3 pos)
    {
        next = Time.time + Random.Range(gullInterval.x, gullInterval.y);
        if (gulls == null || gullClips == null || gullClips.Length == 0) return;
        var c = gullClips[Random.Range(0, gullClips.Length)]; if (c == null) return;
        gulls.transform.position = pos;
        gulls.pitch = Random.Range(0.92f, 1.08f);
        gulls.PlayOneShot(c, gullVolume);
        GullCalls++; LastCallTime = Time.time; LastCallPos = pos;
    }
}
