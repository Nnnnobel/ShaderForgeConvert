# Shader Forge Convert

我在做一个 Unity Editor 工具，把 Shader Forge 生成的 Shader 整理成便于继续手写维护的代码。转换从 `SF_DATA` 恢复节点关系，以原 `.shader` 保留 Pass、渲染状态和光照实现。当前版本为 **`0.1.0-preview.2`**，面向 Unity 2022.3 的 Built-in Render Pipeline；所有导出结果均标记为 `Partial`。

## 安装与使用

在 Package Manager 中选择 **Add package from git URL**，输入：

```text
https://github.com/Nnnnobel/ShaderForgeConvert.git?path=/Packages/com.shaderforgeconvert
```

也可以将 `Packages/com.shaderforgeconvert` 目录复制到 Unity 项目的 `Packages` 下作为嵌入包。选中包含 `SF_DATA` 的 `.shader` 资源，执行 **Tools > Shader Forge Convert > Convert Selected Shader**，再选择一个新的 `.shader` 路径。工具会导入并检查新 Shader；原文件和材质引用保持不变。输出的 Shader 名称附加 `/Code`，不能与已有名称冲突。

## 目前做到了什么

- 读取节点、连线和 Final 输入，追踪参与输出的节点。
- 根据节点备注重命名能在源码中找到的 `node_ID` 变量，根据属性名整理部分纹理采样变量；保留节点来源注释。
- 对结构符合规则的单一 Emission 片元逻辑提取 `ComputeEmission`。其余生成代码、Pass、宏和渲染状态沿用原实现。
- 移除 `SF_DATA` 和 Shader Forge 专用 `CustomEditor`。导出或导入失败时不保留目标文件。

这还不是通用的“节点图转手写 Shader”：多数计算链未被重组，也没有逐节点的兼容承诺。`Partial` 结果需要检查源码与画面，不能仅凭编译通过判定效果相同。

## 验证

公开仓库的 Git 包已在全新 Unity 2022.3.62f3 工程安装。上游 10 个预设和 10 个示例的原版、导出版均无 Shader 导入错误；20 组属性、默认值、Pass 名称等材质契约检查未发现差异。`VertexAnimation` 在 Metal 上两组参数的 128×128 渲染结果逐像素一致。验证条件及复现方法见[验证记录](./docs/验证记录.md)。

## 文档

- [设计与开发状态](./docs/项目文档.md)
- [可行性与转换边界](./docs/可行性调研.md)
- [Shader Forge 与 Shader Graph 架构调研](./docs/Shader%20Forge%20与%20Shader%20Graph%20架构调研.md)
- [Shader Forge 组件清单](./docs/Shader%20Forge%20组件清单.md)与[逐文件清单](./docs/Shader%20Forge%20文件清单.tsv)

## 许可与来源

本项目采用 [MIT 许可证](./LICENSE)。仓库不分发 Shader Forge 源码或资源；调研基于 [FreyaHolmer/ShaderForge](https://github.com/FreyaHolmer/ShaderForge) 及其[节点文档](https://www.acegikmo.com/shaderforge/nodes/)。问题与代码提交的要求见[贡献指南](./CONTRIBUTING.md)。
