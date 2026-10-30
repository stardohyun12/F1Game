"""Run the unmodified reference's trim_fan on two synthetic fixtures (not its full map)."""
import ast, json, sys, types, pathlib
import numpy as np
sys.path.insert(0, 'D:/Temp/pitcrew-berat-reference/tools')
# Windows has no os.sysconf; bypass only the package's machine-sizing initializer.
# junction.py, geometry.py and config.py are loaded unchanged.
package=types.ModuleType('roads'); package.__path__=['D:/Temp/pitcrew-berat-reference/tools/roads']
sys.modules['roads']=package
from roads import config
import shapely
from shapely.geometry import LineString, Polygon
from dataclasses import dataclass, field
# Execute the original function AST without the unrelated lane/numba pipeline imports.
source=ast.parse(pathlib.Path('D:/Temp/pitcrew-berat-reference/tools/roads/junction.py').read_text())
subset=ast.Module(body=[n for n in source.body if isinstance(n,(ast.FunctionDef,ast.ClassDef)) and n.name in {'Arm','_points_of','facing_corner','trim_fan'}],type_ignores=[])
exec(compile(subset,'original junction.py (isolated functions)','exec'))

def run(angles):
    fan=[]
    for i,angle in enumerate(angles):
        a=np.radians(angle); direction=np.array([np.cos(a),np.sin(a)])
        u=np.arange(81,dtype=float); xy=u[:,None]*direction
        normal=np.array([-direction[1],direction[0]])
        fan.append(Arm(i,0,0,u,xy,np.repeat(direction[None,:],len(u),axis=0),xy+3*normal,xy-3*normal,4))
    corners=trim_fan(fan)
    return {'angles':angles,'trim':[round(a.trim,3) for a in fan], 'crossings':sum(c is not None for c in corners)}
print(json.dumps([run([0,90,180]),run([0,20,180])],indent=2))
