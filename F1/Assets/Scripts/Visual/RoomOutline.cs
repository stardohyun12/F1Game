using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 과제 20261104-1449(D-297 그림 문법 B안 = Valley Peaks 식): 방 안 얇은 어두운 윤곽선 1px.
/// 저해상 카메라(LowResRenderTest 가 RT 로 그리는 카메라)가 방 상자 안에 있을 때만, 그 카메라가 다 그린 뒤
/// ① 방이 든 집의 렌더러만 법선·눈 거리 RT 에 따로 그리고 ② 깊이 끊김·면 꺾임 픽셀을 어둡게 덮는다. 방 밖·다른 카메라는 손대지 않는다.
/// 켜고 끄는 건 A7ShadeGlobals.roomOutline 하나(끄면 = A안, 윤곽선 없음).
/// </summary>
public sealed class RoomOutline
{
    readonly Transform room;
    Matrix4x4 box;
    bool hasBox;
    Material ndMat, edgeMat;
    readonly List<Renderer> rs = new List<Renderer>();
    readonly CommandBuffer cb = new CommandBuffer { name = "RoomOutline" };
    public float depthRatio = 0.015f, creaseDot = 0.8f, dark = 0.15f, ink = 0f;
    public Color inkColor = new Color(0.17f, 0.14f, 0.13f);
    /// <summary>마지막 프레임에 선을 그렸나(검사·캡처용).</summary>
    public bool DrewLastFrame { get; private set; }

    public RoomOutline(Transform room) { this.room = room; }

    /// <summary>인스펙터 값이 바뀌었나(바뀌면 다시 만든다).</summary>
    public bool Dirty(A7ShadeGlobals g) => g.outlineDepth != depthRatio || g.outlineCrease != creaseDot || g.outlineDark != dark || g.outlineInk != ink;

    public void Enable()
    {
        ndMat = new Material(Resources.Load<Shader>("RoomOutlineND")) { hideFlags = HideFlags.HideAndDontSave };
        edgeMat = new Material(Resources.Load<Shader>("RoomOutlineEdge")) { hideFlags = HideFlags.HideAndDontSave };
        rs.Clear();
        rs.AddRange(room.parent.GetComponentsInChildren<Renderer>(false));
        hasBox = A7ShadeGlobals.TryRoomBox(room, out box);
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    public void Disable()
    {
        RenderPipelineManager.endCameraRendering -= OnEnd;
        if (ndMat != null) Object.DestroyImmediate(ndMat);
        if (edgeMat != null) Object.DestroyImmediate(edgeMat);
        DrewLastFrame = false;
    }

    public bool CameraInside(Camera cam)
    {
        if (!hasBox) hasBox = A7ShadeGlobals.TryRoomBox(room, out box);
        if (!hasBox) return false;
        var q = box.MultiplyPoint3x4(cam.transform.position);
        return Mathf.Abs(q.x) <= 1f && Mathf.Abs(q.y) <= 1f && Mathf.Abs(q.z) <= 1f;
    }

    void OnEnd(ScriptableRenderContext ctx, Camera cam)
    {
        var target = cam.targetTexture;
        if (target == null || cam.GetComponent<LowResRenderTest>() == null) return;
        DrewLastFrame = false;
        if (ndMat == null || edgeMat == null || !CameraInside(cam)) return;
        int w = target.width, h = target.height;
        cb.Clear();
        int nd = Shader.PropertyToID("_RoomND"), tmp = Shader.PropertyToID("_RoomOutlineTmp");
        cb.GetTemporaryRT(nd, new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGBHalf, 24) { msaaSamples = 1 }, FilterMode.Point);
        cb.GetTemporaryRT(tmp, new RenderTextureDescriptor(w, h, target.format, 0) { msaaSamples = 1, sRGB = target.sRGB }, FilterMode.Point);
        cb.SetRenderTarget(nd);
        cb.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        cb.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
        foreach (var r in rs)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
            if (r.sharedMaterial == null || r.sharedMaterial.renderQueue >= 2500) continue;   // 유리(반투명)는 빼서 창밖과의 경계에 선이 생기게
            int n = r.sharedMaterials.Length;
            for (int s = 0; s < n; s++) cb.DrawRenderer(r, ndMat, s, 0);
        }
        cb.SetGlobalTexture(nd, nd);
        cb.SetGlobalVector("_RoomNDTexel", new Vector4(1f / w, 1f / h, w, h));
        cb.SetGlobalVector("_RoomEdge", new Vector4(depthRatio, creaseDot, dark, ink));
        cb.SetGlobalColor("_RoomInk", inkColor);
        cb.Blit(target, tmp);
        cb.Blit(tmp, target, edgeMat);
        cb.ReleaseTemporaryRT(nd); cb.ReleaseTemporaryRT(tmp);
        ctx.ExecuteCommandBuffer(cb);
        ctx.Submit();
        DrewLastFrame = true;
    }
}
