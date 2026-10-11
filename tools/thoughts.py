"""Dump every ThoughtDef as TSV. Input: a folder of def XML files flattened as <Pack>__<path with __ for />.xml (copy them from the game install, they are not in this repo).
Usage: python3 tools/thoughts.py <flattened defs dir> > thoughts.tsv"""
import sys, os, glob, xml.etree.ElementTree as ET
d = sys.argv[1]
abstracts = {}
rows = []
def text(e, tag):
    x = e.find(tag); return x.text.strip() if x is not None and x.text else ""
allnodes = []
for f in sorted(glob.glob(os.path.join(d, "*.xml"))):
    try: root = ET.parse(f).getroot()
    except Exception as ex: print("ERR", f, ex, file=sys.stderr); continue
    pack = os.path.basename(f).split("__")[0]
    for e in root:
        if e.tag != "ThoughtDef": continue
        allnodes.append((pack, os.path.basename(f), e))
        n = e.get("Name")
        if n: abstracts[n] = e
def resolve(e, tag):
    while e is not None:
        v = e.find(tag)
        if v is not None: return v
        p = e.get("ParentName"); e = abstracts.get(p) if p else None
    return None
for pack, fn, e in allnodes:
    if e.get("Abstract") == "True": continue
    dn = text(e, "defName")
    tc = resolve(e, "thoughtClass"); wc = resolve(e, "workerClass"); dur = resolve(e, "durationDays")
    rt = resolve(e, "requiredTraits")
    stages = resolve(e, "stages")
    moods = []
    if stages is not None:
        for li in stages:
            m = li.find("baseMoodEffect"); o = li.find("baseOpinionOffset")
            moods.append(f"{m.text if m is not None else ''}/{o.text if o is not None else ''}")
    print("\t".join([pack, fn.split("__")[-1], dn, (tc.text or "") if tc is not None else "", (wc.text or "") if wc is not None else "", (dur.text or "") if dur is not None else "", "T" if rt is not None else "", " ".join(moods)]))
