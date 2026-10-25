p = 'F1/Assets/Editor/BuildBayLoop.cs'
s = open(p, encoding='utf-8').read()
s = s.replace('''        log.Add(LivedIn(Group("LivedIn")));   // v5 D-141 생활 층
''', '''        log.Add(LivedIn(Group("LivedIn")));   // v5 D-141 생활 층
        log.Add(Tutorial(Group("Tutorial"), mainR));   // 과제 2338 D-142 첫 배달 동선 자리(코드 임시 모양 + 아트 슬롯)
''', 1)
old = '''        foreach (var fx in new[] { -6f, 8f })
        {'''
new = '''        foreach (var fx in new[] { -6f })   // 과제 2338: 동쪽 위판장(+8) 자리는 정비소 PitCrew 가 쓴다 → Tutorial()
        {'''
assert old in s
s = s.replace(old, new, 1)
anchor = '    // ------------------------------------------------------------------ 차\n'
fn = open('docs/qa/tmp/tut_fn.cs.txt', encoding='utf-8').read()
assert anchor in s
s = s.replace(anchor, fn + anchor, 1)
open(p, 'w', encoding='utf-8').write(s)
print('ok')
