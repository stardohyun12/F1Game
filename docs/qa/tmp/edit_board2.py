p = 'F1/Assets/Scripts/Interactables/Pack2D/PackBoard.cs'
lines = open(p, encoding='utf8').read().split('\n')
start = None
for i, l in enumerate(lines):
    if '흔들기: 닫은 상자를 집은 채 마우스가 빠르게 오가면 소리' in l:
        start = i
        break
assert start is not None
end = None
for i in range(start, len(lines)):
    if 'pendingTake = true; ExitAsked = true;' in lines[i]:
        end = i + 1
        break
assert lines[end].strip() == '}', lines[end]
new = open('docs/qa/tmp/board_block.cs', encoding='utf8').read().rstrip('\n').split('\n')
lines[start:end + 1] = new
open(p, 'w', encoding='utf8').write('\n'.join(lines))
print(start, end)
