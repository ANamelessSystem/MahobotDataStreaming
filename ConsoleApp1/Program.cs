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
        static void SetDatabaseInfo()
        {
            DBProperties.DBUserID = "MIRACLEMAHO";
            DBProperties.DBPassword = "pupupu";
            DBProperties.DBAddress = "192.168.29.12";
            DBProperties.DBPort = "1521";
            DBProperties.DBServiceName = "MAHOMAHO";
        }
        static void SetHttpApiInfo()
        {
            //ApiProperties.PostAddress = "http://[::1]:10202";
            ApiProperties.ApiPostAddr = "http://+:8876/";//监听地址（接收酷Q HTTPAPI上报信息的地址）
            ApiProperties.ApiAddr = "http://127.0.0.1:5700/";//上报地址
            ApiProperties.HttpApi = new HttpApiClient();
            ApiProperties.HttpApi.ApiAddress = ApiProperties.ApiAddr;
            ApiProperties.ApiForwardToAddr = "http://[::1]:10202";
        }

        static void Main(string[] args)
        {
            var culture = CultureInfo.GetCultureInfo("zh-CN");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            SetDatabaseInfo();
            SetHttpApiInfo();
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
                    GroupMsgBLL.GrpMsgReco(message,memberInfo);
                }
                else if (message.Endpoint is DiscussEndpoint)
                {
                    //处理讨论组消息
                }
                else if (message.Endpoint is PrivateEndpoint)
                {
                    //处理私聊消息
                }
                else
                {
                    //其他
                }
            };
            Console.ReadKey();
        }
        //static string vfGroupID = "720671752";//设置群号//maho群
        //static string vfGroupID = "877184755";//测试群
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


    }
}
