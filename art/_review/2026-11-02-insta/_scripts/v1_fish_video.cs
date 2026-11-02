// 15초 영상: 앉은 시점. Time.captureFramerate=30 → 게임 시간이 프레임당 1/30 s 로 고르게 흐른다(영상 시간 = 게임 시간). 소리는 못 담는다(에디터 안 오디오 녹음 수단 없음).
const string FF = "C:/Users/rtx40/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-8.1.2-full_build/bin/ffmpeg.exe";
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
if (d.State != OnFootDirector.Mode.Seated) return "FAIL: 앉아 있지 않음 " + d.State;
System.IO.Directory.CreateDirectory(OUT + "video");
System.Diagnostics.Process Spawn(int w, int h, string file) {
  var psi = new System.Diagnostics.ProcessStartInfo(FF, "-y -f rawvideo -pix_fmt rgb24 -video_size " + w + "x" + h + " -framerate 30 -i - -an -vf vflip,format=yuv420p -c:v libx264 -preset slow -crf 14 -movflags +faststart \"" + OUT + "video/" + file + "\"")
    { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
  return System.Diagnostics.Process.Start(psi);
}
var p45 = Spawn(1080, 1350, "fishing_sunset_15s_1080x1350.mp4"); var p11 = Spawn(1080, 1080, "fishing_sunset_15s_1080x1080.mp4");
int total = 450, n = 0, last = -1; float t0 = Time.time; var sw = System.Diagnostics.Stopwatch.StartNew();
Time.captureFramerate = 30;
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  if (Time.frameCount == last) return; last = Time.frameCount;
  var a = Grab(360, 450, 3, null, null, 0f); var b = Grab(360, 360, 3, null, null, 0f);
  var ba = a.GetRawTextureData(); p45.StandardInput.BaseStream.Write(ba, 0, ba.Length);
  var bb = b.GetRawTextureData(); p11.StandardInput.BaseStream.Write(bb, 0, bb.Length);
  UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
  n++;
  if (n >= total) {
    UnityEditor.EditorApplication.update -= cb; Time.captureFramerate = 0;
    p45.StandardInput.Close(); p11.StandardInput.Close();
    UnityEditor.SessionState.SetString("insta.video", "done frames=" + n + " gameSec=" + (Time.time - t0).ToString("F2") + " wallSec=" + sw.Elapsed.TotalSeconds.ToString("F1"));
  }
};
UnityEditor.SessionState.SetString("insta.video", "running");
UnityEditor.EditorApplication.update += cb;
return "started";
