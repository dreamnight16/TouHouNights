import re, os, glob

root = "Assets/Scripts"
files = glob.glob(root + "/**/*.cs", recursive=True)

def members(path):
    txt = open(path, encoding='utf-8').read()
    out = set()
    for m in re.finditer(r'public\s+(?:static\s+|const\s+)?(?:readonly\s+)?[\w<>\[\]]+\s+(\w+)\s*[=;{]', txt):
        out.add(m.group(1))
    for m in re.finditer(r'public\s+static\s+class\s+(\w+)', txt):
        out.add(m.group(1))
    return out

theme_path = "Assets/Scripts/UI/TdTheme.cs"
theme_members = members(theme_path)
txt = open(theme_path, encoding='utf-8').read()
nested = {}
for cls in ["Sem", "Space", "Motion", "Radius"]:
    m = re.search(r'public static class ' + cls + r'\s*\{(.*?)\n        \}', txt, re.S)
    if m:
        nested[cls] = set(re.findall(r'public\s+(?:static\s+)?(?:readonly\s+)?[\w<>\[\]]+\s+(\w+)\s*[=;]', m.group(1)))

missing = {}
for f in files:
    if os.path.abspath(f) == os.path.abspath(theme_path):
        continue
    s = open(f, encoding='utf-8').read()
    for m in re.finditer(r'TdTheme\.(\w+)(?:\.(\w+))?', s):
        top, sub = m.group(1), m.group(2)
        if top in nested:
            if sub and sub not in nested[top]:
                missing.setdefault("TdTheme.%s.%s" % (top, sub), []).append(os.path.basename(f))
        elif sub is None:
            if top not in theme_members:
                missing.setdefault("TdTheme.%s" % top, []).append(os.path.basename(f))
        else:
            if sub not in nested.get(top, set()):
                missing.setdefault("TdTheme.%s.%s" % (top, sub), []).append(os.path.basename(f))

print("=== MISSING TdTheme tokens ===")
for k, v in sorted(missing.items()):
    print("  ", k, "->", sorted(set(v)))
if not missing:
    print("   (none)")

print("\n=== brace balance ===")
bad = []
for f in files:
    s = open(f, encoding='utf-8').read()
    if s.count('{') != s.count('}'):
        bad.append((f, s.count('{'), s.count('}')))
for b in bad:
    print("   UNBALANCED", b)
if not bad:
    print("   all balanced, %d files" % len(files))

print("\n=== TdLayout / TdKit / UiFont / UiFactory API check ===")
def pub_methods(path):
    s = open(path, encoding='utf-8').read()
    return set(re.findall(r'public\s+(?:static\s+|override\s+)?[\w<>\[\],\s\?]+\s+(\w+)\s*\(', s))

apis = {}
for name, path in [("TdLayout", "Assets/Scripts/UI/TdLayout.cs"),
                   ("TdKit", "Assets/Scripts/UI/TdKit.cs"),
                   ("UiFont", "Assets/Scripts/UI/UiFont.cs"),
                   ("UiFactory", "Assets/Scripts/UI/UiFactory.cs"),
                   ("UiIcon", "Assets/Scripts/UI/UiIcon.cs")]:
    apis[name] = pub_methods(path)

for name, meths in apis.items():
    miss = {}
    for f in files:
        if any(name in os.path.basename(f) for k in [0] if False):
            continue
        s = open(f, encoding='utf-8').read()
        for m in re.finditer(re.escape(name) + r'\.(\w+)\s*\(', s):
            fn = m.group(1)
            if fn not in meths:
                miss.setdefault(fn, []).append(os.path.basename(f))
    print(" ", name, "missing:", {k: sorted(set(v)) for k, v in miss.items()} or "none")
