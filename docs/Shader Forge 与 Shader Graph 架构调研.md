# Shader Forge 与 Shader Graph 架构调研

我以 [FreyaHolmer/ShaderForge](https://github.com/FreyaHolmer/ShaderForge) 提交 `3bde185e67178b687b4d0b1964367355aef471f8` 为 Shader Forge 基线，参考 [Unity Shader Graph 旧仓库](https://github.com/Unity-Technologies/ShaderGraph)与 [Agentlien/ShaderCleanup](https://github.com/Agentlien/ShaderCleanup) 的生成和清理流程。旧 Shader Graph 仓库已迁移，此处只取架构思路，不把旧接口作为当前 Unity API。

## 资料范围

| 资料 | 用途 |
| --- | --- |
| [Shader Forge README](https://github.com/FreyaHolmer/ShaderForge) 与 [仓库 Readme.txt](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Readme.txt) | 安装、操作、维护状态与许可；不定义代码等价转换规则 |
| [官网](https://www.acegikmo.com/shaderforge/) 与 [节点文档](https://www.acegikmo.com/shaderforge/nodes/) | Main 输出及节点用途；具体端口和公式仍以本次源码为准 |
| [SF_Editor 节点菜单注册](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/SF_Editor.cs#L170-L340) | 确定这一提交实际提供的菜单节点 |
| [SF_Parser](https://github.com/FreyaHolmer/ShaderForge/blob/master/Shader%20Forge/Assets/ShaderForge/Editor/Code/SF_Parser.cs#L47-L115) 与 [SF_Evaluator](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L3252-L3301) | 图的读取、Pass 与 ShaderLab 的生成顺序 |

官网列表和仓库节点菜单未必同步，因此[组件清单](./Shader%20Forge%20组件清单.md)以这一提交的实际注册和文件为准。

## 可以借鉴的生成流程

旧版 Shader Graph 的 [GraphUtil.GetShader](https://github.com/Unity-Technologies/ShaderGraph/blob/master/com.unity.shadergraph/Editor/Data/Util/GraphUtil.cs#L70-L210) 从输出节点收集活跃节点，再汇总输入需求、属性和节点函数，生成 [Surface Description](https://github.com/Unity-Technologies/ShaderGraph/blob/master/com.unity.shadergraph/Editor/Data/Util/GraphUtil.cs#L230-L289) 与 Shader 源码。这对应本项目从 Final 端口反向追踪依赖的第一步。

[ShaderCleanup](https://github.com/Agentlien/ShaderCleanup/blob/master/Source/Editor/GeneratedShaderCleanup.cs) 针对 Shader Graph 已生成代码顺序清理中转变量、节点函数、Split/Swizzle、条件编译和空块。它的 [README](https://github.com/Agentlien/ShaderCleanup) 报告了测试范围内的行为一致性；其正则和全文替换依赖 Shader Graph 的输出格式，不能直接套用在 Shader Forge 上。本项目把规则拆开，分别检查源码适用条件和验证结果。

## Shader Forge 组件链

| 层 | 主要组件 | 对转换器的意义 |
| --- | --- | --- |
| 编辑器 | `SF_Editor`、`SF_EditorNodeView`、`SF_EditorNodeBrowser`、`SF_SelectionManager` | 节点菜单、图的交互与状态；转换器不依赖这套 UI |
| 序列化 | `SF_EditorNodeView.GetNodeDataSerialized`、`SF_Parser`、`SF_Node.Serialize/Deserialize` | `SF_DATA` 保存设置、节点与连线；历史格式需辨认 |
| 节点 | `SF_Node`、127 个 `SFN_*.cs`、`SF_NodeConnector`、`SF_Link`、6 个连接组 | `Evaluate` 与连接器决定端口、类型和表达式 |
| 属性 | `SF_ShaderProperty`、10 个 `SFP_*` 文件 | 生成 ShaderLab 属性、默认值与纹理设置 |
| Pass 设置 | `SF_PassSettings`、`SFPSC_*`、`SFPS_Category`、`SF_FeatureChecker` | 混合、光照、几何、Meta 及功能开关 |
| 依赖与生成 | `SF_Dependencies`、`SF_Evaluator`、`Pass_FwdAdd`、`DependencyTree` | 根据 Pass 与顶点/片元需求写出最终 Shader |
| 资源 | 187 个 Editor 内部 `.shader`、10 个预设、10 个示例及其他资源 | 节点预览和回归样本；10 个预设已计入 Editor Shader 数量 |

文件定位和全部类名见[组件清单](./Shader%20Forge%20组件清单.md)及[512 项逐文件清单](./Shader%20Forge%20文件清单.tsv)。

`SF_Evaluator.Evaluate()` 按配置写出 Properties、SubShader、Outline、Deferred、ForwardBase、ForwardAdd、ShadowCaster、Meta 等 Pass，再写 Fallback 与自定义 Inspector；`SaveShaderAsset()` 将图数据与源码拼接，并设置默认纹理。[生成入口](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L3252-L3301)、[保存流程](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L3315-L3390)

生成器区分 `currentPass` 和 `currentProgram`；同一个图节点可能在不同 Pass、顶点或片元阶段生成不同代码。转换规则不能把这些表达式当作同一份可随意移动的计算。[生成器状态](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L40-L49)

## 节点族与现状

| 菜单类别 | 数量 | 主要核对项 |
| --- | ---: | --- |
| Arithmetic | 41 | 运算顺序、类型广播、分支和表达式 |
| Constant Vectors | 5 | 精度、分量和矩阵类型 |
| Properties | 10 | 属性名、类型、默认值和纹理 |
| Vector Operations | 18 | 坐标系、法线与导数阶段 |
| UV Operations | 4 | UV 通道、时间、变换与采样 |
| Geometry Data | 14 | 顶点/片元输入及空间变换 |
| Lighting | 6 | Pass、光源、阴影与渲染路径 |
| External Data | 5 | Unity 内建变量及阶段 |
| Scene Data | 4 | 深度、屏幕纹理与 GrabPass |
| Math Constants | 5 | 常量与精度 |
| Trigonometry | 7 | 单位、精度与平台函数 |
| Code | 1 | 自定义代码与外部依赖 |
| Utility | 3 | Relay/Get/Set 的引用关系 |

还有 `SFN_Final` 与 3 个未直接注册的节点源码；Skyshop 的两个扩展靠程序集动态探测。这些分类是盘点结果，不是转换器的兼容矩阵。节点要进入可转换范围，仍需核对端口、序列化字段、`Evaluate` 分支、阶段条件与真实生成代码。

## 当前映射能力

当前包读取图节点及边，追踪 Final 输入；根据图备注命名实际出现的 `node_ID` 变量，根据纹理属性名整理部分采样变量，并为严格的单一 Emission 声明序列提取 `ComputeEmission`。未知表达式保留原代码，导出状态为 `Partial`。词法处理只覆盖受限重命名，尚不能承担通用 HLSL 函数重组。

Unity 2022.3.62f3 中的 20 个上游预设/示例已通过原版与导出版 Shader 导入检查；公开材质契约检查差异为 0。`VertexAnimation` 的两组 Metal 渲染对照逐像素一致，适用条件和剩余空白见[验证记录](./验证记录.md)。
