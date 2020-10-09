using System;

namespace Gt3_server_csharp_aspnetcoremvc_bypass.Controllers
{
    public class GeetestConfig
    {
        public const string GEETEST_ID = "c9c4facd1a6feeb80802222cbb74ca8e";
        public const string GEETEST_KEY = "e4e298788aa8c768397639deb9b249a9";
        public const string REDIS_SERVER = "192.168.1.156:6379";
        public const string BYPASS_URL = "http://bypass.geetest.com/v1/bypass_status.php";
        public const int CYCLE_TIME = 10000;
        public const string GEETEST_BYPASS_STATUS_KEY = "gt_server_bypass_status";
    }
}
