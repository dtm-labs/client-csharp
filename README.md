English | [简体中文](./README-cn.md)

# dtmcli-csharp

`dtmcli-csharp` is the C# client of Distributed Transaction Manager [DTM](https://github.com/dtm-labs/dtm) that communicates with DTM Server through HTTP protocol. 

It has supported distributed transaction patterns of Saga pattern, TCC pattern and 2-phase message pattern.

![Build_And_Test](https://github.com/dtm-labs/dtmcli-csharp/actions/workflows/build.yml/badge.svg) [![codecov](https://codecov.io/gh/dtm-labs/dtmcli-csharp/branch/main/graph/badge.svg?token=Y2BOSQ5QKO)](https://codecov.io/gh/dtm-labs/dtmcli-csharp)

![](https://img.shields.io/nuget/v/Dtmcli.svg)  ![](https://img.shields.io/nuget/vpre/Dtmcli.svg) ![](https://img.shields.io/nuget/dt/Dtmcli) ![](https://img.shields.io/github/license/dtm-labs/dtmcli-csharp)


## What is DTM

DTM is a distributed transaction solution which provides cross-service eventually data consistency. It provides saga, tcc, xa, 2-phase message strategies for a variety of application scenarios. It also supports multiple languages and multiple store engine to form up a transaction as following:

<!--
- [非单体的订单系统，大幅简化架构](https://dtm.pub/app/order.html)
- [秒杀系统，做到在Redis中精准扣库存](https://dtm.pub/app/flash.html)
- [保证缓存与DB的一致性](https://dtm.pub/app/cache.html)
- 微服务架构中跨服务更新数据保证一致性
-->

<img alt="function-picture" src="https://en.dtm.pub/assets/function.7d5618f8.png" height=250 />

## Features

* Extremely easy to adapt
  - Support HTTP and gRPC, provide easy-to-use programming interfaces, lower substantially the barrier of getting started with distributed transactions. Newcomers can adapt quickly.

* Easy to use
  - Relieving developers from worrying about suspension, null compensation, idempotent transaction, and other tricky problems, the framework layer handles them all.

* Language-agnostic
  - Suit for companies with multiple-language stacks.
    Easy to write bindings for Go, Python, PHP, Node.js, Ruby, and other languages.

* Easy to deploy, easy to extend
  - DTM depends only on MySQL, easy to deploy, cluster, and scale horizontally.

* Support for multiple distributed transaction protocol
  - TCC, SAGA, XA, Transactional messages.

## DTM vs. others

There is no mature open-source distributed transaction framework for non-Java languages.
Mature open-source distributed transaction frameworks for Java language include Ali's Seata, Huawei's ServiceComb-Pack, Jingdong's shardingsphere, himly, tcc-transaction, ByteTCC, and so on, of which Seata is most widely used.

The following is a comparison of the main features of dtm and Seata.

| Features                | DTM                                                                                           | Seata                                                                                            | Remarks                                                             |
| :-----:                 | :----:                                                                                        | :----:                                                                                           | :----:                                                              |
| Supported languages     | <span style="color:green">Golang, C#, Java, Python, PHP,  and others</span>                               | <span style="color:orange">Java</span>                                                           | dtm allows easy access from a new language                            |
| Exception handling      | [Sub-transaction barrier](https://zhuanlan.zhihu.com/p/388444465)                             | <span style="color:orange">manual</span>                                                         | dtm solves idempotent transaction, hanging, null compensation                   |
| TCC                     | <span style="color:green">✓</span>                                                            | <span style="color:green">✓</span>                                                               |                                                                     |
| XA                      | <span style="color:green">✓</span>                                                            | <span style="color:green">✓</span>                                                               |                                                                     |
| AT                      | <span style="color:orange">suggest XA</span>                                                              | <span style="color:green">✓</span>                                                               | AT is similar to XA with better performance but with dirty rollback |
| SAGA                    | <span style="color:green">support concurrency</span>                                                 | <span style="color:green">complicated state-machine mode</span>                                   | dtm's state-machine mode is being planned                         |
| Transactional Messaging | <span style="color:green">✓</span>                                                            | <span style="color:red">✗</span>                                                                 | dtm provides Transactional Messaging similar to RocketMQ               |
| Multiple DBs in a service |<span style="color:green">✓</span>|<span style="color:red">✗</span>||
| Communication protocols | <span style="color:green">HTTP, gRPC</span>                                                   | <span style="color:green">Dubbo, no HTTP</span>                                             |                                                                     |
| Star count              | <img src="https://img.shields.io/github/stars/dtm-labs/dtm.svg?style=social" alt="github stars"/> | <img src="https://img.shields.io/github/stars/seata/seata.svg?style=social" alt="github stars"/> | dtm 0.1 is released from 20210604 and under fast development                    |

From the features' comparison above, if your language stack includes languages other than Java, then dtm is the one for you.
If your language stack is Java, you can also choose to access dtm and use sub-transaction barrier technology to simplify your business development.

## Installation

Add nuget package via the following command

```sh
dotnet add package Dtmcli
```

## Configuration

There are two ways to configure

1. Configure with setup action

```cs
services.AddDtmcli(x =>
{
    // DTM server HTTP address
    x.DtmUrl = "http://localhost:36789";
    
    // request timeout for DTM server, unit is milliseconds
    x.DtmTimeout = 10000; 
    
    // request timeout for trans branch, unit is milliseconds
    x.BranchTimeout = 10000;
    
    // barrier database type, mysql, postgres, sqlserver
    x.DBType = "mysql";

    // barrier table name
    x.BarrierTableName = "dtm_barrier.barrier";
});
```

2. Configure with `IConfiguration`

```cs
services.AddDtmcli(Configuration, "dtm");
```

And the configuration file 

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

## Usage

### SAGA pattern

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
        // Add sub-transaction
        saga.Add(
            // URL of forward action 
            svc + "/api/TransOut",
            
            // URL of compensating action
            svc + "/api/TransOutCompensate",

            // Arguments of actions
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
            // Create tcc sub-transaction
            await tcc.CallBranch(
                // Arguments of stages
                req,

                // URL of Try stage
                svc + "/api/TransOutTry",

                // URL of Confirm stage
                svc + "/api/TransOutConfirm",

                 // URL of Cancel stage
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

### 2-phase message pattern

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
        // Add sub-transaction
        msg.Add(
            // URL of action 
            svc + "/api/TransOut",

            // Arguments of action
            req);
        msg.Add(
            svc + "/api/TransIn",
            req);

        // Usage 1:
        // Send prepare message 
        await msg.Prepare(svc + "/api/QueryPrepared");
        // Send submit message
        await msg.Submit();

        // Usage 2:
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

## Complete example


Refer to [https://github.com/dtm-labs/dtmcli-csharp-sample](https://github.com/dtm-labs/dtmcli-csharp-sample)

## Contact us

<!--
### WeChat official account

dtm官方公众号：分布式事务，大量干货分享，以及dtm的最新消息

-->

### Wechat communication group

Add wechat friend with id **yedf2008**, or scan the OR code. Fill in **csharp** as verification.

![yedf2008](http://service.ivydad.com/cover/dubbingb6b5e2c0-2d2a-cd59-f7c5-c6b90aceb6f1.jpeg)

