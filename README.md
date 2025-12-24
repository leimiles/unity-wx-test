# unity-wx-test

## 自动代码审查（Automated Code Review）

本项目配置了每日自动代码审查功能，每天凌晨 2 点（UTC）自动运行并通过邮件发送审查结果。

### 快速开始

1. **配置邮件密钥**：在 GitHub 仓库的 Settings → Secrets and variables → Actions 中添加：
   - `MAIL_USERNAME`：发件邮箱地址
   - `MAIL_PASSWORD`：邮箱应用专用密码

2. **详细配置说明**：请查看 [.github/workflows/README_CN.md](.github/workflows/README_CN.md)

3. **手动测试**：前往 Actions 选项卡，选择 "Daily Code Review" 工作流，点击 "Run workflow" 进行测试

### 功能特点

- ✅ 自动分析 C# 代码（Unity 脚本）
- ✅ 自动分析 JavaScript/Node.js 代码（Server 目录）
- ✅ 安全扫描（硬编码凭据、TODO 项目）
- ✅ 邮件报告（发送到 leimiles_18@hotmail.com）
- ✅ 审查报告自动存档

详细文档：[English](.github/workflows/README.md) | [中文](.github/workflows/README_CN.md)