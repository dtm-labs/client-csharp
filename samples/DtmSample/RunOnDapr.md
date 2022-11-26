English | [简体中文](./RunOnDapr-cn.md)

# How to run dtm and sample in dapr

## Change DtmUrl and BusiUrl in appsettings.json

```json
"DtmUrl": "http://localhost:3602/v1.0/invoke/dtm/method",
"BusiUrl": "http://localhost:3601/v1.0/invoke/sample/method/api",
```

## Start DTM with dapr sidecar

In dtm server directory, run below command:

```bash
dapr run --app-id dtm --app-port 36789 --dapr-http-port 3601 -- go run main.go
```

## Start sample with dapr sidecar

In DtmSample directory, run below command:

```bash
dapr run --app-id sample --app-port 9090 --dapr-http-port 3602 -- dotnet run
```