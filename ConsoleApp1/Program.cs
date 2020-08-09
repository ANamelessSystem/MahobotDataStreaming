using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Mirai_CSharp.Example;
using Marchen.Model;
using Marchen.BLL;


namespace Marchen.Garden
{
    class Program
    {
        public static async Task Main()
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
            if (!CmdHelper.LoadValueLimits())
            {
                Console.WriteLine("警告：无法读取上限值设置！");
            }
            ValueLimits.DamageLimitMax = 0;
            ValueLimits.RoundLimitMax = 0;

            #region Mirai Listening Setup
            MiraiHttpSessionOptions options = new MiraiHttpSessionOptions(ApiProperties.HttpApiIP, ApiProperties.HttpApiPort, ApiProperties.HttpApiAuthKey);
            await using MiraiHttpSession session = new MiraiHttpSession();
            ExamplePlugin plugin = new ExamplePlugin();
            session.AddPlugin(plugin);
            await session.ConnectAsync(options, long.Parse(SelfProperties.SelfID));
            ApiProperties.session = session;
            Console.ReadKey();
            #endregion

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
            uint mode;
            GetConsoleMode(hStdin, out mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
            mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
            SetConsoleMode(hStdin, mode);
        }
        #endregion
    }
}
