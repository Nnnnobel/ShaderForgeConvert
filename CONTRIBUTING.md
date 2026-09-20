# 贡献指南

本仓库处理 Shader Forge `.shader` 中的节点图与生成代码，不分发 Shader Forge 上游源码或示例资源。

## 问题报告

记录 Unity 版本、渲染管线、Shader Forge 版本、涉及的节点和输入文件、预期与实际输出，以及编译信息。画面问题需注明 Graphics API、材质参数和对照条件。GitHub Issue 及附件公开，提交前需移除不宜公开的项目资源。

## 转换规则

规则应能从节点端口、序列化字段与上游 `Evaluate` 实现追溯到生成代码。保留原 Shader 的属性、Pass 和宏；无法证明的变更保持 `Partial`。提交规则时附可区分正确与错误结果的测试，运行：

```sh
dotnet run --project tools/SmokeTests/SmokeTests.csproj
```

Editor 或 Shader 改动还需在 Unity 2022.3 中核对编译与画面。引用上游代码时保留其 MIT 版权与许可文本。
