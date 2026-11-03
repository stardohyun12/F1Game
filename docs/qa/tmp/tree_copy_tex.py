"""artlive 20261031-0142: art/export/tree-cands/T1~T8.png → F1Studio/Assets/Art/Review/trees (임포트 설정은 .meta 에 남아 있음)"""
import pathlib, shutil
R = pathlib.Path('D:/Github/F1Game')
for n in range(1, 9):
    shutil.copyfile(R / f'art/export/tree-cands/T{n}.png', R / f'F1Studio/Assets/Art/Review/trees/T{n}.png')
print('copied 8')
