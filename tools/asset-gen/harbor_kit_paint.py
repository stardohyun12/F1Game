"""항구 키트 조각용 손칠 텍스처 (D-162). PIL 만 — 사진·AI 없음. paint_atlas 의 붓 얼룩 함수를 바탕칠로 재사용하고,
조각마다 눈으로 본 뒤 고친 값(진폭·자국 수)을 여기 박는다.

    python -X utf8 tools/asset-gen/harbor_kit_paint.py
"""
import os, sys
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, os.path.dirname(__file__))
import paint_atlas as pa

OUT = os.path.join(pa.ROOT, 'art', 'source', 'harbor-kit-v0', 'tex')
S = pa.S


def stone(seed):
    """석회암 테두리·모서리돌·부두: 밝은 중성 + 큰 얼룩 조금 강하게(돌이 회반죽보다 거칠다)."""
    return pa.painted_surface((232, 228, 218), seed, amp=10.0, warm=0.04, n_strokes=60)


def glass(seed):
    """창 속: 짙은 청록-갈색, 위 1/3 더 짙음(인방 그늘), 아래쪽 희미한 반사 한 줄."""
    im = pa.painted_surface(pa.WIN_DEEP, seed, amp=5.0, warm=0.0, n_strokes=12)
    d = ImageDraw.Draw(im, 'RGBA')
    for i in range(S // 3):
        d.line((0, i, S, i), fill=(10, 12, 12, int(110 * (1 - i / (S / 3)))))
    d.rectangle((0, int(S * .62), S, int(S * .70)), fill=(120, 140, 140, 40))
    return im.filter(ImageFilter.GaussianBlur(2))


def cloth(seed):
    return pa.painted_surface((238, 236, 230), seed, amp=6.0, n_strokes=10)


TEX = {
    'plaster': lambda s: pa.painted_surface((236, 232, 224), s, amp=12.0),
    'plaster_b': lambda s: pa.painted_surface((238, 235, 228), s + 17, amp=9.0),
    'roof': pa.roof, 'shutter': pa.shutter, 'door': pa.door, 'awning': pa.awning,
    'eave': pa.eave, 'pave': pa.pave, 'stone': stone, 'glass': glass, 'cloth': cloth,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for k, (name, fn) in enumerate(TEX.items()):
        im = fn(700 + k)
        im.save(os.path.join(OUT, name + '.png'))
        print(f'{name:9s} speck {pa.speck_pct(im):.3f}%  lumStd {pa.lum_std(im):.1f}')


if __name__ == '__main__':
    main()
