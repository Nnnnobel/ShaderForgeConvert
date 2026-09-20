# Shader Forge Convert

Open-source Unity Editor tool for turning Shader Forge output into readable code shaders. **Preview / partial conversion.**

目标：制作一个 Unity Editor 插件，保留 Shader Forge 效果和渲染行为，把节点图及生成代码重构为**按计算逻辑组织、可以继续手写维护的 Shader**。

当前阶段是**可运行探针**：已建 Unity Editor 包，能读取节点图、追踪 Final 依赖、按节点备注命名部分生成变量；对符合严格结构的单一 Emission Shader，还能抽出 `ComputeEmission` 函数。输出始终标记为 `Partial`；通用逻辑链重构尚未完成。

## 文档

- [可行性调研](./docs/可行性调研.md)：图与生成代码如何协同、自动重构的边界和风险。
- [项目需求与实施方案](./docs/项目文档.md)：逻辑化输出标准、转换流程、分阶段交付与验收。
- [Shader Forge 与 Shader Graph 架构调研](./docs/Shader%20Forge%20与%20Shader%20Graph%20架构调研.md)：上游文档、生成机制、组件职责与已完成探针。
- [Shader Forge 组件清单](./docs/Shader%20Forge%20组件清单.md)及[逐文件清单](./docs/Shader%20Forge%20文件清单.tsv)：节点与资源盘点。
- [验证记录](./docs/验证记录.md)：Git 安装、真实样本导入及尚未覆盖的验证边界。

## 一句话结论

这个目标**有条件可行**：用 `SF_DATA` 图恢复节点间的意图和依赖，用原 `.shader` 保留 Pass、光照和平台相关实现，再把可识别的效果计算组织为有意义的变量、函数和阶段。首版应聚焦受支持的节点与 Shader 类型；遇到无法证明等价的区域，要明确保留原生成代码并标记待处理，不能用删注释或格式化冒充“转换成功”。

## 当前插件试用

在 Unity 2022.3 的 Package Manager 中选择 **Add package from git URL**，输入：

```text
https://github.com/Nnnnobel/ShaderForgeConvert.git?path=/Packages/com.shaderforgeconvert
```

也可把 `Packages/com.shaderforgeconvert` 作为 Unity 本地包加入项目。在 Project 面板选中 Shader Forge `.shader`，运行 `Tools > Shader Forge Convert > Convert Selected Shader`。插件会要求选择新文件路径，导入新 Shader 并检查编译错误。源 Shader 不会被覆盖；材质引用不会自动迁移。

当前已实现受限的语义重命名、纹理采样变量命名、来源注释与单一 Emission 函数抽取。即使导入成功，也应人工检查代码和画面。用 `dotnet run --project tools/SmokeTests/SmokeTests.csproj` 可运行核心解析冒烟测试；`tools/UnityBatchProbe` 提供在临时 Unity 工程中复测样本的方法。

## 开源与来源

本项目代码采用 [MIT 许可证](./LICENSE)。Shader Forge 上游源码与资源不随本仓库分发；调研依据是 [FreyaHolmer/ShaderForge](https://github.com/FreyaHolmer/ShaderForge) 和 [Shader Forge 节点文档](https://www.acegikmo.com/shaderforge/nodes/)。欢迎通过 [贡献指南](./CONTRIBUTING.md)提交样本与问题。
