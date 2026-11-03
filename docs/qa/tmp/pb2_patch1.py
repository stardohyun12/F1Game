p='F1/Assets/Scripts/Packing/Bench/PackBox.cs'
s=open(p,encoding='utf-8').read()
def rep(a,b):
    global s
    assert a in s, a
    s=s.replace(a,b)
rep("""                if (it.kind == PackItemKind.Eggs) continue;
                cur.softTotal++;
                if (!IsPinned(it)) continue;""","""                if (it.kind == PackItemKind.Eggs) continue;
                cur.softTotal++;
                // 뽁뽁이 시트(2판): 상자 안에서 내려앉으면 이미 상자 몸의 일부(굽힌 칸 콜라이더)
                if (it.sheet != null) { if (it.transform.IsChildOf(transform)) cur.softPinned++; continue; }
                if (!IsPinned(it)) continue;""")
rep("foreach (var it in shakeSet) { it.Body.WakeUp(); }","foreach (var it in shakeSet) { if (it.Body != null) it.Body.WakeUp(); }\n            pairs.Clear();")
rep("bool IsLocked(PackItem it) => it != null && (it.kind == PackItemKind.Eggs ? trayLocked : pinned.Contains(it));",
    "bool IsLocked(PackItem it) => it != null && (it.kind == PackItemKind.Eggs ? trayLocked : pinned.Contains(it) || (it.sheet != null && it.transform.IsChildOf(transform)));")
rep("""            cur.rattle += vol; cur.hits++; cur.peakHit = Mathf.Max(cur.peakHit, vol);""","""            cur.rattle += vol; cur.hits++; cur.peakHit = Mathf.Max(cur.peakHit, vol);
            string key = it.kind + "-" + (other != null ? other.kind.ToString() : (lastHitCollider != null ? lastHitCollider.name : "?"));
            pairs.TryGetValue(key, out var pv); pairs[key] = new Vector2(pv.x + 1f, pv.y + vol);""")
rep("""        public void ReportHit(PackItem it, PackItem other, float relSpeed)
        {""","""        /// <summary>QA: 흔드는 동안 부딪힌 짝별 (횟수, 소리 합) — "Eggs-Lid" 꼴.</summary>
        readonly Dictionary<string, Vector2> pairs = new Dictionary<string, Vector2>();
        public static Collider lastHitCollider;
        public string PairsJson()
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var kv in pairs) sb.Append('"').Append(kv.Key).Append("\\":[").Append(kv.Value.x.ToString("F0")).Append(',').Append(kv.Value.y.ToString("F2")).Append("],");
            if (sb.Length > 1) sb.Length--;
            return sb.Append('}').ToString();
        }

        public void ReportHit(PackItem it, PackItem other, float relSpeed)
        {""")
open(p,'w',encoding='utf-8').write(s)

p='F1/Assets/Scripts/Packing/Bench/PackItem.cs'
s=open(p,encoding='utf-8').read()
rep("            box.ReportHit(this, other, c.relativeVelocity.magnitude);","            PackBox.lastHitCollider = c.collider;\n            box.ReportHit(this, other, c.relativeVelocity.magnitude);")
open(p,'w',encoding='utf-8').write(s)
