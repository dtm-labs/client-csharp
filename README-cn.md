[English](./README.md) | 简体中文

# dtmcli-csharp

`dtmcli-csharp` 是分布式事务管理器 DTM 的 C# 客户端，使用 HTTP 协议和 DTM 服务端进行交互。

目前已经支持 SAGA 、 TCC 和二阶段消息三种事务模式。


## dtm分布式事务管理服务

DTM是一款变革性的分布式事务框架，提供了傻瓜式的使用方式，极大的降低了分布式事务的使用门槛，改变了“能不用分布式事务就不用”的行业现状。 dtm 的应用范围非常广，可以应用于以下常见的领域：
- [非单体的订单系统，大幅简化架构](https://dtm.pub/app/order.html)
- [秒杀系统，做到在Redis中精准扣库存](https://dtm.pub/app/flash.html)
- [保证缓存与DB的一致性](https://dtm.pub/app/cache.html)
- 微服务架构中跨服务更新数据保证一致性

他优雅的解决了幂等、空补偿、悬挂等分布式事务难题，提供跨语言，跨存储引擎组合事务的强大功能：

<img src="https://pica.zhimg.com/80/v2-2f66cb3074e68d38c29694318680acac_1440w.png" height=250 />

## 亮点

* 极易接入
  - 支持HTTP，提供非常简单的接口，极大降低上手分布式事务的难度，新手也能快速接入
* 使用简单
  - 开发者不再担心悬挂、空补偿、幂等各类问题，框架层代为处理
* 跨语言
  - 可适合多语言栈的公司使用。方便go、python、php、nodejs、ruby、c# 各类语言使用。
* 易部署、易扩展
  - 仅依赖mysql，部署简单，易集群化，易水平扩展
* 多种分布式事务协议支持
  - TCC、SAGA、XA、事务消息

## 与其他框架对比

目前开源的分布式事务框架，暂未看到非Java语言有成熟的框架。而Java语言的较多，有阿里的SEATA、华为的ServiceComb-Pack，京东的shardingsphere，以及himly，tcc-transaction，ByteTCC等等，其中以seata应用最为广泛。

下面是dtm和seata的主要特性对比：

|  特性| DTM | SEATA |备注|
|:-----:|:----:|:----:|:----:|
| 支持语言 |<span style="color:green">Golang, C#, Java, Python, PHP及其他</span>|<span style="color:orange">Java</span>|dtm可轻松接入一门新语言|
|异常处理| <span style="color:green">[子事务屏障自动处理](https://zhuanlan.zhihu.com/p/388444465)</span>|<span style="color:orange">手动处理</span> |dtm解决了幂等、悬挂、空补偿|
| TCC事务| <span style="color:green">✓</span>|<span style="color:green">✓</span>||
| XA事务|<span style="color:green">✓</span>|<span style="color:green">✓</span>||
|AT事务|<span style="color:red">✗</span>|<span style="color:green">✓</span>|AT与XA类似，性能更好，但有脏回滚|
| SAGA事务 |<span style="color:orange">简单模式</span> |<span style="color:green">状态机复杂模式</span> |dtm的状态机模式在规划中|
|事务消息|<span style="color:green">✓</span>|<span style="color:red">✗</span>|dtm提供类似rocketmq的事务消息|
|通信协议|HTTP|dubbo等协议，无HTTP|dtm后续将支持grpc类协议|
|star数量|<img src="https://img.shields.io/github/stars/yedf/dtm.svg?style=social" alt="github stars"/>|<img src="https://img.shields.io/github/stars/seata/seata.svg?style=social" alt="github stars"/>|dtm从20210604发布0.1，发展快|

从上面对比的特性来看，如果您的语言栈包含了Java之外的语言，那么dtm是您的首选。如果您的语言栈是Java，您也可以选择接入dtm，使用子事务屏障技术，简化您的业务编写。


## 安装

通过下面的命令安装 nuget 包

```sh
dotnet add package Dtmcli
```

## 配置

这里有两种方式进行配置

1. 使用 setup action

```cs
services.AddDtmcli(x =>
{
    // DTM server 的 HTTP 地址
    x.DtmUrl = "http://localhost:36789";
    
    // 请求 DTM server 的超时时间, 单位是毫秒
    x.DtmTimeout = 10000; 
    
    // 请求分支事务的超时时间, 单位是毫秒
    x.BranchTimeout = 10000;
    
    // 子事务屏障的数据库类型, mysql, postgres, sqlserver
    x.DBType = "mysql";

    // 子事务屏障的数据表名
    x.BarrierTableName = "dtm_barrier.barrier";
});
```

2. 使用  `IConfiguration`

```cs
services.AddDtmcli(Configuration, "dtm");
```

添加配置文件，以 JSON 为例：

```JSON
{
  "dtm": {
    "DtmUrl": "http://localhost:36789",
    "DtmTimeout": 10000,
    "BranchTimeout": 10000,
    "DBType": "mysql",
    "BarrierTableName": "dtm_barrier.barrier",
  }
}
```

## 用法

### SAGA

```cs
public class MyBusi
{ 
    private readonly Dtmcli.IDtmTransFactory _transFactory;

    public MyBusi(Dtmcli.IDtmTransFactory transFactory)
    {
        this._transFactory = transFactory;
    }

    public async Task DoBusAsync()
    {
        var gid = Guid.NewGuid().ToString();
        var req = new BusiReq {  Amount = 30 };
        
        // NOTE: After DTM v1.12.2
        // when svc start with http or https, DTM server will send HTTP request to svc
        // when svc don't start with http or https,  DTM server will send gRPC request to svc
        var svc = "http://localhost:5005";

        var saga = _transFactory.NewSaga(gid);
        // 添加子事务操作
        saga.Add(
            // 正向操作 URL
            svc + "/api/TransOut",
            
            // 逆向操作 URL
            svc + "/api/TransOutCompensate",

            // 参数
            req);
        saga.Add(
            svc + "/api/TransIn",
            svc + "/api/TransInCompensate",
            req);

        await saga.Submit();
    }
}
```

### TCC pattern

```cs
public class MyBusi
{ 
    private readonly Dtmcli.TccGlobalTransaction _globalTransaction;

    public MyBusi(Dtmcli.TccGlobalTransaction globalTransaction)
    {
        this._globalTransaction = globalTransaction;
    }

    public async Task DoBusAsync()
    {
        var gid = Guid.NewGuid().ToString();
        var req = new BusiReq {  Amount = 30 };
        var svc = "http://localhost:5005";

        await _globalTransaction.Excecute(gid, async tcc =>
        {
            // 调用 TCC 子事务
            await tcc.CallBranch(
                // 参数
                req,

                // Try 阶段的 URL
                svc + "/api/TransOutTry",

                // Confirm 阶段的 URL 
                svc + "/api/TransOutConfirm",

                // Cancel 阶段的 URL
                svc + "/api/TransOutCancel");

            await tcc.CallBranch(
                req,
                svc + "/api/TransInTry",
                svc + "/api/TransInConfirm",
                svc + "/api/TransInCancel");
        });
    }
}
```

### 二阶段消息

```cs
public class MyBusi
{ 
    private readonly Dtmcli.IDtmTransFactory _transFactory;

    public MyBusi(Dtmcli.IDtmTransFactory transFactory)
    {
        this._transFactory = transFactory;
    }

    public async Task DoBusAsync()
    {
        var gid = Guid.NewGuid().ToString();
        var req = new BusiReq {  Amount = 30 };
        var svc = "http://localhost:5005";

        var msg = _transFactory.NewMsg(gid);
        // 添加子事务操作
        msg.Add(
            // 操作的 URL
            svc + "/api/TransOut",

            // 参数
            req);
        msg.Add(
            svc + "/api/TransIn",
            req);

        // 用法 1:
        // 发送 prepare 消息
        await msg.Prepare(svc + "/api/QueryPrepared");
        // 发送 submit 消息
        await msg.Submit();

        // 用法 2:
        using (var conn = GetDbConnection())
        {
            await msg.DoAndSubmitDB(svc + "/api/QueryPrepared", conn, async tx => 
            {
                await conn.ExecuteAsync("insert ....", new { }, tx);
                await conn.ExecuteAsync("update ....", new { }, tx);
                await conn.ExecuteAsync("delete ....", new { }, tx);
            });
        }
    }
}
```


## 可运行的使用示例

见[https://github.com/dtm-labs/dtmcli-csharp-sample](https://github.com/dtm-labs/dtmcli-csharp-sample)

## 联系我们
### 公众号
dtm官方公众号：分布式事务，大量干货分享，以及dtm的最新消息
### 交流群
请加 yedf2008 好友或者扫码加好友，验证回复 csharp 按照指引进群

![yedf2008](http://service.ivydad.com/cover/dubbingb6b5e2c0-2d2a-cd59-f7c5-c6b90aceb6f1.jpeg)

