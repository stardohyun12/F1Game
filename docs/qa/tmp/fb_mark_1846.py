# artlive 1846: 처리한 코멘트 줄에만 "status","fix" 를 덧붙인다(다른 줄은 그대로, 같은 JSON 모양 유지).
import json
Z = '겹친 면 자동 검사 → 작은 덩어리(띠·스티커·테두리)를 1cm 띄움, 같은 덩어리 안 중복 면 삭제 (tools/asset-gen/fix_1846.py, docs/qa/tmp/fix_1846_<건물>_fix.json)'
FIX = {
 'depot-20261028-184108-1yzo': 'Q3_decals 5장을 뒤 면까지 1cm 로 붙임(떠 있던 거리 12.6cm·36.6cm)',
 'depot-20261028-184119-ztzt': Z + ' · 보관소 51쌍→1쌍',
 'depot-20261028-184255-kfrs': Z + ' · 택배 띠·스티커가 상자 면과 같은 높이였음',
 'postoffice-20261028-184329-i97z': '지붕·벽 텍스처만 밉맵 켬(Point 유지) — Unity 라이브러리 사본·뷰어 둘 다 · ' + Z,
 'postoffice-20261028-184350-vpwt': '창 2개 + 유리문: 벽에 구멍(불리언) + 1cm 안쪽에 옅은 하늘빛 반투명 유리판(Q4_window_glass_*, 재질 Glass)',
 'postoffice-20261028-184404-w6x8': Z + ' · 우체국 193쌍→3쌍',
 'postoffice-20261028-184446-lc0d': Z + ' · 우체국 193쌍→3쌍',
 'room-20261028-183939-tglf': '뷰어 땅을 2cm 아래로(건물 바닥 y=0 과 같은 높이였음)',
 'room-20261028-183953-oxq0': Z + ' · 방 7쌍→0',
 'room-20261028-184017-hjwi': Z + ' · 방 7쌍→0',
 'garage-20261028-183800-oih2': '타이어 p094·p095 이동만 export fbx 에 적용(tools/asset-gen/tires_1846.py). 매끈/노이즈 지적은 2/2 에서',
}
for b in ['depot', 'postoffice', 'room', 'garage']:
    p = 'tools/viewer/feedback/%s.jsonl' % b; L = open(p, encoding='utf-8').read().split('\n'); n = 0
    for k, l in enumerate(L):
        if not l.strip(): continue
        d = json.loads(l)
        if d['id'] in FIX:
            d['status'] = 'partial' if d['id'].endswith('oih2') else 'done'; d['fix'] = FIX[d['id']]
            sep = (',', ':') if '":' in l and '": ' not in l else (', ', ': ')
            L[k] = json.dumps(d, ensure_ascii=False, separators=sep); n += 1
    open(p, 'w', encoding='utf-8', newline='\n').write('\n'.join(L)); print(b, n)
