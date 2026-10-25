using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 과제 0131(갭 #1 움직임 최소): 인도 위를 순환 경로로 걷는 사람(임시 상자 몸). BayLoop 빌더가 경로·멈춤 지점을 넣는다.
/// NavMesh 가 구워져 있으면 NavMeshAgent 로, 없으면 경로 점 사이를 직접 걷고 땅에 붙인다(같은 경로).
/// 멈춤 지점에선 pauseSec 만큼 서 있거나(앉기 지점이면 몸을 낮춘다), 차가 빠르게 다가오면 길 바깥쪽으로 비켜섰다가 돌아온다.
/// </summary>
public class Pedestrian : MonoBehaviour
{
    public Vector3[] path;
    public int[] pauseAt;          // path 인덱스
    public float[] pauseSec;
    public bool[] sitAt;           // 같은 길이 — 그 멈춤에서 앉는다
    public Vector3[] awayDir;      // 점마다 길 바깥쪽(비켜설 방향)
    public float speed = 1.2f;

    NavMeshAgent agent;
    int idx;
    float wait, dodge;
    Vector3 dodgeTarget;
    Transform body, head, arm;
    Rigidbody car;
    Resident resident;
    float phase;

    void Start()
    {
        phase = Random.value * 10f;
        body = transform.Find("NPC_Body"); head = transform.Find("NPC_Head"); arm = transform.Find("ArmPivot");
        if (path == null || path.Length < 2) { enabled = false; return; }
        if (NavMesh.SamplePosition(transform.position, out var hit, 1.5f, NavMesh.AllAreas))
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = speed; agent.radius = 0.3f; agent.height = 1.7f; agent.angularSpeed = 360f; agent.acceleration = 6f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.Warp(hit.position); agent.SetDestination(path[idx]);
        }
    }

    void Update()
    {
        if (car == null) { var c = FindFirstObjectByType<ArcadeCarController>(); if (c != null) car = c.GetComponent<Rigidbody>(); }
        // 차가 3초 안에 4m 안으로 들어올 것 같으면 비켜선다.
        if (car != null && dodge <= 0f)
        {
            var v = car.linearVelocity; v.y = 0f; var d = transform.position - car.position; d.y = 0f;
            if (v.magnitude > 2f && d.magnitude < 14f && Vector3.Dot(v, d) > 0f && Vector3.Cross(v.normalized, d).magnitude < 4f)
            {
                var away = awayDir != null && awayDir.Length > idx ? awayDir[idx] : Vector3.Cross(Vector3.up, v.normalized);
                dodgeTarget = transform.position + away.normalized * 1.6f; dodge = 2.5f; wait = 0f;
                if (agent != null) agent.SetDestination(dodgeTarget);
            }
        }
        bool moving;
        if (dodge > 0f)
        {
            dodge -= Time.deltaTime;
            if (agent == null) MoveToward(dodgeTarget, speed * 2.2f); else agent.speed = speed * 2.2f;
            moving = (transform.position - dodgeTarget).sqrMagnitude > 0.04f;
            if (dodge <= 0f && agent != null) { agent.speed = speed; agent.SetDestination(path[idx]); }
        }
        else if (wait > 0f) { wait -= Time.deltaTime; moving = false; }
        else
        {
            var target = path[idx];
            bool arrived = agent != null ? !agent.pathPending && agent.remainingDistance < 0.4f : (Flat(transform.position - target)).sqrMagnitude < 0.09f;
            if (arrived)
            {
                int p = pauseAt == null ? -1 : System.Array.IndexOf(pauseAt, idx);
                if (p >= 0) wait = pauseSec[p];
                idx = (idx + 1) % path.Length;
                if (agent != null) agent.SetDestination(path[idx]);
                moving = false;
            }
            else { if (agent == null) MoveToward(target, speed); moving = true; }
        }
        // 보이는 움직임: 걸을 땐 팔 흔들기·몸 오르내림, 앉기 멈춤에선 몸을 낮춘다.
        bool sitting = !moving && wait > 0f && sitAt != null && pauseAt != null && SitNow();
        // 과제 0226: 주민 모델이 붙어 있으면 Animator(Walk/Stand/Sit)가 몸을 움직인다 — 가짜 오르내림·팔은 안 겹친다.
        if (resident != null || (resident = GetComponentInChildren<Resident>()) != null) { resident.Set(moving ? 1 : sitting ? 2 : 0, dodge > 0f ? speed * 2.2f : speed); return; }
        float bob = moving ? Mathf.Abs(Mathf.Sin((Time.time + phase) * 7f)) * 0.04f : 0f, drop = sitting ? 0.3f : 0f;
        if (body != null) body.localPosition = new Vector3(0f, 0.55f - drop + bob, 0f);
        if (head != null) head.localPosition = new Vector3(0f, 1.32f - drop + bob, 0f);
        if (arm != null) arm.localRotation = Quaternion.Euler(moving ? Mathf.Sin((Time.time + phase) * 7f) * 28f : 0f, 0f, 0f);
    }

    bool SitNow()
    {
        int prev = (idx - 1 + path.Length) % path.Length; int p = System.Array.IndexOf(pauseAt, prev);
        return p >= 0 && p < sitAt.Length && sitAt[p];
    }

    void MoveToward(Vector3 t, float spd)
    {
        var d = Flat(t - transform.position); if (d.sqrMagnitude < 1e-4f) return;
        var step = Vector3.ClampMagnitude(d, spd * Time.deltaTime); var p = transform.position + step;
        // 높이: 경로 점 높이(빌더가 인도 높이로 넣음)를 거리 비율로 잇고, 바로 밑에 콜라이더가 있으면 거기 붙인다.
        float full = Flat(t - transform.position).magnitude; p.y = Mathf.Lerp(transform.position.y, t.y, full > 1e-3f ? step.magnitude / full : 1f);
        if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var h, 2f, ~0, QueryTriggerInteraction.Ignore) && h.rigidbody == null && Mathf.Abs(h.point.y - p.y) < 0.4f) p.y = h.point.y;
        transform.position = p;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 8f);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
}
