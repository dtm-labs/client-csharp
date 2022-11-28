English | [简体中文](./RunOnDapr-cn.md)

# How to run dtm and sample in dapr

## Start DTM with dapr driver and dapr sidecar

In dtm server directory, run below command:

```bash
git clone github.com/dtm-labs/dtm && cd dtm
MICRO_SERVICE_DRIVER=dtm-driver-dapr dapr run --app-id dtm --app-port 36789 --dapr-http-port 3601 -- go run main.go
```

## Start sample with dapr sidecar

In DtmOnDaprSample directory, run below command:

```bash
dapr run --app-id sample --app-port 9090 --dapr-http-port 3602 -- dotnet run
```