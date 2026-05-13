using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>한 stroke = 한 ribbon. 마우스 드래그 경로의 sample 받아 mesh strip 생성.</summary>
    public sealed class WrapRibbon : MonoBehaviour
    {
        [SerializeField] private float widthMeters = 0.15f;
        [SerializeField] private float surfaceOffsetMeters = 0.01f;
        [SerializeField] private float minSampleDistMeters = 0.015f;

        MeshFilter mf;
        MeshRenderer mr;
        Mesh mesh;

        readonly List<Vector3> verts = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uvs = new();
        readonly List<int> tris = new();

        Vector3 lastLocalPos;
        Vector3 lastLocalNormal;
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

        public void AddSample(Vector3 worldPos, Vector3 worldNormal)
        {
            var localPos = transform.parent != null ? transform.parent.InverseTransformPoint(worldPos) : worldPos;
            var localNormal = transform.parent != null ? transform.parent.InverseTransformDirection(worldNormal).normalized : worldNormal.normalized;

            if (!hasLast)
            {
                lastLocalPos = localPos;
                lastLocalNormal = localNormal;
                hasLast = true;
                return;
            }

            var delta = localPos - lastLocalPos;
            float dist = delta.magnitude;
            if (dist < minSampleDistMeters) return;
            var dir = delta / dist;

            // Lateral perpendicular to drag direction AND surface normal
            var lat = Vector3.Cross(localNormal, dir);
            if (lat.sqrMagnitude < 1e-6f) lat = Vector3.Cross(Vector3.up, dir);
            lat = lat.normalized;

            float w = widthMeters * 0.5f;
            float o = surfaceOffsetMeters;

            // If first segment, add prev endpoint
            if (verts.Count == 0)
            {
                verts.Add(lastLocalPos + lat * w + lastLocalNormal * o);
                verts.Add(lastLocalPos - lat * w + lastLocalNormal * o);
                normals.Add(lastLocalNormal);
                normals.Add(lastLocalNormal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
            }

            uAccum += dist;
            verts.Add(localPos + lat * w + localNormal * o);
            verts.Add(localPos - lat * w + localNormal * o);
            normals.Add(localNormal);
            normals.Add(localNormal);
            uvs.Add(new Vector2(uAccum, 0f));
            uvs.Add(new Vector2(uAccum, 1f));

            int currR = verts.Count - 1;
            int currL = verts.Count - 2;
            int prevR = verts.Count - 3;
            int prevL = verts.Count - 4;
            tris.Add(prevL); tris.Add(currL); tris.Add(prevR);
            tris.Add(prevR); tris.Add(currL); tris.Add(currR);

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            lastLocalPos = localPos;
            lastLocalNormal = localNormal;
        }

        public bool HasGeometry => verts.Count > 0;

        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
