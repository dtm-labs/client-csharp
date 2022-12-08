# 如何在Dapr中运行DTM和本例子

## 使用Dapr驱动启动DTM和dapr边车

在dtm服务器的目录中，运行如下命令:

```bash
git clone github.com/dtm-labs/dtm && cd dtm
MICRO_SERVICE_DRIVER=dtm-driver-dapr dapr run --app-id dtm --app-port 36789 --dapr-http-port 3601 -- go run main.go
```

## 启动示例应用和dapr边车

在DtmOnDaprSample文件夹，运行如下命令：

```bash
dapr run --app-id sample --app-port 9090 --dapr-http-port 3602 -- dotnet run
```