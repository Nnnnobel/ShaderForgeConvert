# 贡献指南

这是预览阶段的 Unity Editor 工具。欢迎提交可复现的 Shader Forge `.shader` 样本、节点映射问题和转换规则改进。

## 提交问题前

请附上 Unity 版本、渲染管线、Shader Forge 版本（如 `SF_DATA` 中可见）、输入节点类型、预期与实际代码，以及编译错误。涉及画面差异时，附上相同参数下的原/新截图和 Graphics API。公开仓库里的问题和附件也会公开，请先检查样本是否包含私人项目资源。

## 修改转换规则

1. 说明对应节点的端口、序列化字段和 Shader Forge `Evaluate` 行为。
2. 为规则添加能区分正确/错误行为的测试；不要只检查“输出包含某字符串”。
3. 保持源 Shader 原样，任何不确定的转换返回 `Partial` 或 `Unsupported`。
4. 运行 `dotnet run --project tools/SmokeTests/SmokeTests.csproj`。如改动 Unity Editor 代码，还应在 Unity 2022.3 中导入包并检查 Shader 编译及画面。

本仓库不包含 Shader Forge 上游源码或示例资产。引用上游代码时请遵守其 MIT 许可并附带版权声明。
