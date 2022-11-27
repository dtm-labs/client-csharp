English | [简体中文](./README-cn.md)

# dtmcli-csharp-sample
The sample repository of [dtmcli-csharp](https://github.com/dtm-labs/dtmcli-csharp)

# Quick start

## Non docker user

### Deploy and start DTM server

Reference resources [Installation](https://en.dtm.pub/guide/install.html)

### Run the sample

```sh
cd DtmSample
dotnet run DtmSample.csproj
```

Open through browser with this URL `http://localhost:9090 `. It will jump to the swagger page and you can selectively test the corresponding type of transaction mode.

> NOTE: In order to facilitate quick experience, the database in the sample code can be used directly.

## Docker user

By executing `runsample.ps1` to run the sample code quickly.

It will start DTM server, MySQL (demonstration sub transaction barrier) and dtmsample through **docker compose**.

After startup, you can see output similar to the following

![](./media/run.png)

Open through browser with this URL `http://localhost:9090 `. It will jump to the swagger page and you can selectively test the corresponding type of transaction mode.
