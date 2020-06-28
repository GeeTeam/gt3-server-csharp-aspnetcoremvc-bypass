using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Gt3_server_csharp_aspnetcoremvc_sdk.Controllers.Sdk;

namespace Gt3_server_csharp_aspnetcoremvc_sdk.Controllers
{
    public class GeetestController : Controller
    {

        [HttpGet("/register")]
        // 验证初始化接口，GET请求
        public ContentResult FirstRegister()
        {
            /*
            必传参数
                digestmod 此版本sdk可支持md5、sha256、hmac-sha256，md5之外的算法需特殊配置的账号，联系极验客服
            自定义参数,可选择添加
                user_id user_id作为客户端用户的唯一标识，确定用户的唯一性；作用于提供进阶数据分析服务，可在register和validate接口传入，不传入也不影响验证服务的使用；若担心用户信息风险，可作预处理(如哈希处理)再提供到极验
                client_type 客户端类型，web：电脑上的浏览器；h5：手机上的浏览器，包括移动应用内完全内置的web_view；native：通过原生sdk植入app应用的方式；unknown：未知
                ip_address 客户端请求sdk服务器的ip地址
            */
            GeetestLib gtLib = new GeetestLib(GeetestConfig.GEETEST_ID, GeetestConfig.GEETEST_KEY);
            string userId = "test";
            string digestmod = "md5";
            IDictionary<string, string> paramDict = new Dictionary<string, string> { { "digestmod", digestmod }, { "user_id", userId }, { "client_type", "web" }, { "ip_address", "127.0.0.1" } };
            GeetestLibResult result = gtLib.Register(digestmod, paramDict);
            // 将结果状态写到session中，此处register接口存入session，后续validate接口会取出使用
            // 注意，此demo应用的session是单机模式，格外注意分布式环境下session的应用
            HttpContext.Session.SetInt32(GeetestLib.GEETEST_SERVER_STATUS_SESSION_KEY, result.GetStatus());
            HttpContext.Session.SetString("userId", userId);
            // 注意，不要更改返回的结构和值类型
            return Content(result.GetData(), "application/json;charset=UTF-8");
        }

        [HttpPost("/validate")]
        // 二次验证接口，POST请求
        public JsonResult SecondValidate()
        {
            GeetestLib gtLib = new GeetestLib(GeetestConfig.GEETEST_ID, GeetestConfig.GEETEST_KEY);
            string challenge = Request.Form[GeetestLib.GEETEST_CHALLENGE];
            string validate = Request.Form[GeetestLib.GEETEST_VALIDATE];
            string seccode = Request.Form[GeetestLib.GEETEST_SECCODE];
            int? status = HttpContext.Session.GetInt32(GeetestLib.GEETEST_SERVER_STATUS_SESSION_KEY);
            string userId = HttpContext.Session.GetString("userId");
            // session必须取出值，若取不出值，直接当做异常退出
            if (status is null)
            {
                return Json(new { result = "fail", version = GeetestLib.VERSION, msg = "session取key发生异常" });
            }
            GeetestLibResult result = null;
            if (status == 1)
            {
                /*
                自定义参数,可选择添加
                    user_id user_id作为客户端用户的唯一标识，确定用户的唯一性；作用于提供进阶数据分析服务，可在register和validate接口传入，不传入也不影响验证服务的使用；若担心用户信息风险，可作预处理(如哈希处理)再提供到极验
                    client_type 客户端类型，web：电脑上的浏览器；h5：手机上的浏览器，包括移动应用内完全内置的web_view；native：通过原生sdk植入app应用的方式；unknown：未知
                    ip_address 客户端请求sdk服务器的ip地址
                */
                IDictionary<string, string> paramDict = new Dictionary<string, string> { { "user_id", userId }, { "client_type", "web" }, { "ip_address", "127.0.0.1" } };
                result = gtLib.SuccessValidate(challenge, validate, seccode, paramDict);
            }
            else
            {
                result = gtLib.FailValidate(challenge, validate, seccode);
            }
            // 注意，不要更改返回的结构和值类型
            if (result.GetStatus() == 1)
            {
                return Json(new { result = "success", version = GeetestLib.VERSION });
            }
            else
            {
                return Json(new { result = "fail", version = GeetestLib.VERSION, msg = result.GetMsg() });
            }
        }

    }
}
