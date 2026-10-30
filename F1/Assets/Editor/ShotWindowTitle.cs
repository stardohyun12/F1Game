// 사본 에디터(D:/Github/F1Game_shot) 창 제목에 표식 — 도현이 본 에디터와 구분하게(10/30). 본 저장소에서는 아무 일도 안 함.
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class ShotWindowTitle
{
    static ShotWindowTitle()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("F1Game_shot")) return;
        EditorApplication.updateMainWindowTitle += d => d.title = "[사본·기능팀 — 코멘트 넣지 말 것] " + d.title;
        EditorApplication.UpdateMainWindowTitle();
    }
}
