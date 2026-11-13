import json, sys, glob, os, re, collections
from datetime import datetime
ROOT=os.path.expanduser('~/.claude/projects')
SK=('update-knowledge','work-on-feature')
W={'in':1,'cw':1.25,'cr':0.1,'out':5}   # relative price weights (input = 1)
def human_text(d):
    if d.get('type')!='user' or d.get('isMeta'): return None
    c=d['message'].get('content')
    if isinstance(c,str): return c
    if isinstance(c,list):
        if any(b.get('type')=='tool_result' for b in c if isinstance(b,dict)): return None
        t=' '.join(b.get('text','') for b in c if isinstance(b,dict) and b.get('type')=='text')
        return t or None
    return None
def ts(d):
    try: return datetime.fromisoformat(d['timestamp'].replace('Z','+00:00'))
    except: return None
rows=[]; sessions={}
files=[l.split(' ',2)[2].strip() for l in open(sys.argv[1])]
for rel in files:
    path=os.path.join(ROOT,rel); recs=[]
    for l in open(path,encoding='utf-8'):
        try: d=json.loads(l)
        except: continue
        if d.get('type') in('user','assistant') and not d.get('isSidechain'): recs.append(d)
    # segment by human prompts
    segs=[]; cur=None; seen=set()
    for d in recs:
        h=human_text(d)
        if h is not None and not h.startswith('<local-command') and 'Caveat:' not in h[:40]:
            cur={'prompt':h,'t0':ts(d),'t1':ts(d),'u':collections.Counter(),'tools':collections.Counter(),'writes':[],'reads':[],'n':0}
            segs.append(cur); continue
        if cur is None: continue
        if d['type']=='assistant':
            m=d['message']; cur['t1']=ts(d) or cur['t1']
            if m.get('id') not in seen:
                seen.add(m.get('id')); u=m.get('usage') or {}
                cur['u']['in']+=u.get('input_tokens',0); cur['u']['cw']+=u.get('cache_creation_input_tokens',0)
                cur['u']['cr']+=u.get('cache_read_input_tokens',0); cur['u']['out']+=u.get('output_tokens',0); cur['n']+=1
            for b in m.get('content',[]):
                if isinstance(b,dict) and b.get('type')=='tool_use':
                    nm=b['name']; cur['tools'][nm]+=1; inp=b.get('input',{})
                    fp=inp.get('file_path') or inp.get('path') or ''
                    if nm in('Write','Edit','MultiEdit'): cur['writes'].append(fp)
                    if nm=='Read': cur['reads'].append(fp)
                    if nm in('Bash','PowerShell'):
                        c=inp.get('command','')
                        if re.search(r'\b(cat|head|sed -n|Get-Content)\b',c): cur['tools']['shell-read']+=1
    wcost=lambda u: sum(u[k]*W[k] for k in W)
    stot=collections.Counter(); [stot.update(s['u']) for s in segs]
    sessions[rel]={'prompts':len(segs),'wcost':wcost(stot),'out':stot['out'],'start':segs[0]['t0'] if segs else None}
    for i,s in enumerate(segs):
        m=re.search(r'<command-name>/?([\w-]+)</command-name>',s['prompt'])
        if not m or m.group(1) not in SK: continue
        a=re.search(r'<command-args>(.*?)</command-args>',s['prompt'],re.S)
        nxt=segs[i+1]['prompt'] if i+1<len(segs) else ''
        rows.append(dict(sess=rel,skill=m.group(1),args=(a.group(1) if a else '').strip(),idx=i,of=len(segs),
            t0=s['t0'].isoformat()[:16] if s['t0'] else '',secs=int((s['t1']-s['t0']).total_seconds()) if s['t0'] and s['t1'] else 0,
            calls=s['n'],out=s['u']['out'],cw=s['u']['cw'],cr=s['u']['cr'],inp=s['u']['in'],wcost=wcost(s['u']),
            share=wcost(s['u'])/max(1,sessions[rel]['wcost']),tools=dict(s['tools']),writes=s['writes'],reads=s['reads'],
            next=re.sub(r'\s+',' ',nxt)[:300]))
json.dump({'rows':rows,'sessions':{k:{**v,'start':str(v['start'])} for k,v in sessions.items()}},open(sys.argv[2],'w'),default=str,indent=1)
print(len(rows),'invocations')
