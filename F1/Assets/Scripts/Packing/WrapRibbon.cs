using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>
    /// 한 stroke = 한 ribbon. 단면이 베개형 (L=surface, M=lift, R=surface).
    /// lift는 AddSample마다 다르게 — 같은 자리 두 번 감으면 두 번째 stroke이 더 솟음.
    /// </summary>
    public sealed class WrapRibbon : MonoBehaviour
    {
        [SerializeField] private float widthMeters = 0.18f;
        [SerializeField] private float minSampleDistMeters = 0.012f;

        const int CrossN = 5;  // 단면 vertex 수 — 반원형 베개 단면

        MeshFilter mf;
        MeshRenderer mr;
        Mesh mesh;

        readonly List<Vector3> verts = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uvs = new();
        readonly List<int> tris = new();

        Vector3 lastLocalPos;
        Vector3 lastLocalNormal;
        Vector3 lastLat;
        float lastLift;
        bool hasLast;
        float uAccum;

        public void Init(Material mat, float width)
        {
            widthMeters = width;
            mf = gameObject.AddComponent<MeshFilter>();
            mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mesh = new Mesh { name = "WrapRibbonMesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mf.sharedMesh = mesh;
        }

        public void AddSample(Vector3 worldPos, Vector3 worldNormal, float lift)
        {
            var localPos = transform.parent != null ? transform.parent.InverseTransformPoint(worldPos) : worldPos;
            var localNormal = transform.parent != null ? transform.parent.InverseTransformDirection(worldNormal).normalized : worldNormal.normalized;

            if (!hasLast)
            {
                lastLocalPos = localPos;
                lastLocalNormal = localNormal;
                lastLift = lift;
                hasLast = true;
                return;
            }

            AppendSegment(localPos, localNormal, lift);
        }

        void AppendSegment(Vector3 localPos, Vector3 localNormal, float lift)
        {
            var delta = localPos - lastLocalPos;
            float dist = delta.magnitude;
            if (dist < minSampleDistMeters) return;
            var dir = delta / dist;

            var lat = Vector3.Cross(localNormal, dir);
            if (lat.sqrMagnitude < 1e-6f) lat = Vector3.Cross(Vector3.up, dir);
            lat = lat.normalized;

            if (verts.Count > 0 && Vector3.Dot(lat, lastLat) < 0f) lat = -lat;
            lastLat = lat;

            // 현재 cross-section의 기준 normal도 직전과 보간 — 모서리 음영 자연스럽게.
            Vector3 smoothedNormal = (verts.Count == 0)
                ? localNormal
                : Vector3.Slerp(lastLocalNormal, localNormal, 0.5f).normalized;

            float w = widthMeters * 0.5f;

            if (verts.Count == 0)
            {
                EmitCross(lastLocalPos, lastLocalNormal, lat, lastLift, w, 0f);
            }

            uAccum += dist;
            EmitCross(localPos, smoothedNormal, lat, lift, w, uAccum);

            int total = verts.Count;
            int prevBase = total - 2 * CrossN;
            int curBase = total - CrossN;
            for (int i = 0; i < CrossN - 1; i++)
            {
                int pi  = prevBase + i;
                int pi1 = prevBase + i + 1;
                int ci  = curBase + i;
                int ci1 = curBase + i + 1;
                tris.Add(pi);  tris.Add(ci);  tris.Add(pi1);
                tris.Add(pi1); tris.Add(ci);  tris.Add(ci1);
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            lastLocalPos = localPos;
            lastLocalNormal = localNormal;
            lastLift = lift;
        }

        void EmitCross(Vector3 pos, Vector3 nrm, Vector3 lat, float lift, float halfW, float u)
        {
            // 반원형 단면 — 위치는 lat 방향, 높이는 cos, 법선은 회전
            for (int i = 0; i < CrossN; i++)
            {
                float f = (2f * i / (CrossN - 1)) - 1f;  // -1..+1
                float a = f * (Mathf.PI * 0.5f);          // -π/2 .. +π/2
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                var v = pos + lat * (f * halfW) + nrm * (lift * c);
                verts.Add(v);
                var n = (nrm * c + lat * s).normalized;
                normals.Add(n);
                uvs.Add(new Vector2(u, (float)i / (CrossN - 1)));
            }
        }

        public bool HasGeometry => verts.Count > 0;

        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
