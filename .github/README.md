# GitHub CI/CD 说明

本项目已配置 GitHub Actions CI/CD 工作流,实现自动化编译、测试、打包和发布功能。

## 工作流说明

### 1. CI 工作流 (`.github/workflows/ci.yml`)

**触发条件:**
- 推送到 `master` 或 `main` 分支
- 创建 Pull Request 到 `master` 或 `main` 分支

**执行内容:**
- 在 .NET 8.0 和 9.0 环境下编译项目
- 运行 Test 目录中的所有测试
- 上传测试结果作为构建产物

### 2. Release 工作流 (`.github/workflows/release.yml`)

**触发条件:**
- 推送版本标签 (格式: `v*.*.*`, 例如 `v1.0.0`)

**执行步骤:**

#### 第一阶段: 构建和测试
- 在 .NET 8.0 和 9.0 环境下编译项目
- 运行所有测试确保代码质量

#### 第二阶段: 打包和发布到 NuGet
- 从标签获取版本号
- 打包 Core 目录下的项目:
  - `Mud.ServiceCodeGenerator`
  - `Mud.EntityCodeGenerator`
- 发布到 NuGet 官方仓库
- 上传 NuGet 包作为构建产物

#### 第三阶段: 创建 GitHub Release
- 自动生成变更日志 (基于 Git 提交记录)
- 创建 GitHub Release
- 附加 NuGet 包文件
- 根据版本号判断是否为预发布版本 (包含 `-` 的版本号为预发布版本)

## 使用方法

### 配置 NuGet API Key

在发布前,需要配置 NuGet API Key:

1. 在 [NuGet.org](https://www.nuget.org/) 注册账号并获取 API Key
2. 在 GitHub 仓库中添加 Secret:
   - 进入仓库 Settings → Secrets and variables → Actions
   - 点击 "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: 你的 NuGet API Key
   - 点击 "Add secret"

### 发布新版本

1. **确保代码已提交并通过 CI 测试**

2. **创建版本标签:**
   ```bash
   # 创建标签 (例如 v1.0.0)
   git tag v1.0.0
   
   # 推送标签到 GitHub
   git push origin v1.0.0
   ```

3. **等待 GitHub Actions 完成**
   - 自动编译和测试
   - 自动打包并发布到 NuGet
   - 自动创建 GitHub Release

### 版本号规范

遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范:

- **主版本号 (MAJOR)**: 不兼容的 API 修改
- **次版本号 (MINOR)**: 向下兼容的功能新增
- **修订号 (PATCH)**: 向下兼容的问题修正
- **预发布版本**: 包含 `-` 后缀,如 `v1.0.0-beta`, `v2.0.0-preview.1`

示例:
- `v1.0.0` - 正式版本
- `v1.1.0` - 新增功能
- `v1.0.1` - Bug 修复
- `v2.0.0-beta` - 预发布版本

## 工作流自定义

### 修改目标框架

编辑 `.github/workflows/ci.yml` 和 `.github/workflows/release.yml` 中的 `dotnet-version` 矩阵:

```yaml
strategy:
  matrix:
    dotnet-version: ['8.0.x', '9.0.x']  # 可添加或删除版本
```

### 修改打包项目

编辑 `.github/workflows/release.yml` 中的打包步骤,添加或删除项目:

```yaml
- name: Pack YourProject
  run: dotnet pack Core/YourProject/YourProject.csproj --configuration Release --no-build --output ./nupkgs -p:PackageVersion=${{ steps.get_version.outputs.VERSION }}
```

## 故障排查

### CI 失败

1. 检查测试是否全部通过: `dotnet test`
2. 检查代码是否编译通过: `dotnet build`
3. 查看 GitHub Actions 日志获取详细错误信息

### 发布失败

1. 检查 NuGet API Key 是否正确配置
2. 检查版本号是否已存在: 同一版本号不能重复发布
3. 检查包元数据是否符合 NuGet 规范
4. 查看 GitHub Actions 日志获取详细错误信息

### 变更日志不完整

- 确保提交信息清晰明了
- 使用规范的提交信息格式,如:
  - `feat: 新增功能描述`
  - `fix: 修复问题描述`
  - `docs: 文档更新描述`

## 相关文档

- [GitHub Actions 文档](https://docs.github.com/zh/actions)
- [NuGet 发布指南](https://docs.microsoft.com/zh-cn/nuget/nuget-org/publish-a-package)
- [语义化版本规范](https://semver.org/lang/zh-CN/)
