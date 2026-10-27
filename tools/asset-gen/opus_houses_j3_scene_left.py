"""j3 시트용 왼쪽(-X) 옆벽 보기 — opus_houses_j3_scene.py 를 J3_LEFT 로 실행. 출력 art/_review/2026-10-27-houses-j3/left/"""
import os
os.environ['J3_LEFT'] = '1'
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_j3_scene.py')
exec(compile(open(_P, encoding='utf-8').read(), _P, 'exec'))
