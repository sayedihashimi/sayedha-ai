import json,glob,os,re,collections
ROOT=os.path.expanduser('~/.claude/projects')
BS=chr(92)
TAIL=re.compile(r'((?:[A-Za-z]:[/]|/c/|~/)?[^\s"\'`*<>|]*?)((?:memory|features/[^/\s"\']+)/(?:decisions|research|open-questions|status|project-status)\.md)',re.I)
def proj_of(prefix,cwd):
    full=(prefix if re.match(r'^([a-z]:/|/c/)',prefix,re.I) else cwd.replace(BS,'/')+'/'+prefix).lower()
    m=re.search(r'_claude-code-projects/(.*?)/?$',full)
    p=m.group(1) if m else full
    p=re.sub(r'/features/[^/]+/?$','',p.strip('/')); p=re.sub(r'/(memory)$','',p)
    return p
ev=[]
for p in glob.glob(ROOT+'/*/*.jsonl'):
    sess=os.path.basename(p)[:8]
    for l in open(p,encoding='utf-8'):
        if '"tool_use"' not in l: continue
        try: d=json.loads(l)
        except: continue
        if d.get('type')!='assistant': continue
        cwd=d.get('cwd','')
        for b in d['message'].get('content',[]):
            if not isinstance(b,dict) or b.get('type')!='tool_use': continue
            nm=b['name']; inp=b.get('input',{})
            txt=(inp.get('file_path') or inp.get('command') or inp.get('pattern') or '')
            txt=txt.replace(BS,'/')
            kind='W' if nm in('Write','Edit') else 'R'
            if nm in('Bash','PowerShell') and re.search(r'>\s*\S*\.md|Set-Content|Out-File',txt): kind='W?'
            for m in TAIL.finditer(txt):
                key=proj_of(m.group(1),cwd)+' :: '+m.group(2).lower()
                ev.append((d['timestamp'],sess,kind,key))
ev.sort(); json.dump(ev,open('ev.json','w'))
byp=collections.defaultdict(list)
for e in ev: byp[e[3]].append(e)
rows=[]
for k,es in byp.items():
    ws=[e for e in es if e[2]=='W']
    if not ws: continue
    first=ws[0][0]
    later={e[1] for e in es if e[2]=='R' and e[0]>first and e[1]!=ws[0][1]}
    rows.append((len(later),len({e[1] for e in ws}),k))
rows.sort(reverse=True)
for r in rows: print(r)
print('files written:',len(rows),' read in a later session:',sum(1 for r in rows if r[0]))
