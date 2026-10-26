import requests,re,sys,os,json
from bs4 import BeautifulSoup
H={'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130 Safari/537.36'}
def dl(page,outdir,only=None,maxmb=200):
    s=requests.Session(); s.headers.update(H)
    t=s.get(page).text
    csrf=re.search(r'name="csrf_token" content="([^"]+)"',t) or re.search(r'csrf_token[^"]*"\s*value="([^"]+)"',t)
    csrf=csrf.group(1)
    if 'data-upload_id' in t:
        t2=t; key=None
    else:
        r=s.post(page.rstrip('/')+'/download_url',data={'csrf_token':csrf}).json()
        dp=r['url']; t2=s.get(dp).text
        key=re.search(r'/download/([^?"/]+)',dp).group(1)
        m2=re.search(r'name="csrf_token" value="([^"]+)"',t2)
        if m2: csrf=m2.group(1)
    b=BeautifulSoup(t2,'html.parser'); os.makedirs(outdir,exist_ok=True); got=[]
    for up in b.select('.upload'):
        a=up.select_one('[data-upload_id]'); name=up.select_one('.name').get_text(strip=True)
        if not a: continue
        if only and not re.search(only,name,re.I): continue
        uid=a['data-upload_id']
        from urllib.parse import unquote
        prm={'source':'game_download','after_download_lightbox':'1','as_props':'1'} if key else {'source':'view_game','as_props':'1','after_download_lightbox':'true'}
        j=s.post(page.rstrip('/')+f'/file/{uid}',params=prm,data={'csrf_token':csrf}).json()
        if 'url' not in j: print('FAIL',name,j); continue
        f=s.get(j['url'],stream=True); fn=os.path.join(outdir,re.sub(r'[\/:*?"<>|]','_',name))
        cd=f.headers.get('content-disposition','')
        m=re.search(r'filename="?([^";]+)',cd)
        if m and '.' in m.group(1): fn=os.path.join(outdir,m.group(1))
        with open(fn,'wb') as o:
            for c in f.iter_content(1<<20): o.write(c)
        got.append((fn,os.path.getsize(fn)))
    return got
if __name__=='__main__':
    print(dl(sys.argv[1],sys.argv[2],sys.argv[3] if len(sys.argv)>3 else None))
