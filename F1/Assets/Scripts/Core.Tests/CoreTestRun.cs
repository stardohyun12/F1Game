using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>D-287: exec 로 Core EditMode 테스트를 돌리고 결과를 파일로 남긴다. CoreTestRun.Start(path) → Status 가 "done …" 이면 끝.</summary>
public static class CoreTestRun
{
    public static string Status = "idle";

    public static string Start(string outPath)
    {
        Status = "run";
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Cb(outPath));
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "PitCrew.Core.Tests" } }));
        return "started";
    }

    class Cb : ICallbacks
    {
        readonly string path; readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();
        public Cb(string p) { path = p; }
        public void RunStarted(ITestAdaptor t) { sb.AppendLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] 시작 {t.Name} 테스트 {t.TestCaseCount}개"); }
        public void TestStarted(ITestAdaptor t) { }
        public void TestFinished(ITestResultAdaptor r)
        {
            if (r.HasChildren) return;
            sb.AppendLine($"{r.TestStatus,-7} {r.Test.Name}{(string.IsNullOrEmpty(r.Message) ? "" : "  — " + r.Message.Trim())}");
        }
        public void RunFinished(ITestResultAdaptor r)
        {
            sb.AppendLine($"끝: 통과 {r.PassCount} · 실패 {r.FailCount} · 건너뜀 {r.SkipCount} · {r.Duration:F2}s");
            System.IO.File.WriteAllText(path, sb.ToString());
            Status = $"done pass={r.PassCount} fail={r.FailCount}";
        }
    }
}
