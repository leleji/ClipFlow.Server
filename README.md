# ClipFlow服务端

## 安装指南

搭建完成后访问ip:port，出现Version:版本号 即搭建成功

#### Docker

```
docker run -d \
  --name=clipflow-server \
  -p 6060:8080 \
  -e AppSettings__FileCacheMinutes=30 \
  -e AppSettings__TokenSettings__0__Token=token1 \
  -e AppSettings__TokenSettings__0__MaxFileSize=0.2 \
  -e AppSettings__TokenSettings__1__Token=token2 \
  -e AppSettings__TokenSettings__1__MaxFileSize=1.0 \
  --restart unless-stopped \
  leleji/clipflow-server:latest
```

#### Docker Compose

映射appsettings.json到容器的/app/appsettings.json例子

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
   TokenSettings": [
      {
        "Token": "token1", //用来校验客户端的认证token。可随意填写
        "MaxFileSize": 0.1 //最大上传多少mb，默认0不限制
      },
      {
        "Token": "token2",
        "MaxFileSize": 0
      }
    ],
  "FileCacheMinutes": 60 // 默认缓存60分钟
}
```


