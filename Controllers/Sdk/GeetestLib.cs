using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;

namespace Gt3_server_csharp_aspnetcoremvc_sdk.Controllers.Sdk
{
    /*
     * sdk lib包，核心逻辑。
     *
     * @author liuquan@geetest.com
     */
    public class GeetestLib
    {
        // 公钥
        private string geetest_id;

        // 私钥
        private string geetest_key;

        // 返回数据的封装对象
        private GeetestLibResult libResult;

        // 调试开关，是否输出调试日志
        private const bool IS_DEBUG = true;

        private const string API_URL = "http://api.geetest.com";

        private const string REGISTER_URL = "/register.php";

        private const string VALIDATE_URL = "/validate.php";

        private const string JSON_FORMAT = "1";

        private const bool NEW_CAPTCHA = true;

        private const int HTTP_TIMEOUT_DEFAULT = 5000; // 单位：毫秒

        public const string VERSION = "csharp-aspnetcoremvc:3.1.0";

        // 极验二次验证表单传参字段 chllenge
        public const string GEETEST_CHALLENGE = "geetest_challenge";

        // 极验二次验证表单传参字段 validate
        public const string GEETEST_VALIDATE = "geetest_validate";

        // 极验二次验证表单传参字段 seccode
        public const string GEETEST_SECCODE = "geetest_seccode";

        // 极验验证API服务状态Session Key
        public const string GEETEST_SERVER_STATUS_SESSION_KEY = "gt_server_status";

        public GeetestLib(string geetest_id, string geetest_key)
        {
            this.geetest_id = geetest_id;
            this.geetest_key = geetest_key;
            this.libResult = new GeetestLibResult();
        }

        public void Gtlog(string message)
        {
            if (IS_DEBUG)
            {
                Console.WriteLine("Gtlog: " + message);
            }
        }

        // 验证初始化
        public GeetestLibResult Register(string digestmod, IDictionary<string, string> paramDict)
        {
            this.Gtlog($"Register(): 开始验证初始化, digestmod={digestmod}.");
            string origin_challenge = this.RequestRegister(paramDict);
            this.BuildRegisterResult(origin_challenge, digestmod);
            this.Gtlog($"Register(): 验证初始化, lib包返回信息={this.libResult}.");
            return this.libResult;
        }

        // 向极验发送验证初始化的请求，GET方式
        private string RequestRegister(IDictionary<string, string> paramDict)
        {
            paramDict.Add("gt", this.geetest_id);
            paramDict.Add("json_format", JSON_FORMAT);
            string register_url = API_URL + REGISTER_URL;
            this.Gtlog($"RequestRegister(): 验证初始化, 向极验发送请求, url={register_url}, params={JsonSerializer.Serialize(paramDict)}.");
            string origin_challenge = null;
            try
            {
                string resBody = this.HttpGet(register_url, paramDict);
                this.Gtlog($"RequestRegister(): 验证初始化, 与极验网络交互正常, 返回body={resBody}.");
                Dictionary<string, string> resDict = JsonSerializer.Deserialize<Dictionary<string, string>>(resBody);
                origin_challenge = resDict["challenge"];
            }
            catch (Exception e)
            {
                this.Gtlog("RequestRegister(): 验证初始化, 请求异常，后续流程走宕机模式, " + e);
                origin_challenge = "";
            }
            return origin_challenge;
        }

        // 构建验证初始化返回数据
        private void BuildRegisterResult(string origin_challenge, string digestmod)
        {
            // origin_challenge为空或者值为0代表失败
            if (string.IsNullOrWhiteSpace(origin_challenge) || "0".Equals(origin_challenge))
            {
                // 本地随机生成32位字符串
                string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
                StringBuilder randomStr = new StringBuilder();
                Random rd = new Random();
                for (int i = 0; i < 32; i++)
                {
                    randomStr.Append(characters[rd.Next(characters.Length)]);
                }
                string challenge = randomStr.ToString();
                var data = new { success = 0, gt = this.geetest_id, challenge = challenge, new_captcha = NEW_CAPTCHA };
                this.libResult.SetAll(0, JsonSerializer.Serialize(data), "请求极验register接口失败，后续流程走宕机模式");
            }
            else
            {
                string challenge = null;
                if ("md5".Equals(digestmod))
                {
                    challenge = this.Md5_encode(origin_challenge + this.geetest_key);
                }
                else if ("sha256".Equals(digestmod))
                {
                    challenge = this.Sha256_encode(origin_challenge + this.geetest_key);
                }
                else if ("hmac-sha256".Equals(digestmod))
                {
                    challenge = this.Hmac_sha256_encode(origin_challenge, this.geetest_key);
                }
                else
                {
                    challenge = this.Md5_encode(origin_challenge + this.geetest_key);
                }
                var data = new { success = 1, gt = this.geetest_id, challenge = challenge, new_captcha = NEW_CAPTCHA };
                this.libResult.SetAll(1, JsonSerializer.Serialize(data), "");
            }
        }

        // 正常流程下（即验证初始化成功），二次验证
        public GeetestLibResult SuccessValidate(string challenge, string validate, string seccode, IDictionary<string, string> paramDict)
        {
            this.Gtlog($"SuccessValidate(): 开始二次验证 正常模式, challenge={challenge}, validate={validate}, seccode={seccode}.");
            if (!this.CheckParam(challenge, validate, seccode))
            {
                this.libResult.SetAll(0, "", "正常模式，本地校验，参数challenge、validate、seccode不可为空");
            }
            else
            {
                string response_seccode = this.RequestValidate(challenge, validate, seccode, paramDict);
                if (string.IsNullOrWhiteSpace(response_seccode))
                {
                    this.libResult.SetAll(0, "", "请求极验validate接口失败");
                }
                else if ("false".Equals(response_seccode))
                {
                    this.libResult.SetAll(0, "", "极验二次验证不通过");
                }
                else
                {
                    this.libResult.SetAll(1, "", "");
                }
            }
            this.Gtlog($"SuccessValidate(): 二次验证 正常模式, lib包返回信息={this.libResult}.");
            return this.libResult;
        }

        // 异常流程下（即验证初始化失败，宕机模式），二次验证
        // 注意：由于是宕机模式，初衷是保证验证业务不会中断正常业务，所以此处只作简单的参数校验，可自行设计逻辑。
        public GeetestLibResult FailValidate(string challenge, string validate, string seccode)
        {
            this.Gtlog($"FailValidate(): 开始二次验证 宕机模式, challenge={challenge}, validate={validate}, seccode={seccode}.");
            if (!this.CheckParam(challenge, validate, seccode))
            {
                this.libResult.SetAll(0, "", "宕机模式，本地校验，参数challenge、validate、seccode不可为空.");
            }
            else
            {
                this.libResult.SetAll(1, "", "");
            }
            this.Gtlog($"FailValidate(): 二次验证 宕机模式, lib包返回信息={this.libResult}.");
            return this.libResult;
        }

        // 向极验发送二次验证的请求，POST方式
        private string RequestValidate(string challenge, string validate, string seccode, IDictionary<string, string> paramDict)
        {
            paramDict.Add("seccode", seccode);
            paramDict.Add("json_format", JSON_FORMAT);
            paramDict.Add("challenge", challenge);
            paramDict.Add("sdk", VERSION);
            paramDict.Add("captchaid", this.geetest_id);
            string validate_url = API_URL + VALIDATE_URL;
            this.Gtlog($"RequestValidate(): 二次验证 正常模式, 向极验发送请求, url={validate_url}, params={JsonSerializer.Serialize(paramDict)}.");
            string response_seccode = null;
            try
            {
                string resBody = this.HttpPost(validate_url, paramDict);
                this.Gtlog($"RequestValidate(): 二次验证 正常模式, 与极验网络交互正常, 返回body={resBody}.");
                Dictionary<string, string> resDict = JsonSerializer.Deserialize<Dictionary<string, string>>(resBody);
                response_seccode = resDict["seccode"];
            }
            catch (Exception e)
            {
                this.Gtlog("RequestValidate(): 二次验证 正常模式, 请求异常, " + e);
                response_seccode = "";
            }
            return response_seccode;
        }

        // 校验二次验证的三个参数，校验通过返回true，校验失败返回false
        private bool CheckParam(string challenge, string validate, string seccode)
        {
            return !(string.IsNullOrWhiteSpace(challenge) || string.IsNullOrWhiteSpace(validate) || string.IsNullOrWhiteSpace(seccode));
        }

        // 发送GET请求，获取服务器返回结果
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
                throw e;
            }
            finally
            {
                if (resStream != null)
                {
                    resStream.Close();
                }
            }
        }

        // 发送POST请求，获取服务器返回结果
        private string HttpPost(string url, IDictionary<string, string> paramDict)
        {
            Stream reqStream = null;
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
                byte[] bytes = Encoding.UTF8.GetBytes(paramStr.ToString().Substring(1));
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ReadWriteTimeout = HTTP_TIMEOUT_DEFAULT;
                req.Timeout = HTTP_TIMEOUT_DEFAULT;
                reqStream = req.GetRequestStream();
                reqStream.Write(bytes, 0, bytes.Length);
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                resStream = res.GetResponseStream();
                StreamReader reader = new StreamReader(resStream, Encoding.GetEncoding("utf-8"));
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (reqStream != null)
                {
                    reqStream.Close();
                }
                if (resStream != null)
                {
                    resStream.Close();
                }
            }
        }

        // md5 加密
        private string Md5_encode(string value)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // sha256加密
        public string Sha256_encode(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // hmac-sha256 加密
        private string Hmac_sha256_encode(string value, string key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] data = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

    }
}

