# ClipFlow服务端

## 安装指南

#### Docker
```
docker run -d \
  --name=clipflow-server \
  -p 8080:8080 \
  --restart unless-stopped \
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
      - "8080:8080" # Update this if you have changed the port in appsettings.json
    environment:
```



