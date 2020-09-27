using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Gt3_server_csharp_aspnetcoremvc_sdk.Controllers.Sdk;
using CSRedis;
using System.Threading;
using System.IO;
using System.Text;
using System.Web;
using System.Net;
using Newtonsoft.Json;

namespace Gt3_server_csharp_aspnetcoremvc_sdk.Controllers
{
    public class GeetestController : Controller
    {
        public int HTTP_TIMEOUT_DEFAULT = 5000;
        public string BYPASS_URL = GeetestConfig.BYPASS_URL;
        protected static string _connect_str = GeetestConfig.REDIS_HOST;
        protected static CSRedisClient rds_conn = null;
        protected static object _lockObj_conn = new object();
        public IDictionary<string, string> paramDict = new Dictionary<string, string> { { "gt", GeetestConfig.GEETEST_ID } };

        private string HttpGet(string url, IDictionary<string, string> paramDict)
        {
            Stream resStream = null;
            try
            {
                StringBuilder paramStr = new StringBuilder();
                foreach (KeyValuePair<string, string> item in paramDict)
                {
                    if (!(string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
                    {
                        paramStr.AppendFormat("&{0}={1}", HttpUtility.UrlEncode(item.Key, Encoding.UTF8), HttpUtility.UrlEncode(item.Value, Encoding.UTF8));
                    }
                }
                url = url + "?" + paramStr.ToString().Substring(1);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.ReadWriteTimeout = HTTP_TIMEOUT_DEFAULT;
                req.Timeout = HTTP_TIMEOUT_DEFAULT;
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                resStream = res.GetResponseStream();
                StreamReader reader = new StreamReader(resStream, Encoding.GetEncoding("utf-8"));
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                //throw e;
                return "{\"status\": \"fail\"}";
            }
            finally
            {
                if (resStream != null)
                {
                    resStream.Close();
                }
            }
        }

        public static CSRedisClient GetRedisClient()
        {
            if (string.IsNullOrEmpty(_connect_str))
            {
                Console.WriteLine("Connectstring is not set");
            }
            if (rds_conn == null)
            {
                rds_conn = new CSRedisClient($"{_connect_str},poolsize=50,ssl=false,writeBuffer=10240");
                RedisHelper.Initialization(rds_conn);
            }
            return rds_conn;
        }

        public class ResBody
        {
            public string status { get; set; }
        }

        public void CheckStatus()
        {
            while (true)
            {
                string resBody = HttpGet(BYPASS_URL, paramDict);
                Console.Write("CheckStatus(): 返回body={resBody}.");

                ResBody res = JsonConvert.DeserializeObject<ResBody>(resBody);
                if (rds_conn == null)
                    rds_conn = GetRedisClient();
                rds_conn.Set(key: GeetestConfig.GEETEST_BYPASS_STATUS_KEY, value: res.status);
                rds_conn.Set(key: "ttt", value: DateTime.Now.ToString());
                //return Json(new { result = "success" });
                Thread.Sleep(millisecondsTimeout: GeetestConfig.CYCLE_TIME);
            }
        }

        private string GetBypassCache()
        {
            string cache_status = null;
            if (rds_conn == null)
                rds_conn = GetRedisClient();
            cache_status = rds_conn.Get(GeetestConfig.GEETEST_BYPASS_STATUS_KEY);
            return cache_status;
        }


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
            string bypass_cache = GetBypassCache();
            GeetestLibResult result;
            if (bypass_cache == "success")
            {
                result = gtLib.Register(digestmod, paramDict);
            }
            else
            {
                result = gtLib.LocalRegister();
            }
            // 将结果状态写到session中，此处register接口存入session，后续validate接口会取出使用
            // 注意，此demo应用的session是单机模式，格外注意分布式环境下session的应用
            //HttpContext.Session.SetInt32(GeetestLib.GEETEST_SERVER_STATUS_SESSION_KEY, result.GetStatus());
            //HttpContext.Session.SetString("userId", userId);
            // 注意，不要更改返回的结构和值类型
            return Content(result.GetData(), "application/json;charset=UTF-8");
        }

        [HttpPost("/validate")]
        // 二次验证接口，POST请求
        public JsonResult SecondValidate()
        {
            GeetestLibResult result = null;
            IDictionary<string, string> paramDict = new Dictionary<string, string> { };
            GeetestLib gtLib = new GeetestLib(GeetestConfig.GEETEST_ID, GeetestConfig.GEETEST_KEY);
            string challenge = Request.Form[GeetestLib.GEETEST_CHALLENGE];
            string validate = Request.Form[GeetestLib.GEETEST_VALIDATE];
            string seccode = Request.Form[GeetestLib.GEETEST_SECCODE];
            string bypass_cache = GetBypassCache();

            if (bypass_cache is null)
            {
                return Json(new { result = "fail", version = GeetestLib.VERSION, msg = "获取缓存的bypass状态发生异常" });
            }

            if (bypass_cache == "success")
            {
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
