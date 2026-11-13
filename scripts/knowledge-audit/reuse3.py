import json,glob,os,re,collections
exec(open('reuse2.py').read().split('ev=[]')[0])
def human(d):
    if d.get('type')!='user' or d.get('isMeta'): return None
    c=d['message'].get('content')
    if isinstance(c,str): return c
    if isinstance(c,list) and not any(isinstance(b,dict) and b.get('type')=='tool_result' for b in c):
        return ' '.join(b.get('text','') for b in c if isinstance(b,dict))
ev=[]; sess_has_wof=set()
for p in glob.glob(ROOT+'/*/*.jsonl'):
    sess=os.path.basename(p)[:8]; seg='pre'
    for l in open(p,encoding='utf-8'):
        try: d=json.loads(l)
        except: continue
        if d.get('isSidechain'): continue
        h=human(d)
        if h is not None:
            m=re.search(r'<command-name>/?([\w-]+)</command-name>',h)
            seg=m.group(1) if m and m.group(1) in('update-knowledge','work-on-feature') else ('compact' if 'continued from a previous conversation' in h else 'work')
            if seg=='work-on-feature': sess_has_wof.add(sess)
            continue
        if d.get('type')!='assistant': continue
        cwd=d.get('cwd','')
        for b in d['message'].get('content',[]):
            if not isinstance(b,dict) or b.get('type')!='tool_use': continue
            nm=b['name']; inp=b.get('input',{})
            txt=(inp.get('file_path') or inp.get('command') or inp.get('pattern') or '').replace(BS,'/')
            kind='W' if nm in('Write','Edit') else 'R'
            for m in TAIL.finditer(txt):
                ev.append((d['timestamp'],sess,kind,proj_of(m.group(1),cwd)+' :: '+m.group(2).lower(),seg))
ev.sort()
c=collections.Counter((e[2],e[4]) for e in ev); print(c)
# reads in normal work turns, in sessions WITHOUT work-on-feature: organic reuse
org=[e for e in ev if e[2]=='R' and e[4] in('work','pre','compact')]
print('organic reads in work turns:',len(org),' sessions:',len({e[1] for e in org}),' of which sessions w/o wof:',len({e[1] for e in org if e[1] not in sess_has_wof}))
wof=[e for e in ev if e[2]=='R' and e[4]=='work-on-feature']
print('reads during work-on-feature:',len(wof),'sessions',len({e[1] for e in wof}))
# sessions that read memory at start via CLAUDE.md-driven "load knowledge" (pre segment / first work turn)
