# Shader Forge 与 Shader Graph 架构调研

调研基线：Shader Forge `3bde185e`（2023-01-03）；Agentlien/ShaderCleanup `c6d943b4`（2022-11-28）；Unity 旧 [ShaderGraph 仓库](https://github.com/Unity-Technologies/ShaderGraph)中的 `GraphUtil.cs`。旧 ShaderGraph 仓库已迁移到 Graphics 仓库，因此这里借鉴的是**生成流程的设计原则**，不把旧源码当作当前 Unity API。[Unity 官方 Shader Graph 仓库](https://github.com/Unity-Technologies/ShaderGraph)、[Shader Graph 文件导入器说明](https://docs.unity3d.com/Manual/class-ShaderImporter.html)

## 1. Shader Forge 自带文档的范围

| 文档 | 已确认内容 | 对转换器的价值及边界 |
| --- | --- | --- |
| [GitHub README](https://github.com/FreyaHolmer/ShaderForge) | 上游以修复与兼容更新为主；附 MIT 许可文本 | 确定源码版本与许可；不是节点语义规范 |
| [仓库内 Readme.txt](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Readme.txt) | 安装、快捷操作、许可与生成 Shader 的使用说明 | 确认插件安装/使用边界；没有定义生成代码的等价转换算法 |
| [官方网站](https://www.acegikmo.com/shaderforge/) | 功能概览，列出光照、顶点动画、曲面细分、Outline、10 个示例与 Main 输出用途 | 帮助定义回归场景；页面是产品说明，不等于当前仓库支持清单 |
| [官方节点文档](https://www.acegikmo.com/shaderforge/nodes/) | Main 输入及各节点的文字说明，含 Diffuse、Emission、Opacity Clip、Vertex Offset 等 | 用于理解意图；实际端口、公式和阶段条件仍要以当前源码与测试核对 |

官方网页的节点列表与本次仓库提交不是同一份可执行注册表，所以[组件清单](./Shader%20Forge%20组件清单.md)以 `SF_Editor.InitializeNodeTemplates()` 为准。每个节点的 `Evaluate`、序列化字段和依赖分析，才是后续编写转换规则的代码证据。

## 2. 两种“Shader Graph 转代码”经验

Unity Shader Graph 自身的生成流程不是“直接把节点字符串拼起来”。旧版 `GraphUtil.GetShader` 先从输出节点收集活跃节点，再汇总输入需求，生成顶点输入和 Surface Inputs，收集属性与节点函数，最后组装 Shader 源码。[GraphUtil.cs](https://github.com/Unity-Technologies/ShaderGraph/blob/master/com.unity.shadergraph/Editor/Data/Util/GraphUtil.cs#L70-L210)、[SurfaceDescription 生成](https://github.com/Unity-Technologies/ShaderGraph/blob/master/com.unity.shadergraph/Editor/Data/Util/GraphUtil.cs#L230-L289)

Agentlien 的 ShaderCleanup 针对**已经生成**的 Shader Graph 源码，按固定顺序清理属性中转变量、节点函数、Split/Swizzle、条件编译和空块。其 README 称对测试过的 Shader 输出逻辑相同，但源码本身使用多个正则和全文件替换，适用模式与版本有限；不能把这些规则直接搬到 Shader Forge。[ShaderCleanup 源码](https://github.com/Agentlien/ShaderCleanup/blob/master/Source/Editor/GeneratedShaderCleanup.cs)、[README](https://github.com/Agentlien/ShaderCleanup)

本项目分别借鉴两点：**从最终输出反向找活跃依赖**，以及**每条清理规则独立、按顺序执行并验证**。Shader Forge 的输入格式与生成代码不同，因此节点语义和源码改写都要另做适配。

## 3. Shader Forge 的真实组件链

| 层 | 主要组件 | 作用与转换器的关系 |
| --- | --- | --- |
| 编辑器与交互 | `SF_Editor`、`SF_EditorNodeView`、`SF_EditorNodeBrowser`、`SF_SelectionManager`、预览/状态类 | 注册节点菜单、维护当前图、交互及预览；转换器不需要依赖其 UI，但节点分类来自这里的注册表 |
| 序列化/读取 | `SF_EditorNodeView.GetNodeDataSerialized`、`SF_Parser`、`SF_Node.Serialize/Deserialize` | `SF_DATA` 存图、设置、节点和连线；转换器需独立读取并标记版本/未知记录 |
| 节点模型 | `SF_Node`、127 个 `SFN_*.cs`、`SF_NodeConnector`、`SF_Link`、6 个连接组 | 节点端口、类型、连线、求值表达式和部分类型适配；每类节点的 `Evaluate` 是转换语义的依据 |
| 属性模型 | `SF_ShaderProperty` 与 10 个 `SFP_*` 文件 | 材质属性的声明、默认值、纹理等；输出必须保留属性契约 |
| Pass 与渲染设置 | `SF_PassSettings`、`SFPSC_*`、`SFPS_Category`、`SF_FeatureChecker` | 光照、混合、几何、Meta、实验设置；会影响 Pass、状态、宏和功能可用性 |
| 依赖与生成 | `SF_Dependencies`、`SF_Evaluator`、`Pass_FwdAdd`、`DependencyTree` | 按 Pass/顶点/片元阶段分析需求，生成 Properties/SubShader/Pass/HLSL 并写盘；源文件主体是保留行为的基线 |
| 内部资源与例子 | 187 个 Editor 内部 `.shader`、10 个预设、10 个示例 `.shader`，以及纹理、材质、字体、模型和场景 | 节点预览、预设与回归样本；不等于都需要随转换器分发 |

数量以[组件清单](./Shader%20Forge%20组件清单.md)及[512 项文件清单](./Shader%20Forge%20文件清单.tsv)为准。上表中“10 个预设”属于 187 个 Editor Shader 的子集，不应相加。

### 上游实际生成顺序

`SF_Evaluator.Evaluate()` 更新功能可用性和依赖，写 Properties/SubShader，按条件写 Outline、Deferred、ForwardBase、ForwardAdd、ShadowCaster、Meta 等 Pass，最后写 Fallback 和 Shader Forge 自定义 Inspector，再由 `SaveShaderAsset()` 把 `SF_DATA` 与源码拼接并设置默认纹理。[生成入口](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L3252-L3301)、[保存和默认纹理](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L3315-L3390)

`SF_Evaluator` 显式区分 `currentPass` 与 `currentProgram`，所以同一节点可能在不同 Pass、顶点或片元阶段产生不同表达式。转换器不能只建一份全局图表达式然后任意搬移。[SF_Evaluator.cs](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/_Evaluator/SF_Evaluator.cs#L40-L49)

## 4. 完整节点族与实现优先级

完整类名见[组件清单](./Shader%20Forge%20组件清单.md)。这里把所有菜单类别映射到转换策略：

| 类别 | 数量 | 输出逻辑中应如何处理 | 初始状态 |
| --- | ---: | --- | --- |
| Arithmetic | 41 | 核实端口顺序、广播/分量数、条件分支及生成表达式；可作首批纯运算候选 | 未逐节点实现 |
| Constant Vectors | 5 | 保留精度与矩阵类型；可折叠的常量要经等价验证 | 未实现 |
| Properties | 10 | 保留 ShaderLab 属性名、类型、默认值和纹理导入设置 | 未实现 |
| Vector Operations | 18 | 核实坐标系、法线处理、导数节点的片元阶段约束 | 未实现 |
| UV Operations | 4 | 核实时间分量、UV 通道、变换与采样位置 | 未实现 |
| Geometry Data | 14 | 核实顶点/片元可用性、世界/切线/视图空间 | 未实现 |
| Lighting | 6 | 依赖渲染路径、Pass、光源与阴影宏；高风险 | 未实现 |
| External Data | 5 | 保留 Unity 内建变量的含义与采样阶段 | 未实现 |
| Scene Data | 4 | 深度、屏幕纹理和 GrabPass/相机设置依赖；高风险 | 未实现 |
| Math Constants | 5 | 保留字面值和精度 | 未实现 |
| Trigonometry | 7 | 核实输入单位、输出精度及平台函数 | 未实现 |
| Code | 1 | 用户自定义代码应保留原文和依赖，不能自动内联假设 | 未实现 |
| Utility | 3 | Relay/Get/Set 影响引用与逻辑分组，不能只按语法删去 | 未实现 |

另有 `SFN_Final` 输出节点和 3 个未直接注册的节点源码；两个 Skyshop 扩展仅动态探测。**清单完成不代表 123 种节点已经支持转换。** 每个节点要补端口、序列化字段、`Evaluate` 分支、阶段约束、生成实例和回归测试后，才能进入兼容矩阵。

## 5. 已实现的探针与结果

当前 Unity 包已实现：读取 `SF_DATA` 节点/边，追踪 Final 输入可达节点；根据可达节点的用户备注，把生成代码中的 `node_ID` 变量改成语义名并加来源注释；将可映射的纹理采样变量按属性名命名；对只连接 Emission、且片元逻辑是连续声明语句的 Shader，把该逻辑抽成 `ComputeEmission`。导出时删除图元数据和 Shader Forge 专用 Inspector 引用，输出新 Shader 名称。未知节点或复杂控制流保留原代码。**这仍是 `Partial`，不代表通用 Shader 已完成逻辑化。**

已用 Unity **2022.3.62f3** 的独立临时工程做编辑器编译，并对官方 10 个预设和 10 个示例 `.shader` 导出后进行 Unity Shader 导入与错误检查：20 份原文件和 20 份导出文件均无 `ShaderUtil.ShaderHasError` 错误，材质契约探针未发现属性、默认值、Pass 名称等差异。`PresetUnlit` 和 `PresetParticleAdditive` 各抽出一个 Emission 函数；`VertexAnimation` 解析 27 个节点，实际重命名 1 个源码变量。早期探针曾把 3 个候选名称都计入重命名，本轮已改为只统计真正发生的源码替换。`VertexAnimation` 在 Metal 上两组参数的初步像素对照一致；用户材质、更多场景和平台仍未验证，也没有证明一般 Shader 的效果等价。复测步骤见[验证记录](./验证记录.md)。

## 6. 下一步实现决策

1. 将组件清单的每个节点类型建立**兼容状态**：解析、映射、逻辑重构、编译、视觉验证分别记证据。
2. 从真实 Unlit/UV/遮罩样本选一个端到端链，建立节点端口到生成表达式的映射，再抽成命名函数；只对通过完整验证的组合输出 `Full`。
3. 图注释可能为空，需结合输出通道、属性名和节点类型命名；避免按节点 ID 生成另一套无意义名称。
4. 为源代码建词法/结构模型；现有 token 级替换只够做受限语义重命名，不能承担通用 HLSL 重构。
5. 对所有 Pass 和纹理默认值做保留/比对；先报差异，再决定是否自动迁移。
