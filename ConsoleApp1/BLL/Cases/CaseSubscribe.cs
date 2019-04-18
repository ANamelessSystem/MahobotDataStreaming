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
        /// 订阅Boss提醒
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intBossCode"></param>
        public static void SubsAdd(string strGrpID, string strUserID, string strCmdContext)
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
            if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
            {
                DataRow[] drExistsBoss = dtSubsStatus.Select("BC=" + CommonVariables.IntBossCode);
                if (drExistsBoss.Length == 0)
                {
                    if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, CommonVariables.IntBossCode))
                    {
                        string strOutput = "B" + CommonVariables.IntBossCode.ToString() +"(new)";
                        for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                        {
                            strOutput += "、B" + dtSubsStatus.Rows[i]["BC"];
                        }
                        MsgMessage += new Message("已成功订阅BOSS。\r\n目前正在订阅的BOSS为：" + strOutput + "\r\n");
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
        /// 查看已订阅的BOSS
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
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
                }
                MsgMessage += new Message("已成功订阅BOSS。\r\n目前正在订阅的BOSS为：" + strOutput + "\r\n");
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查看已订阅BOSS失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }


        /// <summary>
        /// 取消boss订阅
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
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
        //    if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
        //    {
        //        DataRow[] drExistsBoss = dtSubsStatus.Select("BC=" + CommonVariables.IntBossCode);
        //        if (drExistsBoss.Length == 0)
        //        {
        //            if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, CommonVariables.IntBossCode))
        //            {
        //                string strOutput = "B" + CommonVariables.IntBossCode.ToString() + "(new)";
        //                for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
        //                {
        //                    strOutput += "、B" + dtSubsStatus.Rows[i]["BC"];
        //                }
        //                MsgMessage += new Message("已成功订阅BOSS。\r\n目前正在订阅的BOSS为：" + strOutput + "\r\n");
        //            }
        //            else
        //            {
        //                MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
        //            }
        //        }
        //        else
        //        {
        //            MsgMessage += new Message("已订阅过本BOSS，无法重复订阅。\r\n");
        //        }
        //    }
        //    else
        //    {
        //        MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
        //    }
        //    MsgMessage += Message.At(long.Parse(strUserID));
        //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }
    }
}
