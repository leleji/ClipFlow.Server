# ClipFlow服务端

## 安装指南

需要下载[appsettings.json]([https://](https://raw.githubusercontent.com/leleji/ClipFlow.Server/refs/heads/master/ClipFlow.Server/appsettings.json))放到指定目录映射到/app/appsettings.json

#### Docker

```
docker run -d \
  --name=clipflow-server \
  -p 8080:8080 \
  --restart unless-stopped \
  -v /opt/clipflow/appsettings.json:/app/appsettings.json:ro \
  leleji/clipflow-server:latest
```

#### Docker Compose

```
version: '3'
services:
  syncclipboard-server:
    image: leleji/clipflow-server:latest
    container_name: clipflow-server
    restart: unless-stopped
    ports:
      - "6060:8080"
    volumes:
      - /opt/clipflow/appsettings.json:/app/appsettings.json:ro
```

## appsettings配置说明

```
"AppSettings": {
  "Tokens": [ "token1", "token2" ], //用来校验客户端的认证token。可随意填写
  "FileCacheMinutes": 60, // 默认缓存60分钟
  "MaxFileSize": 0 //最大上传多少mb，默认0不限制
}
```
