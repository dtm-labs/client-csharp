# 如何在Dapr中运行DTM和本例子

## 改变appsettings.json的DtmUrl和BusiUrl配置

```json
"DtmUrl": "http://localhost:3602/v1.0/invoke/dtm/method",
"BusiUrl": "http://localhost:3601/v1.0/invoke/sample/method/api",
```

## 启动DTM和dapr边车

在dtm服务器的目录中，运行如下命令:

```bash
dapr run --app-id dtm --app-port 36789 --dapr-http-port 3601 -- go run main.go
```

## 启动示例应用和dapr边车

在DtmSample文件夹，运行如下命令：

```bash
dapr run --app-id sample --app-port 9090 --dapr-http-port 3602 -- dotnet run
```