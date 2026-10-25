// 게임 뷰 캡처가 2x2 블록(=Point 확대)인지 센다: 가로로 이웃한 두 칸이 '정확히 같은 색' 인 비율을 짝수 시작/홀수 시작으로 나눠 비교.
var bytes=System.IO.File.ReadAllBytes("D:/Github/F1Game/docs/qa/evidence/lowres-current-gameview.png");
var t=new Texture2D(2,2); t.LoadImage(bytes); int W=t.width,H=t.height; var p=t.GetPixels32();
long evenSame=0, oddSame=0, n=0;
// UI 없는 왼쪽 길·건물 구역만 (GetPixels 는 아래가 0): x 100~560, y 400~1000 중 UI 박스(속도계 y 278~392) 위
for(int y=400;y<1000;y++) for(int x=100;x+2<560;x+=2){ var a=p[y*W+x]; var b=p[y*W+x+1]; var c=p[y*W+x+2];
 bool ab=a.r==b.r&&a.g==b.g&&a.b==b.b; bool bc=b.r==c.r&&b.g==c.g&&b.b==c.b; if(ab) evenSame++; if(bc) oddSame++; n++; }
// 확대 크롭(가운데 240x135 를 8배)
int OW=1920,OH=1080; var z=new Color32[OW*OH]; int cx=80, cy=560; // 왼쪽 전봇대·건물 모서리
for(int y=0;y<OH;y++) for(int x=0;x<OW;x++) z[y*OW+x]=p[(cy+y/8)*W+cx+x/8];
var zt=new Texture2D(OW,OH,TextureFormat.RGB24,false); zt.SetPixels32(z); zt.Apply(); System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/lowres-current-gameview-zoom8.png",zt.EncodeToPNG());
return "size="+W+"x"+H+" evenPairSame="+(100.0*evenSame/n).ToString("F1")+"% oddPairSame="+(100.0*oddSame/n).ToString("F1")+"%";
