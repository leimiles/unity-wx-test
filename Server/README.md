# Unity WebGL 服务器

一个支持可配置构建路径的Unity WebGL运行环境服务器。

## 🚀 快速开始

### 1. 安装依赖
```bash
npm install
```

### 2. 配置构建路径
复制配置文件并修改：
```bash
cp config.example.env .env
```

编辑 `.env` 文件，设置您的WebGL构建路径：
```env
WEBGL_BUILD_PATH=../MyUnityProject/WebGLBuild
PORT=3000
```

### 3. 启动服务器

#### 方式一：使用环境变量
```bash
# 使用默认路径 (WebGLBuild)
npm start

# 使用自定义路径
WEBGL_BUILD_PATH=../MyGame/WebGLBuild npm start
```

#### 方式二：使用npm脚本
```bash
# 使用默认路径
npm start

# 使用自定义路径
npm run start:path --path=../MyGame/WebGLBuild
```

## 🐳 Docker 部署

### 使用 Docker Compose (推荐)
```bash
# 使用默认路径
docker-compose up -d

# 使用自定义路径
WEBGL_BUILD_PATH=../MyGame/WebGLBuild docker-compose up -d

# 或使用npm脚本
npm run docker:compose:path --path=../MyGame/WebGLBuild
```

### 使用 Docker 命令
```bash
# 使用默认路径
docker build -t unity-webgl .
docker run -p 8080:80 unity-webgl

# 使用自定义路径
docker build --build-arg WEBGL_BUILD_PATH=../MyGame/WebGLBuild -t unity-webgl .
docker run -p 8080:80 unity-webgl
```

## 📁 支持的路径格式

- 相对路径：`../MyUnityProject/WebGLBuild`
- 绝对路径：`/path/to/your/build`
- 当前目录：`./MyBuild` 或 `MyBuild`

## 🔧 配置选项

| 环境变量 | 默认值 | 说明 |
|---------|--------|------|
| `WEBGL_BUILD_PATH` | `WebGLBuild` | WebGL构建文件路径 |
| `PORT` | `3000` | 服务器端口 (Node.js) |
| `DOCKER_PORT` | `8080` | Docker容器端口 |

## 📋 使用场景

### 场景1：Unity项目在上级目录
```
Project/
├── UnityProject/
│   └── WebGLBuild/     # Unity构建输出
└── Server/             # 当前服务器目录
    ├── server.js
    └── package.json
```

配置：
```env
WEBGL_BUILD_PATH=../UnityProject/WebGLBuild
```

### 场景2：多个构建版本
```
Project/
├── Builds/
│   ├── WebGLBuild_v1/
│   ├── WebGLBuild_v2/
│   └── WebGLBuild_latest/
└── Server/
    ├── server.js
    └── package.json
```

配置：
```env
WEBGL_BUILD_PATH=./Builds/WebGLBuild_latest
```

### 场景3：绝对路径
```env
WEBGL_BUILD_PATH=/Users/username/UnityProjects/MyGame/WebGLBuild
```

## 🛠️ 开发

### 热重载开发模式
```bash
npm run dev
```

### 健康检查
访问 `http://localhost:3000/health` 检查服务器状态。

## 📝 注意事项

1. 确保指定的WebGL构建路径存在且包含必要的文件（index.html, .wasm, .data等）
2. 路径支持相对路径和绝对路径
3. Docker部署时，确保构建路径在Docker构建上下文中
4. 使用Docker Compose时，路径会通过volume挂载，支持实时更新

## 🔍 故障排除

### 路径不存在错误
```
❌ WebGL构建路径不存在: /path/to/build
请设置正确的 WEBGL_BUILD_PATH 环境变量或确保 WebGLBuild 目录存在
```

**解决方案：**
1. 检查路径是否正确
2. 确保Unity已成功构建WebGL版本
3. 验证路径中的文件是否完整

### Docker构建失败
**解决方案：**
1. 确保构建路径在Docker构建上下文中
2. 使用相对路径而不是绝对路径
3. 检查Dockerfile中的COPY指令路径