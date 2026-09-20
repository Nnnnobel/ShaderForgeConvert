# Shader Forge Convert

Unity 2022.3 Built-in Render Pipeline 的 Editor 包。选中 Shader Forge `.shader` 后，执行 **Tools > Shader Forge Convert > Convert Selected Shader**，选择新文件路径。

工具读取 `SF_DATA` 图和现有 Shader 代码，保留原 Pass 与渲染状态，对能确认映射的变量命名，并在限定条件下提取 Emission 函数。原 Shader 和材质引用不变。所有导出结果均为 `Partial`，需要检查源码与实际画面。

完整说明、验证记录和源码见[项目仓库](https://github.com/Nnnnobel/ShaderForgeConvert)。许可证：MIT。包内不包含 Shader Forge。
