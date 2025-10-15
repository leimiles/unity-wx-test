# 使用Nginx作为Web服务器
FROM nginx:alpine

# 复制Unity WebGL构建文件到nginx目录
COPY ./WebGLBuild /usr/share/nginx/html

# 复制nginx配置文件
COPY nginx.conf /etc/nginx/nginx.conf

# 暴露端口
EXPOSE 80

# 启动nginx
CMD ["nginx", "-g", "daemon off;"]
