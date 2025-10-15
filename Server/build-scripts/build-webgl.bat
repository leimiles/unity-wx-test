@echo off
echo 开始构建Unity WebGL项目...

REM 设置Unity安装路径（请根据实际情况修改）
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.25f1\Editor\Unity.exe"

REM 设置项目路径
set PROJECT_PATH="D:\Projects\unity-wx-test\UnityProject"

REM 设置构建输出路径
set BUILD_PATH="D:\Projects\unity-wx-test\Server\WebGLBuild"

REM 创建构建输出目录
if not exist %BUILD_PATH% mkdir %BUILD_PATH%

REM 执行Unity构建命令
%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -buildTarget WebGL -executeMethod BuildScript.BuildWebGL -logFile build.log

if %ERRORLEVEL% EQU 0 (
    echo 构建成功！
    echo 构建文件已输出到: %BUILD_PATH%
) else (
    echo 构建失败！请检查build.log文件
)

pause
