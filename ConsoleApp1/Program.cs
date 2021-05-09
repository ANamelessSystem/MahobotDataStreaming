using System;
using System.Globalization;
using Sisters.WudiLib;
using Sisters.WudiLib.Posts;
using Sisters.WudiLib.Responses;
using Marchen.Model;
using Marchen.BLL;
using System.Timers;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace Marchen.Garden
{
    class Program
    {
        static void Main(string[] args)
        {
            var culture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            DisbleQuickEditMode();




            if (!CfgLoader.LoadConfigFile())
            {
                Console.WriteLine("请按任意键退出，并在编辑配置文件完成后重启。");
                Console.ReadKey();
                return;
            }
            ApiProperties.HttpApi = new HttpApiClient();
            ApiProperties.HttpApi.ApiAddress = ApiProperties.ApiAddr;
            try
            {
                SelfProperties.SelfID = ApiProperties.HttpApi.GetLoginInfoAsync().Result.UserId.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("获取自己的ID时出现错误，请确认酷Q的运行情况与HTTP API插件的安装。");
                Console.WriteLine("点击任意键退出");
                Console.ReadKey();
                return;
            }
            ApiPostListener postListener = new ApiPostListener
            {
                ApiClient = ApiProperties.HttpApi,
                PostAddress = ApiProperties.ApiPostAddr,
                ForwardTo = ApiProperties.ApiForwardToAddr
            };
            try
            {
                postListener.StartListen();
            }
            catch (System.Net.HttpListenerException hlex)
            {
                Console.WriteLine(hlex);
                Console.WriteLine("无法打开端口，使用netsh命令添加监听地址为URL保留项后重启本程序");
                Console.WriteLine("点击任意键退出");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("监听启动成功");
            
            
            ValueLimits.DamageLimitMax = 0;
            ValueLimits.RoundLimitMax = 0;
            if (!CmdHelper.LoadValueLimits())
            {
                Console.WriteLine("警告：无法读取上限值设置！");
            }
            //Timer timer = new Timer();
            //timer.Enabled = true;
            //timer.Interval = 1800000;
            //timer.Start();
            //timer.Elapsed += new ElapsedEventHandler(Reminder);
            postListener.MessageEvent += (api, message) =>
            {
                if (message.Endpoint is GroupEndpoint)
                {
                    //处理群消息
                    GroupMemberInfo memberInfo = api.GetGroupMemberInfoAsync(long.Parse(message.GetType().GetProperty("GroupId").GetValue(message, null).ToString()), message.UserId).Result;
                    if (EnvSettings.TestMode == "1" && memberInfo.GroupId.ToString() != EnvSettings.TestGrpID)
                    {
                        return;
                    }
                    GroupMsgBLL.GrpMsgReco(message, memberInfo);
                }
                else if (message.Endpoint is DiscussEndpoint)
                {
                    //处理讨论组消息
                }
                else if (message.Endpoint is PrivateEndpoint)
                {
                    //处理私聊消息
                    //2019.05.15
                    //PrivateMsgBLL.PriMsgReco(message);
                }
                else
                {
                    //其他
                }
            };
            Console.ReadKey();
        }

        #region 关闭控制台 快速编辑模式、插入模式
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_INSERT_MODE = 0x0020;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int hConsoleHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        public static void DisbleQuickEditMode()
        {
            IntPtr hStdin = GetStdHandle(STD_INPUT_HANDLE);
            GetConsoleMode(hStdin, out uint mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
            mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
            SetConsoleMode(hStdin, mode);
        }
        #endregion
    }
}
