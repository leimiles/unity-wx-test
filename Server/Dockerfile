# 使用Nginx作为Web服务器
FROM nginx:alpine

# 设置构建参数，默认为WebGLBuild
ARG WEBGL_BUILD_PATH=WebGLBuild

# 复制Unity WebGL构建文件到nginx目录
COPY ./${WEBGL_BUILD_PATH} /usr/share/nginx/html

# 复制nginx配置文件
COPY nginx.conf /etc/nginx/nginx.conf

# 暴露端口
EXPOSE 80

# 启动nginx
CMD ["nginx", "-g", "daemon off;"]
