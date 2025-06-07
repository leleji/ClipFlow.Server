# 基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# 拷贝已发布的文件
COPY ./publish . 

# 设置环境变量
ENV TZ=Asia/Shanghai

ENTRYPOINT ["dotnet", "ClipFlow.Server.dll"]