import json,os,re
ROOT=os.path.expanduser('~/.claude/projects')
rows=json.load(open('out.json'))['rows']
def human(d):
    if d.get('type')!='user' or d.get('isMeta'): return None
    c=d['message'].get('content')
    if isinstance(c,str): return c
    if isinstance(c,list) and not any(isinstance(b,dict) and b.get('type')=='tool_result' for b in c):
        return ' '.join(b.get('text','') for b in c if isinstance(b,dict))
for r in rows:
    if r['skill']!='update-knowledge': continue
    recs=[json.loads(l) for l in open(os.path.join(ROOT,r['sess']),encoding='utf-8')]
    i=-1; seg=None; texts=[]; bash=[]
    for d in recs:
        if d.get('isSidechain'): continue
        h=human(d)
        if h is not None and not h.startswith('<local-command') and 'Caveat:' not in h[:40]:
            i+=1; continue
        if i==r['idx'] and d.get('type')=='assistant':
            for b in d['message'].get('content',[]):
                if b.get('type')=='text': texts.append(b['text'])
                if b.get('type')=='tool_use' and b['name'] in('Bash','PowerShell'): bash.append(b['input'].get('command','')[:150].replace('\n',' | '))
    print(f"\n##### {r['t0']} {r['sess'][-60:-6]} writes={len(r['writes'])} out={r['out']}")
    for c in bash: print('   $',c)
    print('   >>',re.sub(r'\s+',' ',texts[-1] if texts else '')[:700])
