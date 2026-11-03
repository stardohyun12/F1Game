using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        // ── 소리(기존 Kenney CC0 클립 재사용 — Resources/WrapMaterials/Sfx) ──
        static AudioSource[] pool; static int poolNext; static float lastRattle;
        static AudioClip sfxHard, sfxLight, sfxPick, sfxClose, sfxDeny, sfxDone, sfxBubble;
        static AudioClip[] sfxScratch;
        /// <summary>QA: 이번 흔들기에서 실제로 낸 덜그럭 소리 수·크기 합.</summary>
        public static int RattlePlayed; public static float RattleVolumeSum;

        void EnsureSfx()
        {
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0f; pool[i] = s; }
            const string dir = "WrapMaterials/Sfx/";
            sfxHard = Resources.Load<AudioClip>(dir + "drop_002");
            sfxLight = Resources.Load<AudioClip>(dir + "tick_002");
            sfxPick = Resources.Load<AudioClip>(dir + "switch_002");
            sfxClose = Resources.Load<AudioClip>(dir + "drop_004");
            sfxDeny = Resources.Load<AudioClip>(dir + "tick_002");
            sfxDone = Resources.Load<AudioClip>(dir + "select_002");
            sfxBubble = Resources.Load<AudioClip>(dir + "scratch_003");
            sfxScratch = new[] { Resources.Load<AudioClip>(dir + "scratch_001"), Resources.Load<AudioClip>(dir + "scratch_002"), Resources.Load<AudioClip>(dir + "scratch_003") };
        }

        static void Sfx(AudioClip c, float vol, float pitch)
        {
            if (c == null || pool == null) return;
            var s = pool[poolNext]; poolNext = (poolNext + 1) % pool.Length;
            s.pitch = pitch; s.PlayOneShot(c, vol);
        }

        /// <summary>흔드는 중이 아닐 때 닿음(내려놓기·벽에 부딪힘) → 살짝 툭. 무른 쪽 소리.</summary>
        public static void PlayTouch(PackItem it, PackItem other, float speed)
        {
            float w = other != null ? Mathf.Min(it.SoundWeight, other.SoundWeight) : it.SoundWeight;
            float vol = Mathf.Clamp01((speed - 0.12f) / 0.9f) * (0.25f + 0.75f * w) * 0.55f;
            if (vol < 0.02f) return;
            TouchPlayed++;
            if (w >= 0.9f) Sfx(sfxHard, vol, Random.Range(0.95f, 1.05f));
            else Sfx(sfxLight, vol, Random.Range(0.75f, 0.9f));
        }
        public static int TouchPlayed;

        /// <summary>흔드는 중 부딪힘 한 번 → 덜그럭. 크기 = 부딪힌 세기 × 물건 무게(PackItem.SoundWeight).</summary>
        public static void PlayRattle(PackItem it, float vol)
        {
            if (Time.time - lastRattle < 0.025f) return;
            lastRattle = Time.time;
            RattlePlayed++; RattleVolumeSum += vol;
            if (it.kind == PackItemKind.Eggs) Sfx(sfxHard, Mathf.Clamp01(vol * 1.2f), Random.Range(1.15f, 1.35f));
            else if (it.kind == PackItemKind.Bubble) Sfx(sfxLight, vol, Random.Range(0.8f, 0.95f));
            else Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], vol * 0.8f, Random.Range(1.3f, 1.6f));
        }
    }
}
