using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Mirai_CSharp.Models;


namespace Marchen.BLL
{
    class CaseSubscribe : GroupMsgBLL
    {
        /// <summary>
        /// 订阅Boss提醒（普通订阅）（用户调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">用户输入的命令内容</param>
        public static void SubsAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            IMessageBase[] chain;
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);
            if (intMemberStatus == 0)
            {
                MsgMessage += "尚未报名，订阅BOSS失败。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            else if (intMemberStatus == -1)
            {
                MsgMessage += "与数据库失去连接，订阅BOSS失败。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += "未能找到BOSS编号，订阅BOSS失败。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (InputVariables.IntEXT == -1)
            {
                InputVariables.IntEXT = 0;
            }
            int intSubsType = 0;
            if (InputVariables.IntEXT != 0)
            {
                intSubsType = 1;
            }
            if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
            {
                DataRow[] drExistsSubs = dtSubsStatus.Select("BC='" + InputVariables.IntBossCode + "'");
                if (drExistsSubs.Length == 0)
                {
                    if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, InputVariables.IntBossCode, intSubsType))
                    {
                        if (intSubsType == 1)
                        {
                            MsgMessage += "已新增B" + InputVariables.IntBossCode + "的订阅，类型：补时。\r\n";
                        }
                        else
                        {
                            MsgMessage += "已新增B" + InputVariables.IntBossCode + "的订阅，类型：通常。\r\n";
                        }
                    }
                    else
                    {
                        MsgMessage += "与数据库失去连接，订阅BOSS失败。\r\n";
                    }
                }
                else
                {
                    if (SubscribeDAL.UpdateSubsType(strGrpID, strUserID, 0, InputVariables.IntBossCode, intSubsType))
                    {
                        if (intSubsType == 1)
                        {
                            MsgMessage += "已将B" + InputVariables.IntBossCode + "的订阅类型修改为：补时。\r\n";
                        }
                        else
                        {
                            MsgMessage += "已将B" + InputVariables.IntBossCode + "的订阅类型修改为：通常。\r\n";
                        }
                    }
                    else
                    {
                        MsgMessage += "与数据库失去连接，订阅BOSS失败。\r\n";
                    }
                }
            }
            else
            {
                MsgMessage += "与数据库失去连接，订阅BOSS失败。\r\n";
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }

        /// <summary>
        /// 查看已订阅的BOSS
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        public static void SubsShow(string strGrpID, string strUserID, string strCmdContext)
        {
            IMessageBase[] chain;
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                //MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (InputVariables.IntIsAllFlag == 0)
            {
                if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
                {
                    string strOutput = "";
                    for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                    {
                        if (i != 0)
                        {
                            strOutput += "、";
                        }
                        strOutput += "B" + dtSubsStatus.Rows[i]["BC"];
                        if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                        {
                            strOutput += "(补时)";
                        }
                    }
                    if (strOutput != "")
                    {
                        MsgMessage += "目前正在订阅的BOSS为：" + strOutput + "\r\n";
                    }
                    else
                    {
                        MsgMessage += "尚无订阅记录\r\n";
                    }
                }
                else
                {
                    MsgMessage += "与数据库失去连接，查看已订阅BOSS失败。\r\n";
                }
            }
            else
            {
                if (SubscribeDAL.GetSubsStatus(strGrpID, out DataTable dtSubsStatus))
                {
                    string strOutputB1 = "";
                    string strOutputB2 = "";
                    string strOutputB3 = "";
                    string strOutputB4 = "";
                    string strOutputB5 = "";
                    int intCountB1 = 0;
                    int intCountB2 = 0;
                    int intCountB3 = 0;
                    int intCountB4 = 0;
                    int intCountB5 = 0;
                    for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                    {
                        string strExt = "";
                        if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                        {
                            strExt = " 【补时】";
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "1")
                        {
                            intCountB1 += 1;
                            strOutputB1 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")" + strExt;
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "2")
                        {
                            intCountB2 += 1;
                            strOutputB2 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")" + strExt;
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "3")
                        {
                            intCountB3 += 1;
                            strOutputB3 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")" + strExt;
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "4")
                        {
                            intCountB4 += 1;
                            strOutputB4 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")" + strExt;
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "5")
                        {
                            intCountB5 += 1;
                            strOutputB5 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")" + strExt;
                        }
                    }
                    if (strOutputB1 == "" || strOutputB1 == null)
                    {
                        strOutputB1 = "\r\n无";
                    }
                    if (strOutputB2 == "" || strOutputB2 == null)
                    {
                        strOutputB2 = "\r\n无";
                    }
                    if (strOutputB3 == "" || strOutputB3 == null)
                    {
                        strOutputB3 = "\r\n无";
                    }
                    if (strOutputB4 == "" || strOutputB4 == null)
                    {
                        strOutputB4 = "\r\n无";
                    }
                    if (strOutputB5 == "" || strOutputB5 == null)
                    {
                        strOutputB5 = "\r\n无";
                    }
                    //MsgMessage += new Message("B1订阅人数(" + intCountB1 + "人)：" + strOutputB1 + "\r\n---------------------\r\nB2订阅人数(" + intCountB2 + "人)：" + strOutputB2 + "\r\n---------------------\r\nB3订阅人数(" + intCountB3 + "人)：" + strOutputB3 + "\r\n---------------------\r\nB4订阅人数(" + intCountB4 + "人)：" + strOutputB4 + "\r\n---------------------\r\nB5订阅人数(" + intCountB5 + "人)：" + strOutputB5 + "\r\n");
                    MsgMessage = "B1订阅人数(" + intCountB1 + "人)：" + strOutputB1 + "\r\n---------------------";
                    chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                    ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                    MsgMessage = "B2订阅人数(" + intCountB2 + "人)：" + strOutputB2 + "\r\n---------------------";
                    chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                    ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                    MsgMessage = "B3订阅人数(" + intCountB3 + "人)：" + strOutputB3 + "\r\n---------------------";
                    chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                    ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                    MsgMessage = "B4订阅人数(" + intCountB4 + "人)：" + strOutputB4 + "\r\n---------------------";
                    chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                    ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                    MsgMessage = "B5订阅人数(" + intCountB5 + "人)：" + strOutputB5 + "\r\n---------------------\r\n";
                }
                else
                {
                    MsgMessage += "与数据库失去连接，查看已订阅BOSS失败。\r\n";
                }
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }


        /// <summary>
        /// 取消boss订阅（用户调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">用户输入的命令内容</param>
        public static void SubsDel(string strGrpID, string strUserID, string strCmdContext)
        {
            IMessageBase[] chain;
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += "输入【@MahoBot help】获取帮助。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += "未能找到BOSS编号。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (SubscribeDAL.DelBossSubs(strGrpID, strUserID, InputVariables.IntBossCode, out int intDelCount))
            {
                if (intDelCount > 0)
                {
                    MsgMessage += "退订B"+ InputVariables.IntBossCode + "成功。\r\n";
                }
                else
                {
                    MsgMessage += "尚未订阅该BOSS。\r\n";
                }
            }
            else
            {
                MsgMessage += "与数据库失去连接，退订失败。\r\n";
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }

        /// <summary>
        /// 取消boss订阅（自动调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intBossCode">BOSS代码</param>
        public static void SubsDel(string strGrpID, string strUserID, int intBossCode)
        {
            if (SubscribeDAL.DelBossSubs(strGrpID, strUserID, intBossCode, out int intDelCount))
            {
                if (intDelCount > 0)
                {
                    MsgMessage += "【已自动退订B" + intBossCode + "。】\r\n";
                }
            }
            else
            {
                MsgMessage += "与数据库失去连接，退订失败。\r\n";
            }
        }
    }
}
