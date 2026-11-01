#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// D-248 ③ 검증 봇(에디터·개발 빌드 전용). 소리는 사진에 안 찍히니 숫자와 녹음으로 남긴다.
///   Places(dir): 장소마다 순간이동 → 2초 기다림 → 채널(바람·파도·갈매기)을 하나씩만 켜서 귀(AudioListener)에 실제로 닿는 크기(dBFS) → 다 켜고 5초 녹음(wav).
///   StartLog(path): 1초마다 장소 판정·명령값·귀에 닿는 전체 크기를 한 줄씩(기능 2 봇과 같이 돌려 방→문→골목→카트를 그래프로).
/// 귀 크기는 리스너에 붙인 탭(OnAudioFilterRead = 스피커로 나가기 직전 소리)으로 잰다.
/// </summary>
public class AmbienceProbe : MonoBehaviour
{
    public static AmbienceProbe Instance;
    public static string Status = "";

    /// <summary>리스너 출력 탭: 제곱합(크기)과 녹음 버퍼.</summary>
    public class Tap : MonoBehaviour
    {
        readonly object gate = new object();
        double sum; long count; public List<float> rec; public int channels = 2;
        void OnAudioFilterRead(float[] data, int ch)
        {
            lock (gate)
            {
                channels = ch;
                for (int i = 0; i < data.Length; i++) sum += data[i] * data[i];
                count += data.Length;
                if (rec != null) rec.AddRange(data);
            }
        }
        public void Reset() { lock (gate) { sum = 0; count = 0; } }
        public float Db() { lock (gate) return count == 0 ? -999f : (sum <= 1e-12 ? -120f : 10f * Mathf.Log10((float)(sum / count))); }
        public void StartRec() { lock (gate) rec = new List<float>(48000 * 2 * 6); }
        public float[] StopRec() { lock (gate) { var r = rec?.ToArray(); rec = null; return r; } }
    }

    static Tap EnsureTap()
    {
        var l = FindFirstObjectByType<AudioListener>(); if (l == null) return null;
        if (!l.TryGetComponent(out Tap t)) t = l.gameObject.AddComponent<Tap>();
        return t;
    }

    static AmbienceProbe Host()
    {
        if (Instance == null) { var go = new GameObject("AmbienceProbe (dev)"); DontDestroyOnLoad(go); Instance = go.AddComponent<AmbienceProbe>(); }
        return Instance;
    }

    public struct Spot { public string name; public int place; public Vector3 feet; public float yaw; public Spot(string n, int p, Vector3 f, float y) { name = n; place = p; feet = f; yaw = y; } }

    /// <summary>place ≥ 0 = DebugPlaces 번호, 아니면 feet·yaw 로 직접.</summary>
    public static string Places(string dir, Spot[] spots)
    {
        Application.runInBackground = true;
        Host().StopAllCoroutines(); Host().StartCoroutine(Host().RunPlaces(dir, spots));
        return "places started " + spots.Length;
    }

    IEnumerator RunPlaces(string dir, Spot[] spots)
    {
        Directory.CreateDirectory(dir);
        var z = AmbienceZones.Active; var tap = EnsureTap();
        if (z == null || tap == null) { Status = "no zones/tap"; yield break; }
        var wind = z.wind != null ? z.wind.GetComponent<AudioSource>() : null; var waves = z.coast != null ? z.coast.waves : null; var gulls = z.coast != null ? z.coast.gulls : null;
        var sb = new StringBuilder("[\n");
        for (int s = 0; s < spots.Length; s++)
        {
            var sp = spots[s]; Status = "place " + sp.name;
            if (sp.place >= 0) DebugWindow.GoPlace(sp.place);
            else { var d = FindFirstObjectByType<OnFootDirector>(); d.DebugTeleport(sp.feet, sp.yaw, null, 0f); }
            yield return new WaitForSecondsRealtime(2.0f);
            var ear = FindFirstObjectByType<AudioListener>().transform.position;
            // 채널 하나씩: 나머지 음소거 → 1.2초 평균
            float[] solo = new float[3]; var srcs = new[] { wind, waves, gulls };
            for (int c = 0; c < 3; c++)
            {
                for (int j = 0; j < 3; j++) if (srcs[j] != null) srcs[j].mute = j != c;
                if (c == 2 && z.coast != null) z.coast.CallAt(ear + new Vector3(15f, 10f, 15f));   // 갈매기는 매번 같은 거리(약 23 m)에서 한 번
                yield return new WaitForSecondsRealtime(0.15f); tap.Reset();
                yield return new WaitForSecondsRealtime(c == 2 ? 1.6f : 1.2f);
                solo[c] = tap.Db();
            }
            foreach (var x in srcs) if (x != null) x.mute = false;
            // 다 켜고 5초 녹음(1초에 갈매기 한 번)
            tap.StartRec(); tap.Reset(); float t0 = Time.realtimeSinceStartup; bool called = false;
            while (Time.realtimeSinceStartup - t0 < 5f) { if (!called && Time.realtimeSinceStartup - t0 > 1f && z.coast != null) { z.coast.CallAt(ear + new Vector3(-15f, 10f, 15f)); called = true; } yield return null; }
            float all = tap.Db(); var pcm = tap.StopRec();
            string file = Path.Combine(dir, (s + 1).ToString("00") + "_" + Safe(sp.name) + ".wav");
            WriteWav(file, pcm, tap.channels, AudioSettings.outputSampleRate);
            sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{{\"name\":\"{0}\",\"ear\":[{1:F1},{2:F1},{3:F1}],\"label\":\"{4}\",\"roof\":{5},\"walls\":{6:F2},\"indoor\":{7:F2},\"alley\":{8:F2},\"open\":{9:F2},\"seaDist\":{10:F1},\"speed\":{11:F2}," +
                "\"cmd\":{{\"windDb\":{12:F1},\"waveDb\":{13:F1},\"gullDb\":{14:F1},\"cutoffHz\":{15:F0},\"windVol\":{16:F4},\"waveVol\":{17:F4},\"gullVol\":{18:F3} }}," +
                "\"earDbFS\":{{\"wind\":{19:F1},\"waves\":{20:F1},\"gull\":{21:F1},\"all\":{22:F1} }},\"wav\":\"{23}\"}}{24}\n",
                sp.name, ear.x, ear.y, ear.z, z.Label, z.Roof, z.Walls, z.Indoor, z.Alley, z.Open, Mathf.Min(z.SeaDist, 9999f), z.Speed01,
                z.WindDb, z.WaveDb, z.GullDb, z.CutoffHz, wind ? wind.volume : -1f, waves ? waves.volume : -1f, gulls ? gulls.volume : -1f,
                solo[0], solo[1], solo[2], all, Path.GetFileName(file), s < spots.Length - 1 ? "," : ""));
        }
        sb.Append("]\n");
        File.WriteAllText(Path.Combine(dir, "places.json"), sb.ToString());
        Status = "places done";
    }

    // ── 1초 기록 ──
    bool logging; int lines; float interval = 1f; StringBuilder log; string logPath; string recDir; bool driveRecDone;
    public static string StartLog(string path, string recordDriveDir = null, float interval = 1f)
    {
        Application.runInBackground = true;
        var h = Host(); h.logPath = path; h.interval = interval; h.recDir = recordDriveDir; h.driveRecDone = false; h.log = new StringBuilder("t\tx\ty\tz\tlabel\troof\twalls\tindoor\talley\topen\tseaDist\tspeed\twindDb\twaveDb\tcutoffHz\twindVol\twaveVol\tearDbFS\n");
        h.logging = true; h.StartCoroutine(h.RunLog()); return "log started";
    }
    public static string StopLog() { if (Instance == null) return "none"; Instance.logging = false; File.WriteAllText(Instance.logPath, Instance.log.ToString()); return "log saved " + Instance.logPath; }

    IEnumerator RunLog()
    {
        float t0 = Time.realtimeSinceStartup; var tap = EnsureTap(); if (tap != null) tap.Reset();
        while (logging)
        {
            yield return new WaitForSecondsRealtime(interval);
            var z = AmbienceZones.Active; var l = FindFirstObjectByType<AudioListener>();
            if (z == null || l == null) continue;
            if (tap == null || tap.gameObject != l.gameObject) { tap = EnsureTap(); tap.Reset(); continue; }
            var p = l.transform.position; var w = z.wind ? z.wind.GetComponent<AudioSource>() : null; var wv = z.coast ? z.coast.waves : null;
            log.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F1}\t{1:F1}\t{2:F1}\t{3:F1}\t{4}\t{5}\t{6:F2}\t{7:F2}\t{8:F2}\t{9:F2}\t{10:F1}\t{11:F2}\t{12:F1}\t{13:F1}\t{14:F0}\t{15:F4}\t{16:F4}\t{17:F1}\n",
                Time.realtimeSinceStartup - t0, p.x, p.y, p.z, z.Label, z.Roof, z.Walls, z.Indoor, z.Alley, z.Open, Mathf.Min(z.SeaDist, 9999f), z.Speed01, z.WindDb, z.WaveDb, z.CutoffHz, w ? w.volume : -1f, wv ? wv.volume : -1f, tap.Db()));
            tap.Reset();
            if (recDir != null && !driveRecDone && z.Speed01 > 0.25f) { driveRecDone = true; StartCoroutine(RecDrive(tap)); }
            if (++lines % 10 == 0) File.WriteAllText(logPath, log.ToString());
        }
    }

    IEnumerator RecDrive(Tap tap)
    {
        // 기록용 Reset 과 겹치지 않게 녹음 버퍼만 쓴다
        tap.StartRec(); yield return new WaitForSecondsRealtime(5f);
        var pcm = tap.StopRec(); Directory.CreateDirectory(recDir);
        WriteWav(Path.Combine(recDir, "10_카트주행.wav"), pcm, tap.channels, AudioSettings.outputSampleRate);
    }

    static string Safe(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace(' ', '_').Replace('(', '_').Replace(')', '_'); }

    static void WriteWav(string path, float[] pcm, int ch, int rate)
    {
        if (pcm == null) pcm = new float[0];
        using (var f = new BinaryWriter(File.Create(path)))
        {
            int bytes = pcm.Length * 2;
            f.Write(Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + bytes); f.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            f.Write(16); f.Write((short)1); f.Write((short)ch); f.Write(rate); f.Write(rate * ch * 2); f.Write((short)(ch * 2)); f.Write((short)16);
            f.Write(Encoding.ASCII.GetBytes("data")); f.Write(bytes);
            foreach (var v in pcm) f.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
        }
    }
}
#endif
