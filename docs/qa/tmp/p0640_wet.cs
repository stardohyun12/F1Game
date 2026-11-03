// 과제 0640: p0433_fix.cs 를 WET 로 한 번 돌리게 SessionState 를 켠다(돌린 뒤 p0640_dry.cs 로 다시 DRY)
UnityEditor.SessionState.SetBool("p0433.dry", false); return "dry=false scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " playing=" + Application.isPlaying;
