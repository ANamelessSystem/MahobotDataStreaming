using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using Sisters.WudiLib.Responses;


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
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);
            if (intMemberStatus == 0)
            {
                MsgMessage += new Message("尚未报名，订阅BOSS。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else if (intMemberStatus == -1)
            {
                MsgMessage += new Message("与数据库失去连接，查询名单失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (CommonVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("未能找到BOSS编号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
            {
                DataRow[] drExistsBoss = dtSubsStatus.Select("BC='" + CommonVariables.IntBossCode + "'");
                if (drExistsBoss.Length == 0)
                {
                    if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, CommonVariables.IntBossCode, 0))
                    {
                        string strOutput = "B" + CommonVariables.IntBossCode.ToString();
                        strOutput += "(new)";
                        for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                        {
                            strOutput += "、B" + dtSubsStatus.Rows[i]["BC"];
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutput += "(补时)";
                            }
                        }
                        MsgMessage += new Message("已成功订阅。\r\n目前正在订阅的BOSS为：" + strOutput + "\r\n");
                    }
                    else
                    {
                        MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
                    }
                }
                else
                {
                    MsgMessage += new Message("已订阅过本BOSS，无法重复订阅。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 订阅Boss提醒（尾刀专用）（自动调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intBossCode">BOSS代码</param>
        public static void SubsAdd(string strGrpID, string strUserID, int intBossCode)
        {
            if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
            {
                DataRow[] drExistsBoss = dtSubsStatus.Select("BC='" + intBossCode + "'");
                if (drExistsBoss.Length == 0)
                {
                    if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, intBossCode, 1))
                    {
                        //show success
                    }
                    else
                    {
                        //database connect failed
                    }
                }
                else
                {
                    //boss already subs
                    //just change substype
                }
            }
            else
            {
                //database connect failed
            }
            //at message
            //message output
        }

        /// <summary>
        /// 查看已订阅的BOSS
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        public static void SubsShow(string strGrpID, string strUserID)
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
                MsgMessage += new Message("目前正在订阅的BOSS为：" + strOutput + "\r\n");
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查看已订阅BOSS失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }


        /// <summary>
        /// 取消boss订阅（用户调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">用户输入的命令内容</param>
        public static void SubsDel(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (CommonVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("未能找到BOSS编号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (SubscribeDAL.DelBossSubs(strGrpID, strUserID, CommonVariables.IntBossCode, out int intDelCount))
            {
                if (intDelCount > 0)
                {
                    MsgMessage += new Message("退订B"+ CommonVariables.IntBossCode + "成功。\r\n");
                }
                else
                {
                    MsgMessage += new Message("尚未订阅该BOSS。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退订失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
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
                    MsgMessage += new Message("【已自动退订B" + intBossCode + "。】\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退订失败。\r\n");
            }
        }
    }
}
