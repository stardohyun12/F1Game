"""실패한 길 id 를 쉼표로. 사용: python -X utf8 docs/qa/tmp/onlyfail.py [파일]"""
import sys
f = sys.argv[1] if len(sys.argv) > 1 else 'docs/qa/tmp/townv6-bot-cover.txt'
rows = [l.rstrip('\n').split('\t') for l in open(f, encoding='utf-8-sig') if '\t' in l]
print(','.join(sorted({r[0] for r in rows[1:] if r[4] != 'ok'})))
