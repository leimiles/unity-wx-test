# Unity WebGL 运行环境

这个项目提供了多种部署Unity WebGL应用的解决方案。

## 目录结构

```
Server/
├── WebGLBuild/          # Unity WebGL构建输出目录
├── build-scripts/       # 构建脚本
│   ├── build-webgl.bat  # Windows构建脚本
│   └── build-webgl.sh   # macOS/Linux构建脚本
├── Dockerfile           # Docker镜像配置
├── docker-compose.yml   # Docker Compose配置
├── nginx.conf           # Nginx配置文件
├── package.json         # Node.js依赖配置
├── server.js            # Express服务器
└── README.md            # 项目说明文档
```

## 部署方法

### 方法一：Node.js Express服务器

1. **安装依赖**
```bash
npm install
```

2. **构建Unity WebGL项目**
   - 将Unity项目构建为WebGL平台
   - 将构建文件放入 `WebGLBuild/` 目录

3. **启动服务器**
```bash
npm start
```

4. **访问应用**
   - 打开浏览器访问 `http://localhost:3000`

### 方法二：Docker部署

1. **构建Docker镜像**
```bash
docker build -t unity-webgl .
```

2. **运行容器**
```bash
docker run -p 8080:80 unity-webgl
```

3. **使用Docker Compose**
```bash
docker-compose up -d
```

### 方法三：Nginx静态文件服务

1. **安装Nginx**
2. **配置nginx.conf**
3. **将WebGL构建文件复制到nginx目录**
4. **启动Nginx服务**

## 构建脚本使用

### Windows
```cmd
build-scripts\build-webgl.bat
```

### macOS/Linux
```bash
chmod +x build-scripts/build-webgl.sh
./build-scripts/build-webgl.sh
```

## 注意事项

1. **Unity版本兼容性**：确保Unity版本支持WebGL构建
2. **浏览器兼容性**：建议在主流浏览器中测试
3. **文件大小限制**：注意WebGL构建文件的大小
4. **HTTPS要求**：某些功能可能需要HTTPS环境
5. **CORS配置**：确保正确配置跨域资源共享

## 常见问题

### Q: WebGL应用无法加载？
A: 检查以下几点：
- 确保所有文件都在正确的目录中
- 检查MIME类型配置
- 确认浏览器支持WebGL
- 查看浏览器控制台错误信息

### Q: 性能问题？
A: 优化建议：
- 启用gzip压缩
- 使用CDN加速
- 优化Unity项目设置
- 考虑使用WebAssembly优化

## 技术栈

- **Unity**: 游戏引擎
- **Node.js**: 服务器运行时
- **Express**: Web框架
- **Nginx**: Web服务器
- **Docker**: 容器化部署
