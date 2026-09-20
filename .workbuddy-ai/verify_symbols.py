"""灵力重构后的静态符号校验（无 Unity 编译器时的兜底手段）。

做两件事：
1) 花括号配平：捕捉编辑过程中可能出现的截断/漏写。
2) 符号存在性：扫描全工程的 `GameConfig.X` / `game.X` / `def.X` 等引用，
   与类型声明的成员集合比对，找出「引用了不存在的成员」。
"""
import os
import re
import sys

ROOT = r"C:\Users\DreamNight\Documents\01My\personal\TowerDefenseUnity\Assets"

DECL = re.compile(
    r"\b(?:public|private|protected|internal)\s+"
    r"(?:static\s+|readonly\s+|const\s+|sealed\s+|override\s+|virtual\s+|new\s+)*"
    r"[\w\.<>,\[\]\?]+\s+(\w+)\s*(?:[=;{(]|=>)"
)
ENUM_MEMBER = re.compile(r"^\s*(\w+)\s*(?:=\s*[^,]+)?,\s*$", re.M)

# 类型 -> 声明所在文件
SOURCES = {
    "GameConfig": ["Scripts/Core/GameConfig.cs"],
    "GameManager": ["Scripts/Core/GameManager.cs"],
    "TowerDefinition": ["Scripts/Data/Definitions.cs"],
    "EnemyDefinition": ["Scripts/Data/Definitions.cs"],
    "TdTheme": ["Scripts/UI/TdTheme.cs"],
    "BattleUiTheme": ["Scripts/UI/BattleUiTheme.cs"],
    "FloatingTextView": ["Scripts/UI/FloatingTextView.cs"],
    "Enemy": ["Scripts/Actors/Enemy.cs"],
    "Tower": ["Scripts/Actors/Tower.cs"],
    "TowerPlacer": ["Scripts/Systems/TowerPlacer.cs"],
}

# 每个类型内部「不该被当成成员」的通用词（基类/Object 成员、命名空间片段）
BUILTIN = {
    "Equals", "GetHashCode", "GetType", "ToString", "MemberwiseClone", "ReferenceEquals",
    "transform", "gameObject", "name", "tag", "GetComponent", "AddComponent", "Destroy",
    "Instantiate", "StartCoroutine", "StopCoroutine", "SetActive", "GetComponentInChildren",
    "Instance", "Value", "Length", "Count", "Item", "GetEnumerator",
}

REF_PATTERNS = [
    ("GameConfig", re.compile(r"\bGameConfig\.(\w+)")),
    ("GameManager", re.compile(r"\b(?:game|gm|_game|GameManager\.Instance)\.(\w+)")),
    ("TowerDefinition", re.compile(r"\btower\.Definition\.(\w+)")),
    ("TowerDefinition|EnemyDefinition", re.compile(r"\b(?:def|definition|_definition)\.(\w+)")),
    ("TdTheme", re.compile(r"\bTdTheme\.(\w+)")),
    ("BattleUiTheme", re.compile(r"\bBattleUiTheme\.(\w+)")),
    ("FloatingTextView", re.compile(r"\bFloatingTextView\.(\w+)")),
]


def read(rel):
    path = os.path.join(ROOT, rel.replace("/", os.sep))
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def members_of(rel_paths):
    names = set()
    for rel in rel_paths:
        text = read(rel)
        # 先抹掉泛型实参（含空格，如 Dictionary<TowerType, TowerDefinition>），
        # 否则声明正则的类型部分匹配不到，会把 Towers / Enemies 这类字段漏掉。
        # 内容里出现 ; { } ( ) " 时不算泛型 —— 否则 `a <= b` 和后面的 `> c`
        # 会被当成一对尖括号，把中间整段代码误删。
        flat = re.sub(r"<[^<>;{}()\"]*>", "", text)
        names.update(DECL.findall(flat))
        names.update(ENUM_MEMBER.findall(flat))
    return names


def brace_check():
    problems = []
    for dirpath, _, files in os.walk(ROOT):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            full = os.path.join(dirpath, fn)
            with open(full, "r", encoding="utf-8") as f:
                text = f.read()
            stripped = re.sub(r'"(\\.|[^"\\])*"', '""', text)
            stripped = re.sub(r"'(\\.|[^'\\])*'", "''", stripped)
            stripped = re.sub(r"//[^\n]*", "", stripped)
            stripped = re.sub(r"/\*.*?\*/", "", stripped, flags=re.S)
            if stripped.count("{") != stripped.count("}"):
                problems.append(
                    "%s: { = %d, } = %d" % (os.path.relpath(full, ROOT),
                                            stripped.count("{"), stripped.count("}"))
                )
    return problems


def main():
    declared = {name: members_of(paths) for name, paths in SOURCES.items()}
    unresolved = []
    checked = 0

    for dirpath, _, files in os.walk(ROOT):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            full = os.path.join(dirpath, fn)
            rel = os.path.relpath(full, ROOT)
            with open(full, "r", encoding="utf-8") as f:
                lines = f.readlines()

            in_comment = False
            for lineno, line in enumerate(lines, 1):
                s = line.strip()
                if s.startswith("/*"):
                    in_comment = True
                if in_comment:
                    if "*/" in s:
                        in_comment = False
                    continue
                if s.startswith("//"):
                    continue

                for owner, pattern in REF_PATTERNS:
                    for found in pattern.findall(line):
                        checked += 1
                        if found in BUILTIN:
                            continue
                        owners = owner.split("|")
                        if any(found in declared.get(o, set()) for o in owners):
                            continue
                        unresolved.append("%s:%d  %s.%s" % (rel, lineno, owner, found))

    print("引用检查总数: %d" % checked)
    print("花括号配平问题: %d" % len(brace_check()))
    for p in brace_check():
        print("  [BRACE] " + p)
    print("未解析引用: %d" % len(unresolved))
    for p in unresolved:
        print("  [SYMBOL] " + p)
    return 1 if (unresolved or brace_check()) else 0


if __name__ == "__main__":
    sys.exit(main())
