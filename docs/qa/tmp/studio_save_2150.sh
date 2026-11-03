#!/usr/bin/env bash
# 2150 마무리: F1Studio 씬 저장 + Play 꺼짐 확인 (메인 F1 에디터엔 안 보냄)
export UCLI_PROJ=D:/Github/F1Game/F1Studio
bash scripts/ucli-http.sh exec 'UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes(); return UnityEditor.EditorApplication.isPlaying + " " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
