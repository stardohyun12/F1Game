"""연속 프레임 깜빡임 지표 (2111, D-149). PIL 만.

카메라가 움직이면 프레임 차이는 대부분 '움직임'이다. 움직임은 매끈해서 앞뒤 프레임 평균으로 거의 맞춰지고,
깜빡임(켜졌다 꺼졌다·계단이 기어감)은 안 맞춰진다. 그래서
  flicker = |f(t) − (f(t−1)+f(t+1))/2| 의 휘도 평균(0~255), 그리고 그 값이 16 넘는 픽셀 %
  diff    = |f(t) − f(t−1)| 평균(참고: 움직임 포함 전체 변화)
    python -X utf8 tools/qa/flicker.py docs/qa/evidence/look5c/frames_before [frames_after ...]
"""
import os, sys
from PIL import Image, ImageChops, ImageStat


def lum(p):
    return Image.open(p).convert('L')


def run(d):
    fs = sorted(f for f in os.listdir(d) if f.endswith('.png'))
    F = [lum(os.path.join(d, f)) for f in fs]
    fl, hi, df = [], [], []
    for t in range(1, len(F) - 1):
        mid = Image.blend(F[t - 1], F[t + 1], .5)
        e = ImageChops.difference(F[t], mid)
        fl.append(ImageStat.Stat(e).mean[0])
        h = e.point(lambda v: 255 if v > 16 else 0)
        hi.append(100 * ImageStat.Stat(h).mean[0] / 255)
        df.append(ImageStat.Stat(ImageChops.difference(F[t], F[t - 1])).mean[0])
    n = len(fl)
    s = sorted(fl)
    return dict(frames=len(F), flicker=sum(fl) / n, p95=s[int(.95 * n)], hi=sum(hi) / n, diff=sum(df) / n)


if __name__ == '__main__':
    print('세트 | 프레임 | 깜빡임 평균 | 깜빡임 p95 | 16 넘는 픽셀% | 프레임 차 평균')
    for d in sys.argv[1:]:
        r = run(d)
        print(f"{os.path.basename(d)} | {r['frames']} | {r['flicker']:.2f} | {r['p95']:.2f} | {r['hi']:.2f} | {r['diff']:.2f}")
