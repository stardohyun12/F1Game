# artlive 1950: 떠 있는 택배 전/후. mshy = [뷰어 | Unity 전(pre1950) | Unity 후(after1950)], 선반 근접 = [전 | 후]
import sys
sys.argv = sys.argv[:1] + ['none']  # fb_pair_1914 를 import 만 하려고: 태그만 주고 id 는 안 줌 → 그리기 함수만 정의된다
sys.path.insert(0, 'docs/qa/tmp'); from fb_pair_1914 import sheet, D  # noqa: E402
i = 'depot-20261028-193403-mshy'
sheet(['tools/viewer/feedback/shots/%s.png' % i, D + i + '_pre1950.png', D + i + '_after1950.png'], ['viewer shot', 'Unity before', 'Unity after (1950 snap)'], D + i + '_pair.png')
sheet([D + 'depot_shelf_close_pre1950.png', D + 'depot_shelf_close_after1950.png'], ['shelf close before', 'shelf close after (1950)'], D + 'depot_shelf_close_pair_1950.png')
