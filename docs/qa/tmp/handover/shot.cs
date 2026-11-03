// 이름 = tag.txt 의 태그 + 순번(이미 있는 같은 태그 파일 수). 같은 태그로 다시 돌리기 전엔 그 파일들을 지운다.
string tag = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/handover/tag.txt").Trim();
string dirp = "D:/Github/F1Game/docs/qa/evidence/handover/";
int n = System.IO.Directory.GetFiles(dirp, tag + "_*.png").Length;
string name = tag + "_" + n;
System.IO.File.WriteAllText(dirp + tag + "_" + n + ".png", "");   // 다음 순번을 바로 잡아 둔다(캡처는 프레임 끝에 덮어씀)
ScreenCapture.CaptureScreenshot(dirp + name + ".png");
return "shot " + name;
