"""Convex polygon intersection in XZ for measuring overlap area, not just SAT."""
def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def area(p):return abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2 if p else 0
def intersection(subject,clip):
    sign=1 if sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(clip,clip[1:]+clip[:1]))>=0 else -1
    out=list(subject)
    for a,b in zip(clip,clip[1:]+clip[:1]):
        inp=out;out=[]
        if not inp:break
        prev=inp[-1];dp=sign*cross(a,b,prev)
        for curr in inp:
            dc=sign*cross(a,b,curr)
            if (dc>=0)!=(dp>=0):
                t=dp/(dp-dc);out.append((prev[0]+t*(curr[0]-prev[0]),prev[1]+t*(curr[1]-prev[1])))
            if dc>=0:out.append(curr)
            prev=curr;dp=dc
    return out
