"""Inventory the upstream Shader Forge checkout without copying its source."""

from __future__ import annotations

import re
import sys
from collections import defaultdict
from pathlib import Path


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: python3 tools/generate_component_catalog.py <ShaderForge repo> <output.md>")
    repo = Path(sys.argv[1]).resolve()
    output = Path(sys.argv[2])
    root = repo / "Shader Forge" / "Assets" / "ShaderForge"
    code = root / "Editor" / "Code"
    editor = (code / "SF_Editor.cs").read_text(encoding="utf-8-sig")
    categories = dict(re.findall(r'string\s+(cat\w+)\s*=\s*"([^"]+)"', editor))
    groups: dict[str, list[str]] = defaultdict(list)
    for line in editor.splitlines():
        if line.lstrip().startswith("//"):
            continue
        match = re.search(r"AddTemplate\(\s*typeof\(\s*(SFN_\w+)\s*\)\s*,\s*(cat\w+)\s*\+", line)
        if match:
            groups[categories[match.group(2)].rstrip("/")].append(match.group(1))
    active = {name for names in groups.values() for name in names}
    source_nodes = {path.stem for path in (code / "_Nodes").glob("SFN_*.cs")}
    lines = [
        "# Shader Forge 上游组件清单",
        "",
        "基于 FreyaHolmer/ShaderForge `master` 提交 `3bde185e67178b687b4d0b1964367355aef471f8` 的源码静态盘点。",
        "节点分类来自 `SF_Editor.InitializeNodeTemplates()` 的实际菜单注册；下列名称是类名而非功能兼容承诺。",
        "[上游节点注册表](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/SF_Editor.cs#L170-L340)",
        "[Shader Forge 官方节点文档](https://www.acegikmo.com/shaderforge/nodes/)解释了 Main 输出端口及各节点用途；此清单以当前盘点的源码注册为准，因为网页与仓库提交未必同步。",
        "",
        f"- 节点源码文件：{len(source_nodes)}；菜单直接注册：{len(active)}；未直接注册：{len(source_nodes - active)}。",
        "- `SFN_Final` 是输出节点，不在右侧添加菜单中；`SFN_Node_Constant` 是常量基类；`SFN_CommentBox` 的注册被注释；`SFN_StaticBranch` 的注册也被注释。",
        "- `SFN_SkyshopDiff` 和 `SFN_SkyshopSpec` 是按程序集动态探测的可选扩展，仓库没有对应节点源码；不能计入内置节点。",
        "- 整个 `Assets/ShaderForge` 下有 512 个非 `.meta` 文件；逐文件路径和大小见[文件清单](./Shader%20Forge%20文件清单.tsv)。",
        "",
        "## 菜单节点（完整列表）",
        "",
        "| 类别 | 数量 | 类名 |",
        "| --- | ---: | --- |",
    ]
    for category, names in groups.items():
        lines.append(f"| {category} | {len(names)} | " + ", ".join(f"`{name}`" for name in names) + " |")
    lines += [
        "",
        "## 未直接注册的节点源码",
        "",
        ", ".join(f"`{name}`" for name in sorted(source_nodes - active)) + "。",
        "",
        "## 其他 C# 组件（完整文件名）",
        "",
        "以下按源码目录列出其余 `.cs` 文件；这是组件定位索引，具体职责见[架构调研](./Shader%20Forge%20与%20Shader%20Graph%20架构调研.md)。",
        "",
    ]
    directories = [code] + sorted({path.parent for path in code.rglob("*.cs") if path.parent != code})
    for directory in directories:
        files = sorted(path.stem for path in directory.glob("*.cs") if directory.name != "_Nodes" or not path.stem.startswith("SFN_"))
        if files:
            label = "Code 根目录" if directory == code else str(directory.relative_to(code))
            lines += [f"### {label}（{len(files)}）", "", ", ".join(f"`{name}`" for name in files) + "。", ""]
    lines += [
        "## 资源、预设和示例",
        "",
    ]
    for directory in sorted(root.iterdir()):
        if not directory.is_dir():
            continue
        shader_count = len(list(directory.rglob("*.shader")))
        cs_count = len(list(directory.rglob("*.cs")))
        lines.append(f"- `{directory.name}`：{cs_count} 个 `.cs`，{shader_count} 个 `.shader`。")
    presets = sorted((root / "Editor" / "InternalResources" / "Shader Presets").glob("*.shader"))
    examples = sorted((root / "Example Assets" / "Shaders").glob("*.shader"))
    lines += [
        "",
        "### Shader 预设",
        "",
        ", ".join(f"`{path.stem}`" for path in presets) + "。",
        "",
        "### 示例 Shader",
        "",
        ", ".join(f"`{path.stem}`" for path in examples) + "。",
        "",
        "此清单只说明仓库包含哪些组件；每个节点的端口、公式、阶段和兼容性需在实现节点转换时逐一核对其 `Evaluate`、序列化及生成器调用路径。",
        "",
    ]
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines), encoding="utf-8")
    manifest = output.parent / "Shader Forge 文件清单.tsv"
    records = ["relative_path\ttype\tbytes"]
    for path in sorted(root.rglob("*")):
        if path.is_file() and not path.name.endswith(".meta"):
            records.append(f"{path.relative_to(root)}\t{path.suffix or '[none]'}\t{path.stat().st_size}")
    manifest.write_text("\n".join(records) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
