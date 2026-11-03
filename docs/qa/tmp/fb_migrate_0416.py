# 1회용: 이번 세션(04:16) 전에 팀원이 done 으로 닫은 10/29 줄 → fixed 로 되돌려 도현 확인 대기로.
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "../../../scripts"))
import fb

SHEET = "art/_review/2026-10-29-depot-split/sheet.png"
FIX = {
    "depot-20261028-211558-ot14": ("art", "묶인 메시 3개 → 낱개 10개로 분리(전체 72→79), 하나씩 옮길 수 있음 — art 보고 10-29 03:35", SHEET),
    "depot-20261028-211628-vhoh": ("art", "가운데 낀 상자: 상자끼리·선반 관통 0 으로 다시 놓음(clearance 검사) — art 보고 10-29 03:35", SHEET),
    "unity-Map_TownV6-20261029-032644-0293": ("map", "D-203: 능선에 집·우체국·보관소·정비소 4채 배치(커밋 a63bf25c), 03:56 도현 배치로 자리 갱신", None),
    "unity-Map_TownV6-20261029-032858-8877": ("map", "D-203: 능선 테라스 + 올라오는 길 3개(경사 11%, 커밋 a63bf25c·ec684c36)", None),
}
for cid, (who, text, shot) in FIX.items():
    def edit(d, who=who, text=text, shot=shot):
        if d.get("status") != "done":
            print("skip(이미", d.get("status"), ")", cid); return
        fb.push(d, who, text, shot if shot and os.path.exists(os.path.join(fb.ROOT, shot)) else None)
        d["status"] = "fixed"
    print(cid, "→", fb.mutate(cid, edit))
