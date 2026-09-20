# Shader Forge 上游组件清单

基于 FreyaHolmer/ShaderForge `master` 提交 `3bde185e67178b687b4d0b1964367355aef471f8` 的源码静态盘点。
节点分类来自 `SF_Editor.InitializeNodeTemplates()` 的实际菜单注册；下列名称是类名而非功能兼容承诺。
[上游节点注册表](https://github.com/FreyaHolmer/ShaderForge/blob/3bde185e67178b687b4d0b1964367355aef471f8/Shader%20Forge/Assets/ShaderForge/Editor/Code/SF_Editor.cs#L170-L340)
[Shader Forge 官方节点文档](https://www.acegikmo.com/shaderforge/nodes/)解释了 Main 输出端口及各节点用途；此清单以当前盘点的源码注册为准，因为网页与仓库提交未必同步。

- 节点源码文件：127；菜单直接注册：123；未直接注册：4。
- `SFN_Final` 是输出节点，不在右侧添加菜单中；`SFN_Node_Constant` 是常量基类；`SFN_CommentBox` 的注册被注释；`SFN_StaticBranch` 的注册也被注释。
- `SFN_SkyshopDiff` 和 `SFN_SkyshopSpec` 是按程序集动态探测的可选扩展，仓库没有对应节点源码；不能计入内置节点。
- 整个 `Assets/ShaderForge` 下有 512 个非 `.meta` 文件；逐文件路径和大小见[文件清单](./Shader%20Forge%20文件清单.tsv)。

## 菜单节点（完整列表）

| 类别 | 数量 | 类名 |
| --- | ---: | --- |
| Arithmetic | 41 | `SFN_Abs`, `SFN_Add`, `SFN_Blend`, `SFN_BlendOver`, `SFN_Ceil`, `SFN_Clamp`, `SFN_Clamp01`, `SFN_ConstantClamp`, `SFN_Divide`, `SFN_Exp`, `SFN_Floor`, `SFN_Fmod`, `SFN_Frac`, `SFN_HsvToRgb`, `SFN_Hue`, `SFN_If`, `SFN_InverseLerp`, `SFN_ConstantInverseLerp`, `SFN_Lerp`, `SFN_ConstantLerp`, `SFN_Log`, `SFN_Max`, `SFN_Min`, `SFN_Multiply`, `SFN_MultiplyMatrix`, `SFN_Negate`, `SFN_Noise`, `SFN_OneMinus`, `SFN_Posterize`, `SFN_Power`, `SFN_Reciprocal`, `SFN_RemapRangeAdvanced`, `SFN_RemapRange`, `SFN_RgbToHsv`, `SFN_Round`, `SFN_Sign`, `SFN_Smoothstep`, `SFN_Sqrt`, `SFN_Step`, `SFN_Subtract`, `SFN_Trunc` |
| Constant Vectors | 5 | `SFN_Vector1`, `SFN_Vector2`, `SFN_Vector3`, `SFN_Vector4`, `SFN_Matrix4x4` |
| Properties | 10 | `SFN_Color`, `SFN_Cubemap`, `SFN_Matrix4x4Property`, `SFN_Slider`, `SFN_SwitchProperty`, `SFN_Tex2d`, `SFN_Tex2dAsset`, `SFN_ToggleProperty`, `SFN_ValueProperty`, `SFN_Vector4Property` |
| Vector Operations | 18 | `SFN_Append`, `SFN_ChannelBlend`, `SFN_ComponentMask`, `SFN_Cross`, `SFN_Desaturate`, `SFN_DDX`, `SFN_DDXY`, `SFN_DDY`, `SFN_Distance`, `SFN_Dot`, `SFN_Length`, `SFN_Normalize`, `SFN_NormalBlend`, `SFN_Reflect`, `SFN_Transform`, `SFN_Transpose`, `SFN_VectorProjection`, `SFN_VectorRejection` |
| UV Operations | 4 | `SFN_Panner`, `SFN_Parallax`, `SFN_Rotator`, `SFN_UVTile` |
| Geometry Data | 14 | `SFN_Bitangent`, `SFN_Depth`, `SFN_FaceSign`, `SFN_Fresnel`, `SFN_NormalVector`, `SFN_ObjectPosition`, `SFN_ObjectScale`, `SFN_ScreenPos`, `SFN_Tangent`, `SFN_TexCoord`, `SFN_VertexColor`, `SFN_ViewVector`, `SFN_ViewReflectionVector`, `SFN_FragmentPosition` |
| Lighting | 6 | `SFN_AmbientLight`, `SFN_HalfVector`, `SFN_LightAttenuation`, `SFN_LightColor`, `SFN_LightVector`, `SFN_LightPosition` |
| External Data | 5 | `SFN_PixelSize`, `SFN_ProjectionParameters`, `SFN_ScreenParameters`, `SFN_Time`, `SFN_ViewPosition` |
| Scene Data | 4 | `SFN_DepthBlend`, `SFN_FogColor`, `SFN_SceneColor`, `SFN_SceneDepth` |
| Math Constants | 5 | `SFN_E`, `SFN_Phi`, `SFN_Pi`, `SFN_Root2`, `SFN_Tau` |
| Trigonometry | 7 | `SFN_ArcCos`, `SFN_ArcSin`, `SFN_ArcTan`, `SFN_ArcTan2`, `SFN_Cos`, `SFN_Sin`, `SFN_Tan` |
| Code | 1 | `SFN_Code` |
| Utility | 3 | `SFN_Relay`, `SFN_Get`, `SFN_Set` |

## 未直接注册的节点源码

`SFN_CommentBox`, `SFN_Final`, `SFN_Node_Constant`, `SFN_StaticBranch`。

## 其他 C# 组件（完整文件名）

以下按源码目录列出其余 `.cs` 文件；这是组件定位索引，具体职责见[架构调研](./Shader%20Forge%20与%20Shader%20Graph%20架构调研.md)。

### Code 根目录（28）

`SFPSC_Blending`, `SFPSC_Console`, `SFPSC_Experimental`, `SFPSC_Geometry`, `SFPSC_Lighting`, `SFPSC_Meta`, `SFPSC_Properties`, `SFPS_Category`, `SF_Dependencies`, `SF_Editor`, `SF_EditorNodeBrowser`, `SF_EditorNodeData`, `SF_EditorNodeView`, `SF_ErrorEntry`, `SF_FeatureChecker`, `SF_InstructionPass`, `SF_NodeConnectionLine`, `SF_NodeConnector`, `SF_NodePreview`, `SF_NodeStatus`, `SF_NodeTreeStatus`, `SF_Parser`, `SF_PassSettings`, `SF_PreviewSettings`, `SF_PreviewWindow`, `SF_SelectionManager`, `SF_Settings`, `SF_StatusBox`。

### _ConnectionGroups（6）

`SFNCG_Append`, `SFNCG_Arithmetic`, `SFNCG_ChannelBlend`, `SFNCG_ComponentMask`, `SFNCG_MatrixMultiply`, `SF_NodeConnectionGroup`。

### _Enums（1）

`SF_VarTypeEnums`。

### _Evaluator（2）

`Pass_FwdAdd`, `SF_Evaluator`。

### _Evaluator/_NewSystem（1）

`DependencyTree`。

### _Nodes（3）

`SF_Node`, `SF_Node_Arithmetic`, `SF_Node_Resizeable`。

### _ShaderProperties（11）

`SFP_Branch`, `SFP_Color`, `SFP_Cubemap`, `SFP_Matrix4x4Property`, `SFP_Slider`, `SFP_SwitchProperty`, `SFP_Tex2d`, `SFP_ToggleProperty`, `SFP_ValueProperty`, `SFP_Vector4Property`, `SF_ShaderProperty`。

### _Utility（15）

`GUILines`, `SF_Blit`, `SF_ColorPicker`, `SF_Debug`, `SF_DraggableSeparator`, `SF_Extensions`, `SF_GUI`, `SF_Link`, `SF_MinMax`, `SF_Resources`, `SF_Styles`, `SF_Tools`, `SF_Web`, `SF_ZoomArea`, `SerializableDictionary`。

## 资源、预设和示例

- `Editor`：196 个 `.cs`，187 个 `.shader`。
- `Example Assets`：3 个 `.cs`，10 个 `.shader`。

### Shader 预设

`PresetBasic`, `PresetCustomLighting`, `PresetPBR`, `PresetParticleAdditive`, `PresetParticleAlphaBlended`, `PresetParticleMultiplicative`, `PresetPostEffect`, `PresetSky`, `PresetSprite`, `PresetUnlit`。

### 示例 Shader

`CustomLighting`, `LightWrapping`, `Parallax`, `PixelRotator`, `Refraction`, `TessellationDisplacement`, `Tiles`, `Vegetation`, `VertexAnimation`, `VertexColorRounding`。

这份清单记录源码组成，不代表节点已受转换器支持。兼容状态要结合 `Evaluate`、序列化字段、生成器调用路径和实际 Shader 验证。
