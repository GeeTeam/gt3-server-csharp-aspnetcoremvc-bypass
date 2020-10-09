using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gt3_server_csharp_aspnetcoremvc_bypass.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gt3_server_csharp_aspnetcoremvc_bypass
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.QueueUserWorkItem(ThreadWork);
            CreateHostBuilder(args).Build().Run();
        }

        public static void ThreadWork(object ob)
        {
            GeetestController Controller = new GeetestController();
            Controller.CheckStatus();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
