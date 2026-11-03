import io, json, urllib.request as U, urllib.parse as P
from PIL import Image
H = {"User-Agent": "F1Game-artlive/1.0 (stardohyun11@gmail.com)"}
t = "File:Pale pink camellia bloom among dark glossy leaves in a garden in Portland, Oregon.jpg"
r = json.load(U.urlopen(U.Request("https://commons.wikimedia.org/w/api.php?" + P.urlencode({"action":"query","prop":"imageinfo","iiprop":"url","iiurlwidth":900,"format":"json","titles":t}), headers=H)))
ii = list(r["query"]["pages"].values())[0]["imageinfo"][0]
a = Image.open(io.BytesIO(U.urlopen(U.Request(ii["thumburl"], headers=H)).read())).convert("RGB")
b = Image.open("D:/Github/F1Game/art/source/photo-picks/T/T_cypress_B_leaf.jpg"); b.thumbnail((900, 900))
s = Image.new("RGB", (1800, 900)); s.paste(a, (0, 0)); s.paste(b, (900, 0)); s.save("D:/Github/F1Game/art/source/photo-picks/T/_cand/peek.png")
