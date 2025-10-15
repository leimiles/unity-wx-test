// 加载环境变量
require('dotenv').config();

const express = require('express');
const cors = require('cors');
const compression = require('compression');
const path = require('path');
const fs = require('fs');

const app = express();
const PORT = process.env.PORT || 3000;

// 获取WebGL构建路径配置
const WEBGL_BUILD_PATH = process.env.WEBGL_BUILD_PATH || 'WebGLBuild';
const fullBuildPath = path.resolve(__dirname, WEBGL_BUILD_PATH);

// 检查构建路径是否存在
if (!fs.existsSync(fullBuildPath)) {
  console.error(`❌ WebGL构建路径不存在: ${fullBuildPath}`);
  console.error(`请设置正确的 WEBGL_BUILD_PATH 环境变量或确保 ${WEBGL_BUILD_PATH} 目录存在`);
  process.exit(1);
}

console.log(`📁 使用WebGL构建路径: ${fullBuildPath}`);

// 中间件配置
app.use(compression());
app.use(cors());

// 设置静态文件目录
app.use(express.static(fullBuildPath));

// 设置MIME类型
app.use('*.wasm', (req, res, next) => {
  res.setHeader('Content-Type', 'application/wasm');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  next();
});

app.use('*.data', (req, res, next) => {
  res.setHeader('Content-Type', 'application/octet-stream');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  next();
});

app.use('*.symbols.json', (req, res, next) => {
  res.setHeader('Content-Type', 'application/json');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  next();
});

// 路由配置
app.get('/', (req, res) => {
  res.sendFile(path.join(fullBuildPath, 'index.html'));
});

// 健康检查端点
app.get('/health', (req, res) => {
  res.json({ 
    status: 'ok', 
    timestamp: new Date().toISOString(),
    uptime: process.uptime()
  });
});

// 启动服务器
app.listen(PORT, () => {
  console.log(`🚀 Unity WebGL 服务器运行在 http://localhost:${PORT}`);
  console.log(`📁 构建路径: ${fullBuildPath}`);
  console.log(`🔍 健康检查: http://localhost:${PORT}/health`);
});

// 优雅关闭
process.on('SIGTERM', () => {
  console.log('收到SIGTERM信号，正在关闭服务器...');
  process.exit(0);
});

process.on('SIGINT', () => {
  console.log('收到SIGINT信号，正在关闭服务器...');
  process.exit(0);
});
