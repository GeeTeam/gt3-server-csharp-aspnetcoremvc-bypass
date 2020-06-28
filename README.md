# gt3-server-csharp-aspnetcoremvc-sdk

## 示例部署环境
条目|说明
----|----
操作系统|ubuntu 16.04.6 lts
.net core版本|dotnet-sdk-3.1.301

## 部署流程

### 下载sdk demo
```
git clone https://github.com/GeeTeam/gt3-server-csharp-aspnetcoremvc-sdk.git
```

### 配置密钥，修改请求参数
> 配置密钥

从[极验管理后台](https://auth.geetest.com/login/)获取公钥（id）和私钥（key）, 并在代码中配置。配置文件的相对路径如下：
```
Controllers/GeetestConfig.cs
```

> 修改请求参数（可选）

名称|说明
----|------
user_id|user_id作为客户端用户的唯一标识，作用于提供进阶数据分析服务，可在register和validate接口传入，不传入也不影响验证服务的使用；若担心用户信息风险，可作预处理(如哈希处理)再提供到极验
client_type|客户端类型，web：电脑上的浏览器；h5：手机上的浏览器，包括移动应用内完全内置的web_view；native：通过原生sdk植入app应用的方式；unknown：未知
ip_address|客户端请求sdk服务器的ip地址

### 关键文件说明
名称|说明|相对路径
----|----|----
GeetestController.cs|接口请求控制器，主要处理验证初始化和二次验证接口请求|Controllers/
GeetestConfig.cs|配置id和key|Controllers/
GeetestLib.cs|核心sdk，处理各种业务|Controllers/Sdk/
GeetestLibResult.cs|核心sdk返回数据的包装对象|Controllers/Sdk/
index.html|demo示例首页|wwwroot/
launchSettings.json|启动配置文件，服务器、ip、端口等|Properties/
Startup.cs|程序运行相关配置，如服务、路由、中间件等|

### 运行demo
```
cd gt3-server-csharp-aspnetcoremvc-sdk
sudo dotnet watch run
```
在浏览器中访问`http://localhost:5001`即可看到demo界面。

## 发布日志

### tag：20200701
- 统一各语言sdk标准
- 版本：csharp-aspnetcoremvc:3.1.0

>>>>>>> dev
