# 极验行为验证csharp版本ByPassDemo说明

# 1.概述

## 1.1 方案目的

ByPass方案是指在现有的行为验证Failback模式上，基于您的业务逻辑增加验证模块的降级切换方案，确保在任何极端状况下都能及时、可控地实现服务切换，作为对客户业务流程保障的最终兜底机制。

## 1.2 方案说明
1. 极验新增一个独立于行为验证现有的通讯流程之外的服务可用性监控接口，客户可以通过轮询机制请求该接口，通过接口的返回状态确认当前验证服务可用性。
2. 极验提供标准ByPass开发Demo示例（本Demo项目是基于csharp asp .net core mvc框架），Demo包含了通过监控服务可用性接口实现验证模块的自动切换的完整逻辑，客户可以参考Demo基于自身业务实际情况，实现验证模块的ByPass逻辑设计。

# 2.服务可用性接口文档

## 2.1 API文档概述
条目|监控接口
----|----
说明|1.该接口用于检测极验服务运行是否正常，检测范围覆盖极验服务提供的所有 API 接口;<br />2.该接口传入用户验证 ID，针对性检测该用户是否能正常访问极验服务，用户通过此接口的返回状态控制业务是否进入 bypass 模式;<br />3.可针对用户验证 ID 对该用户访问 bypass 接口后的返回状态进行配置，保证突发情况下能使业务立即切换到 bypass 模式;
域名|bypass.geetest.com
接口路径名|/v1/bypass_status.php
完整地址示例|http://bypass.geetest.com/v1/bypass_status.php
请求方式|GET/POST
请求头|(POST)Content-Type: application/x-www-form-urlencoded 
请求参数格式| GET: 键值对, urlEncode<br> POST: 表单, urlEncode
响应头|Content-Type: application/json;charset=UTF-8
响应结果类型|json格式

## 2.2 请求参数
名称|类型|必填|说明
----|----|----|----
gt|string|Y|向极验申请的验证id

> 请求参数示例

```
gt=c9c4facd1a6feeb80802222cbb74ca8e
```

## 2.3 响应参数
名称|类型|必填|说明
----|----|----|----
status|string|Y|success: 极验云服务正常<br>fail: 极验云服务异常

> 响应结果示例

```
{
    "status": "success"
}
```

# 3.Demo项目说明

## 3.1 流程概述
- 项目部署启动，监听器开始执行轮询定时器任务，每隔一段时间(可配置)向极验监控接口 (http://bypass.geetest.com/v1/bypass_status.php) 发起请求，并将返回的标识字段status(success/fail)存入redis数据库中(仅demo示例，可自行设计存储架构)。

- 当客户端向服务端发起register验证初始化和validate二次验证请求时， 先从redis中取出极验状态的标识字段status。若是status是success则走正常流程，后续向极验云发起请求；若status是fail则走宕机模式，后续不论客户端还是服务端，都不会与极验云有任何交互。

## 3.2 示例部署环境
条目|说明
----|----
操作系统|ubuntu 16.04.6 lts
.net core版本|dotnet-sdk-3.1.301
redis数据库|3.0.6

## 3.3 部署流程

### 3.3.1 下载demo
```
git clone https://github.com/GeeTeam/gt3-server-csharp-aspnetcoremvc-bypass.git
```

### 3.3.2 修改项目配置，修改参数
> 修改项目配置

从[极验管理后台](https://auth.geetest.com/login/)获取公钥(id)和私钥(key), 获取redis数据库的相关信息。配置文件的相对路径如下(配置参数说明详见代码)：
```
gt3-server-csharp-aspnetcoremvc-bypass/Controllers/GeetestConfig.cs
```
名称|说明
----|------
GEETEST_ID|从极验管理后台获取的公钥（id）
GEETEST_KEY|从极验管理后台获取的私钥(key)
REDIS_SERVER|对极验接口返回的状态进行缓存的redis服务地址和端口
BYPASS_URL|极验监控接口url
CYCLE_TIME|轮询任务每次运行的时间间隔(单位为毫秒)
GEETEST_BYPASS_STATUS_KEY|将极验接口返回的状态缓存到redis时时使用的key值

> 修改请求参数（可选）

名称|说明
----|------
user_id|客户端用户的唯一标识，作用于提供进阶数据分析服务，可在register和validate接口传入，不传入也不影响验证服务的使用；若担心用户信息风险，可作预处理(如哈希处理)再提供到极验
client_type|客户端类型，web：电脑上的浏览器；h5：手机上的浏览器，包括移动应用内完全内置的web_view；native：通过原生sdk植入app应用的方式；unknown：未知
ip_address|客户端请求sdk服务器的ip地址

### 3.3.3 关键文件说明
名称|说明|相对路径
----|----|----
GeetestController.cs|接口请求控制器，主要处理验证初始化和二次验证接口请求|Controllers/
GeetestConfig.cs|配置参数|Controllers/
GeetestLib.cs|核心sdk，处理各种业务|Controllers/Sdk/
GeetestLibResult.cs|核心sdk返回数据的包装对象|Controllers/Sdk/
index.html|demo示例首页|wwwroot/
gt.js|本地加载的js文件|wwwroot/
launchSettings.json|启动配置文件，服务器、ip、端口等|Properties/
Startup.cs|程序运行相关配置，如服务、路由、中间件等|


### 3.3.4 运行demo
```
cd gt3-server-csharp-aspnetcoremvc-bypass
sudo dotnet watch run
```
在浏览器中访问`http://localhost:5001`即可看到demo界面。


### 3.3.5 模拟宕机模式

- 注意：以下模拟方式原理分为两类，一类是极验云监控接口不可用，网络不通，等同于真实情况极验云遭受攻击或者其他异常导致云端宕机；另一类是极验云监控接口正常，极验云经过自检，发现云端状态异常，而将此异常结果返回。

> 方式一 配置文件中将id改成错误账号

`geetest_config`文件中将`GEETEST_ID`的值改成`1234567890`

> 方式二 配置文件中将极验云监听接口改成错误链接

`geetest_config`文件中将`BYPASS_URL`的值改成`http://www.google.com`

> 方式三 修改服务器hosts，将极验云监听接口域名绑定错误ip

修改服务器hosts配置文件：`127.0.0.1 bypass.geetest.com`

> 方式四 极验云监听接口返回fail状态数据

联系极验客服，提供id号，由极验人员操作，可使极验云端接口直接返回fail状态数据


## 发布日志

### tag：20200928
- 版本：python-flask:3.1.1
