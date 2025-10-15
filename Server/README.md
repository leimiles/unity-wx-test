# Unity WebGL 服务器

支持可配置构建路径的Unity WebGL运行环境。

## 🚀 快速开始

### 1. 安装依赖
```bash
npm install
```

### 2. 配置构建路径
```bash
cp config.example.env .env
```

编辑 `.env` 文件：
```env
WEBGL_BUILD_PATH=../Builds
PORT=3000
```

### 3. 启动服务器
```bash
npm start
```

访问：http://localhost:3000

## 🐳 Docker 部署

```bash
# 启动Docker服务
docker-compose up -d

# 访问：http://localhost:8080
```

## 📁 支持的路径格式

- 相对路径：`../MyUnityProject/WebGLBuild`
- 绝对路径：`/path/to/your/build`
- 当前目录：`./MyBuild`

## 🔧 配置选项

| 环境变量 | 默认值 | 说明 |
|---------|--------|------|
| `WEBGL_BUILD_PATH` | `WebGLBuild` | WebGL构建文件路径 |
| `PORT` | `3000` | 服务器端口 |

## 📝 使用说明

1. 在Unity中构建WebGL到指定目录
2. 在Server目录启动服务：`npm start`
3. 访问 http://localhost:3000 查看结果

## 🔍 故障排除

**路径不存在错误：**
- 检查Unity是否成功构建WebGL
- 确认路径配置正确
- 验证构建文件完整性