"""이미지 1장 → Hunyuan3D-2 (ComfyUI 네이티브) → GLB.

사용:
    python -X utf8 tools/asset-gen/hy3d_run.py --image cake_ref.png --name cake --out out/
옵션:
    --steps 20 --cfg 6 --octree 256 --seed 0 --host http://127.0.0.1:8188

ComfyUI 가 먼저 떠 있어야 한다 (D:/ComfyUI/Imag_Generate, standalone-env/python.exe ComfyUI/main.py).
체크포인트: models/checkpoints/hunyuan3d-dit-v2_fp16.safetensors
"""
import argparse, json, os, shutil, sys, time, urllib.request, urllib.parse, uuid, mimetypes

def http_json(url, data=None, method=None):
    req = urllib.request.Request(url, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))

def upload_image(host, path):
    boundary = uuid.uuid4().hex
    name = os.path.basename(path)
    ctype = mimetypes.guess_type(path)[0] or "application/octet-stream"
    body = b"".join([
        f"--{boundary}\r\n".encode(),
        f'Content-Disposition: form-data; name="image"; filename="{name}"\r\n'.encode(),
        f"Content-Type: {ctype}\r\n\r\n".encode(),
        open(path, "rb").read(), b"\r\n",
        f"--{boundary}\r\n".encode(),
        b'Content-Disposition: form-data; name="overwrite"\r\n\r\ntrue\r\n',
        f"--{boundary}--\r\n".encode(),
    ])
    req = urllib.request.Request(f"{host}/upload/image", data=body, method="POST")
    req.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    with urllib.request.urlopen(req, timeout=60) as r:
        info = json.loads(r.read().decode("utf-8"))
    sub = info.get("subfolder", "")
    return f"{sub}/{info['name']}" if sub else info["name"]

def build_graph(image_name, name, steps, cfg, octree, seed, ckpt):
    """템플릿 3d_hunyuan3d_image_to_model.json 과 같은 배선 (API 포맷)."""
    return {
        "54": {"class_type": "ImageOnlyCheckpointLoader", "inputs": {"ckpt_name": ckpt}},
        "56": {"class_type": "LoadImage", "inputs": {"image": image_name, "upload": "image"}},
        "51": {"class_type": "CLIPVisionEncode",
               "inputs": {"clip_vision": ["54", 1], "image": ["56", 0], "crop": "none"}},
        "80": {"class_type": "Hunyuan3Dv2Conditioning", "inputs": {"clip_vision_output": ["51", 0]}},
        "70": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["54", 0], "shift": 1.0}},
        "66": {"class_type": "EmptyLatentHunyuan3Dv2", "inputs": {"resolution": 3072, "batch_size": 1}},
        "3": {"class_type": "KSampler",
              "inputs": {"model": ["70", 0], "positive": ["80", 0], "negative": ["80", 1],
                         "latent_image": ["66", 0], "seed": seed, "steps": steps, "cfg": cfg,
                         "sampler_name": "euler", "scheduler": "normal", "denoise": 1.0}},
        "61": {"class_type": "VAEDecodeHunyuan3D",
               "inputs": {"samples": ["3", 0], "vae": ["54", 2], "num_chunks": 8000,
                          "octree_resolution": octree}},
        "81": {"class_type": "VoxelToMesh", "inputs": {"voxel": ["61", 0], "algorithm": "surface net",
                                                       "threshold": 0.6}},
        "82": {"class_type": "SaveGLB", "inputs": {"mesh": ["81", 0], "filename_prefix": f"mesh/{name}"}},
    }

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--image", required=True)
    ap.add_argument("--name", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--steps", type=int, default=20)
    ap.add_argument("--cfg", type=float, default=6.0)
    ap.add_argument("--octree", type=int, default=256)
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--host", default="http://127.0.0.1:8188")
    ap.add_argument("--ckpt", default="hunyuan3d-dit-v2_fp16.safetensors")
    ap.add_argument("--comfy-output", default="D:/ComfyUI/Imag_Generate/ComfyUI/output")
    a = ap.parse_args()

    try:
        http_json(f"{a.host}/system_stats")
    except Exception as e:
        print(f"ComfyUI 가 안 떠 있다 ({a.host}): {e}"); sys.exit(2)

    img = upload_image(a.host, a.image)
    graph = build_graph(img, a.name, a.steps, a.cfg, a.octree, a.seed, a.ckpt)
    t0 = time.time()
    res = http_json(f"{a.host}/prompt", json.dumps({"prompt": graph, "client_id": uuid.uuid4().hex}).encode())
    if "prompt_id" not in res:
        print("큐 실패:", json.dumps(res, ensure_ascii=False, indent=1)); sys.exit(3)
    pid = res["prompt_id"]
    print(f"queued {pid}  (steps={a.steps} cfg={a.cfg} octree={a.octree} seed={a.seed})")

    while True:
        time.sleep(3)
        hist = http_json(f"{a.host}/history/{pid}")
        if pid in hist:
            break
        q = http_json(f"{a.host}/queue")
        running = any(item[1] == pid for item in q.get("queue_running", []))
        pending = any(item[1] == pid for item in q.get("queue_pending", []))
        if not running and not pending:
            print("큐에서 사라졌는데 history 에 없음 — 서버 로그 확인"); sys.exit(4)
        print(f"  ... {time.time()-t0:5.0f}s", end="\r")
    h = hist[pid]
    st = h.get("status", {})
    if st.get("status_str") == "error":
        print("\n실행 에러:", json.dumps(st.get("messages", [])[-1:], ensure_ascii=False)); sys.exit(5)

    files = []
    for node_out in h.get("outputs", {}).values():
        for v in node_out.values():
            if isinstance(v, list):
                for f in v:
                    if isinstance(f, dict) and str(f.get("filename", "")).endswith(".glb"):
                        files.append(f)
    if not files:
        print("\nGLB 출력이 history 에 없음:", json.dumps(h.get("outputs"), ensure_ascii=False)[:500]); sys.exit(6)
    os.makedirs(a.out, exist_ok=True)
    for f in files:
        src = os.path.join(a.comfy_output, f.get("subfolder", ""), f["filename"])
        dst = os.path.join(a.out, f"{a.name}_raw.glb")
        shutil.copyfile(src, dst)
        print(f"\nOK {time.time()-t0:.0f}s  {dst}  ({os.path.getsize(dst)/1e6:.1f} MB)")

if __name__ == "__main__":
    main()
