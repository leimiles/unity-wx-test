#!/bin/bash

echo "开始构建Unity WebGL项目..."

# 设置Unity安装路径（请根据实际情况修改）
UNITY_PATH="/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app/Contents/MacOS/Unity"

# 设置项目路径
PROJECT_PATH="/Users/username/Projects/unity-wx-test/UnityProject"

# 设置构建输出路径
BUILD_PATH="/Users/username/Projects/unity-wx-test/Server/WebGLBuild"

# 创建构建输出目录
mkdir -p "$BUILD_PATH"

# 执行Unity构建命令
"$UNITY_PATH" -batchmode -quit -projectPath "$PROJECT_PATH" -buildTarget WebGL -executeMethod BuildScript.BuildWebGL -logFile build.log

if [ $? -eq 0 ]; then
    echo "构建成功！"
    echo "构建文件已输出到: $BUILD_PATH"
else
    echo "构建失败！请检查build.log文件"
    exit 1
fi
