from pathlib import Path
import subprocess,sys
R=Path(__file__).resolve().parent
pages=[('mockup-'+s,1280,720) for s in ['hud','orders','call','packing','receipt']]
pages += [('cargo-sheet',1600,1490),('font-proof',1280,640),('compare',1600,1880),('sprite-sheet',1200,2700)]
pages += [('guides/'+s+'-guide',560,650) for s in ['paperPanel','paperCard','slipHeader','stampRing','buttonPlate','enamelPlate','gaugeFill','stampSprite','gaugeTrackSprite','gaugeFillSprite']]
if len(sys.argv)>1:pages=[p for p in pages if p[0] in sys.argv[1:]]
for name,w,h in pages:
    subprocess.run(['C:/Program Files/Google/Chrome/Application/chrome.exe','--headless','--disable-gpu','--no-first-run','--hide-scrollbars','--allow-file-access-from-files','--user-data-dir=C:/Users/rtx40/.codex/tmp/d123-render',f'--window-size={w},{h}',f'--screenshot={R/name}.png','--virtual-time-budget=2000',(R/(name+'.html')).as_uri()],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=True)
    if not (R/(name+'.png')).exists():raise RuntimeError('missing '+name)
print(f'Rendered {len(pages)} PNGs.')
