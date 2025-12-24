# 每日代码审查工作流配置指南

## 概述
此工作流会在每天凌晨 2 点（UTC时间）自动审查您的代码，并将审查报告通过邮件发送到 `leimiles_18@hotmail.com`。

## 必需配置步骤

### 1. 设置 GitHub 密钥（Secrets）
要发送电子邮件，您需要在 GitHub 仓库中配置以下密钥：

1. 在 GitHub 上打开您的仓库
2. 点击 **Settings**（设置）→ **Secrets and variables** → **Actions**
3. 点击 **New repository secret**（新建仓库密钥），添加以下内容：

   - **MAIL_USERNAME**: 您的邮箱地址（例如：您的 Gmail 地址）
   - **MAIL_PASSWORD**: 您的邮箱应用专用密码（请参见下面的说明）

### 2. 获取 Gmail 应用专用密码

如果使用 Gmail 发送邮件：

1. 访问 Google 账户设置：https://myaccount.google.com/
2. 导航到 **安全性** → **两步验证**（如果未启用，请先启用）
3. 向下滚动到 **应用专用密码**，点击它
4. 为"邮件"和"其他（自定义名称）"生成新的应用专用密码
5. 复制 16 位字符密码，并将其用作 `MAIL_PASSWORD` 密钥

### 3. 使用其他邮件服务商

如果您想使用不同的邮件提供商（非 Gmail）：

编辑工作流文件 `.github/workflows/daily-code-review.yml`，更新以下内容：
```yaml
server_address: smtp.your-provider.com  # 例如：smtp.office365.com 用于 Outlook
server_port: 465  # 或 587 用于 TLS
```

常见 SMTP 设置：
- **Gmail**: smtp.gmail.com:465 (SSL) 或 :587 (TLS)
- **Outlook/Hotmail**: smtp.office365.com:587 (TLS)
- **QQ邮箱**: smtp.qq.com:465 (SSL) 或 :587 (TLS)
- **163邮箱**: smtp.163.com:465 (SSL) 或 :587 (TLS)
- **126邮箱**: smtp.126.com:465 (SSL) 或 :587 (TLS)

### 4. 调整时区

工作流当前在 UTC 时间凌晨 2 点运行。要根据您的时区进行调整：

编辑 `.github/workflows/daily-code-review.yml` 中的 cron 计划：
```yaml
schedule:
  - cron: '0 2 * * *'  # 更改小时数（0-23）
```

**时区转换示例：**
- 凌晨 2 点 UTC = 上午 10 点 CST（中国标准时间，UTC+8）
- 对于北京时间凌晨 2 点，使用：`0 18 * * *`（UTC 时间下午 6 点 = UTC+8 时区的第二天凌晨 2 点）

**注意：** GitHub Actions 使用 UTC 时间，中国时间（北京时间）是 UTC+8。
- 如果想在北京时间凌晨 2 点运行，应该设置为前一天的 `18` 点（UTC 时间）
- 公式：UTC 时间 = 北京时间 - 8 小时

## 功能特点

该工作流执行以下操作：

1. **C# 代码分析**
   - 统计文件和代码行数
   - 识别大型文件（潜在的复杂性问题）
   - 显示最近 24 小时的更改

2. **JavaScript/Node.js 分析**
   - 审查 Server 目录代码
   - 检查包依赖项
   - 如果配置了 ESLint，则运行它

3. **安全检查**
   - 扫描硬编码的凭据
   - 列出 TODO/FIXME 项目

4. **邮件报告**
   - 将综合报告发送到配置的电子邮件
   - 包含改进建议

## 手动测试

您可以手动触发工作流进行测试：

1. 转到 GitHub 仓库的 **Actions**（操作）选项卡
2. 点击 **Daily Code Review**（每日代码审查）工作流
3. 点击 **Run workflow**（运行工作流）按钮
4. 选择分支并点击 **Run workflow**（运行工作流）

## 故障排除

### 没有收到邮件？
- 检查 GitHub Actions 日志以查找错误
- 验证密钥是否正确配置
- 检查垃圾邮件/垃圾文件夹
- 确保邮件提供商允许 SMTP 访问
- 对于 Gmail，确保使用应用专用密码

### 工作流未运行？
- 必须为您的仓库启用 GitHub Actions
- 计划的工作流可能会延迟最多 15 分钟
- 检查 Actions 选项卡以查找任何工作流失败

### QQ 邮箱配置说明
如果使用 QQ 邮箱发送邮件：
1. 登录 QQ 邮箱
2. 设置 → 账户 → 开启 SMTP 服务
3. 生成授权码（不是 QQ 密码）
4. 使用授权码作为 `MAIL_PASSWORD`

## 报告存档

即使邮件发送失败，审查报告也会始终保存为工作流运行中的制品（artifact）：
1. 转到 **Actions**（操作）选项卡
2. 点击工作流运行
3. 下载 **code-review-report** 制品

## 自定义

您可以通过编辑 `.github/workflows/daily-code-review.yml` 自定义审查：
- 添加更多代码分析工具
- 更改审查标准
- 修改报告格式
- 添加其他通知渠道（Slack、Discord 等）

## 快速配置检查清单

- [ ] 在 GitHub 仓库中添加 `MAIL_USERNAME` 密钥
- [ ] 在 GitHub 仓库中添加 `MAIL_PASSWORD` 密钥
- [ ] 如果需要，调整 cron 时间以匹配您的时区
- [ ] 手动运行一次工作流进行测试
- [ ] 检查邮箱（包括垃圾邮件文件夹）是否收到测试邮件
- [ ] 如有必要，调整 SMTP 服务器设置
