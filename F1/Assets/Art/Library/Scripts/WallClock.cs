using UnityEngine;

// artlive 1857: 벽시계 — 시침·분침을 문자판 법선 둘레로 돌린다 (도현 뷰어 코멘트 "시계는 나중에 돌아갈 수 있도록 실제 시계처럼").
// 바늘 메시는 12시를 가리키게 만들어져 있고, 피벗 = 시계 중심. axisLocal = 바늘 로컬 공간에서 본 문자판 법선(보는 사람 쪽).
// 실시간(PC 시계) 또는 게임 시간(게임 1초 = gameMinutesPerSecond 분) 중 고른다. 다른 스크립트가 시각을 정하려면 Show(분).
public class WallClock : MonoBehaviour
{
    public enum Source { RealTime, GameTime }
    public Source source = Source.RealTime;
    [Tooltip("GameTime: 게임 1초 = 시계 몇 분")] public float gameMinutesPerSecond = 1f;
    [Tooltip("GameTime: 시작 시각(시)")] public float startHour = 9f;
    public Transform hourHand, minuteHand;
    [Tooltip("바늘 로컬 공간의 회전축(문자판 법선)")] public Vector3 axisLocal = Vector3.forward;
    [SerializeField, HideInInspector] Quaternion hourRest = Quaternion.identity, minRest = Quaternion.identity;
    [SerializeField, HideInInspector] bool restSaved;
    float gameMinutes;

    void Awake()
    {
        SaveRest();
        gameMinutes = startHour * 60f;
    }

    // 바늘이 12시에 있을 때의 회전을 한 번 기억한다(셋업 스크립트가 에디터에서 불러도 된다).
    public void SaveRest()
    {
        if (restSaved) return;
        if (!hourHand) hourHand = transform.Find("CLOCK_hand_hour");
        if (!minuteHand) minuteHand = transform.Find("CLOCK_hand_min");
        if (hourHand) hourRest = hourHand.localRotation;
        if (minuteHand) minRest = minuteHand.localRotation;
        restSaved = true;
    }

    void Update()
    {
        if (source == Source.RealTime)
        {
            var t = System.DateTime.Now;
            Show(t.Hour * 60f + t.Minute + t.Second / 60f);
        }
        else
        {
            gameMinutes += Time.deltaTime * gameMinutesPerSecond;
            Show(gameMinutes);
        }
    }

    // minutes = 0시부터 센 분. 시침 = 12시간에 한 바퀴, 분침 = 60분에 한 바퀴, 시계 방향.
    public void Show(float minutes)
    {
        SaveRest();
        float hourDeg = (minutes / 60f % 12f) * 30f;
        float minDeg = (minutes % 60f) * 6f;
        if (hourHand) hourHand.localRotation = hourRest * Quaternion.AngleAxis(-hourDeg, axisLocal);
        if (minuteHand) minuteHand.localRotation = minRest * Quaternion.AngleAxis(-minDeg, axisLocal);
    }
}
