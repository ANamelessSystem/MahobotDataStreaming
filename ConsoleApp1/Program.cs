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
        //static void SetDatabaseInfo()
        //{
        //    DBProperties.DBUserID = "MIRACLEMAHO";
        //    DBProperties.DBPassword = "pupupu";
        //    DBProperties.DBAddress = "192.168.29.12";
        //    DBProperties.DBPort = "1521";
        //    DBProperties.DBServiceName = "MAHOMAHO";
        //    DBProperties.DBCreaGDTProcName = "CreaGrpDmgTab";
        //}
        //static void SetHttpApiInfo()
        //{
        //    //ApiProperties.PostAddress = "http://[::1]:10202";
        //    ApiProperties.ApiPostAddr = "http://+:8876/";//监听地址（接收酷Q HTTPAPI上报信息的地址）
        //    ApiProperties.ApiAddr = "http://127.0.0.1:5700/";//上报地址
        //    ApiProperties.HttpApi = new HttpApiClient();
        //    ApiProperties.HttpApi.ApiAddress = ApiProperties.ApiAddr;
        //    ApiProperties.ApiForwardToAddr = "http://[::1]:10202";
        //}

        static void Main(string[] args)
        {
            var culture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            //SetDatabaseInfo();
            //SetHttpApiInfo();
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
            
            #region 控制台锁定
            //Console.Title = ApiProperties.HttpApi.GetLoginInfoAsync().Result.Nickname.ToString(); 
            DisbleQuickEditMode();
            //DisbleClosebtn();
            //Console.CancelKeyPress += new ConsoleCancelEventHandler(CloseConsole);
            #endregion
            ValueLimits.DamageLimitMax = 0;
            ValueLimits.RoundLimitMax = 0;
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
        //private static void MsgRecognization(MessageContext receivedMessage)
        //{
        //    MsgRecog.
        //}
        //private static void Reminder(object source, ElapsedEventArgs e)
        //{
        //    DataTable dtResultTime = DBHelper.GetDataTable("select sysdate from dual");
        //    DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
        //    if (dtNow.Hour >= 0 && dtNow.Hour < 4)
        //    {
        //        var message = new Message("");
        //        DateTime dtStart = GetZeroTime(dtNow.AddDays(-1)).AddHours(4);
        //        DateTime dtEnd = GetZeroTime(dtNow).AddHours(4);
        //        string searchWhoLost = "select id from ML_720671752 where id not in (select userid from GD_720671752 where time between to_date('" + dtStart + "', 'yyyy/mm/dd hh24:mi:ss') and to_date('" + dtEnd + "','yyyy/mm/dd hh24:mi:ss'))";
        //        DataTable dtResultLostMember = DBHelper.GetDataTable(searchWhoLost);
        //        for (int i = 0; i < dtResultLostMember.Rows.Count; i++)
        //        {
        //            Console.WriteLine("一刀未出：" + dtResultLostMember.Rows[i]["id"].ToString());
        //            message += Message.At(long.Parse(dtResultLostMember.Rows[i]["id"].ToString()));
        //            message += new Message("统计显示你尚未出刀\r\n");
        //        }
        //        string searchWhoLeft = "select userid from ( select userid,count(userid) as c1 from GD_720671752 where time  between to_date('" + dtStart + "', 'yyyy/mm/dd hh24:mi:ss') and to_date('" + dtEnd + "','yyyy/mm/dd hh24:mi:ss') group by userid) where c1 < 3";
        //        DataTable dtResultLeftMember = DBHelper.GetDataTable(searchWhoLeft);
        //        for (int i = 0; i < dtResultLeftMember.Rows.Count; i++)
        //        {
        //            Console.WriteLine("还有刀没出：" + dtResultLeftMember.Rows[i]["userid"].ToString());
        //            message += Message.At(long.Parse(dtResultLeftMember.Rows[i]["userid"].ToString()));
        //            message += new Message("统计显示你们还有余刀\r\n");
        //        }
        //        SelfProperties.HttpApi.SendGroupMessageAsync(long.Parse(vfGroupID), message).Wait();
        //    }
        //}

        //private static DateTime GetZeroTime(DateTime datetime)
        //{
        //    return new DateTime(datetime.Year, datetime.Month, datetime.Day);
        //}
        #region 设置控制台标题 禁用关闭按钮

        //[DllImport("user32.dll", EntryPoint = "FindWindow")]
        //extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        //[DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        //extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        //[DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        //extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        //static void DisbleClosebtn()
        //{
        //    IntPtr windowHandle = FindWindow(null, "控制台标题");
        //    IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
        //    uint SC_CLOSE = 0xF060;
        //    RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        //}
        //protected static void CloseConsole(object sender, ConsoleCancelEventArgs e)
        //{
        //    Environment.Exit(0);
        //}
        #endregion

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
