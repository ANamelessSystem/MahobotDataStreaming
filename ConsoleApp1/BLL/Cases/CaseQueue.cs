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
    class CaseQueue : GroupMsgBLL
    {
        /// <summary>
        /// 加入队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueAdd(string strGrpID, string strUserID, string strUserGrpCard)
        {
            int intMemberStatus = QueueDAL.MemberCheck(strGrpID, strUserID);
            if (intMemberStatus == 0)
            {
                MsgMessage += new Message("尚未报名，无法加入队列。\r\n");
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
            if (QueueDAL.AddQueue(strGrpID, strUserID, strUserGrpCard))
            {
                MsgMessage += new Message("已加入队列\r\n--------------------\r\n");
                QueueShow(strGrpID, strUserID);
            }
            else
            {
                Console.WriteLine("与数据库失去连接，加入队列失败。\r\n");
                MsgMessage += new Message("与数据库失去连接，加入队列失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }

        /// <summary>
        /// 展示队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueShow(string strGrpID, string strUserID)
        {
            if (GroupProperties.IsHpShow)
            {
                //Console.WriteLine("查询HP前的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                HpShowAndSubsCheck(strGrpID);
                //Console.WriteLine("查询HP后的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            }
            else
            {
                Console.WriteLine("未打开HP计算功能。");
            }
            if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue))
            {
                if (dtQueue.Rows.Count > 0)
                {
                    MsgMessage += new Message("目前队列：\r\n");
                    //Console.WriteLine("队列查询循环开始的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                    for (int i = 0; i < dtQueue.Rows.Count; i++)
                    {
                        string strOutput = "顺序：" + dtQueue.Rows[i]["seq"].ToString() + "    " + dtQueue.Rows[i]["name"].ToString() + "(" + dtQueue.Rows[i]["id"].ToString() + ")";
                        MsgMessage += new Message(strOutput + "\r\n");
                        Console.WriteLine(strOutput);
                    }
                    //Console.WriteLine("队列查询循环结束后的信息：\r\n" + MsgMessage.Raw.ToString()+"(信息结束)");
                }
                else
                {
                    Console.WriteLine("队列中无人");
                    MsgMessage += new Message("目前队列中无人。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
            }
            //Console.WriteLine("发送最终结果前的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //Console.WriteLine("发送最终结果后的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            //throw new Exception("调试，强制中断程序");
        }

        /// <summary>
        /// 退出队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueQuit(string strGrpID, string strUserID)
        {
            if (QueueDAL.QuitQueue(strGrpID, strUserID, out int deletedCount))
            {
                if (deletedCount > 0)
                {
                    Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀移出队列。");
                    MsgMessage += new Message("已将较早一次队列记录退出。\r\n--------------------\r\n");
                }
                else
                {
                    Console.WriteLine("群：" + strGrpID + "，" + strUserID + "移出队列失败：未找到记录。");
                    MsgMessage += new Message("未找到队列记录，这可能是一次未排刀的伤害上报。\r\n--------------------\r\n");
                }
                //Console.WriteLine("展示队列前的信息输出：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                QueueShow(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退出队列失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueClear(string strGrpID, string strUserID, GroupMemberInfo memberInfo)
        {
            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue_old))
                {
                    if (dtQueue_old.Rows.Count > 0)
                    {
                        if (QueueDAL.ClearQueue(strGrpID, out int deletedCount))
                        {
                            MsgMessage += new Message("已清空队列。\r\n--------------------\r\n");
                            Console.WriteLine("执行清空队列指令成功，共有" + deletedCount + "条记录受到影响");
                            MsgMessage += new Message("由于队列被清空，请以下成员重新排队：");
                            for (int i = 0; i < dtQueue_old.Rows.Count; i++)
                            {
                                string strUID = dtQueue_old.Rows[i]["id"].ToString();
                                MsgMessage += new Message("\r\nID：" + strUID + "， ") + Message.At(long.Parse(strUID));
                            }
                        }
                        else
                        {
                            Console.WriteLine("与数据库失去连接，清空队列失败。");
                            MsgMessage += new Message("与数据库失去连接，清空队列失败。\r\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("执行清空队列指令失败，队列中无人。");
                        MsgMessage += new Message("队列中无人，不需要清空。\r\n");
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
                }
            }
            else
            {
                Console.WriteLine("执行清空队列指令失败，由权限不足的人发起");
                MsgMessage += new Message("拒绝：仅有管理员或群主可执行队列清空指令。\r\n");
            }
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 将一个队列记录修改为等待救援
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        public static void QueueSos(string strGrpID, string strUserID,string strCmdContext)
        {
            //bool isCorrect = true;
            //分拆命令
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
            if (CommonVariables.IntRound == -1)
            {
                //备用
            }
            if (QueueDAL.SosQueue(strGrpID, strUserID, CommonVariables.IntBossCode, CommonVariables.IntRound, out int updCount))
            {
                if (updCount > 0)
                {
                    Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀置为等待救援状态。");
                    MsgMessage += new Message("已将较早一次队列记录置为等待救援状态。\r\n--------------------\r\n");
                }
                else
                {
                    Console.WriteLine("群：" + strGrpID + "，" + strUserID + "修改队列状态失败：未找到记录。");
                    MsgMessage += new Message("未找到队列记录，请先进入队列再改为等待救援状态。\r\n--------------------\r\n");
                }
                QueueShow(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，修改队列状态失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }


        /// <summary>
        /// 显示血量并检查提醒表
        /// </summary>
        /// <param name="strGrpID"></param>
        public static void HpShowAndSubsCheck(string strGrpID)
        {
            if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
            {
                if (dtBossProgress != null && dtBossProgress.Rows.Count > 0)
                {
                    if (dtBossProgress.Rows[0][0] is DBNull)//判断数据库中字段是否为Null
                    {
                        return;
                    }
                }
                try
                {
                    string strHpRemain = dtBossProgress.Rows[0]["hpremain"].ToString();
                    string strOutput2 = "";
                    if (strHpRemain.Length > 4 && !strHpRemain.Contains("-"))
                    {
                        strOutput2 = "目前进度：" + dtBossProgress.Rows[0]["maxround"].ToString() + "周目，B" + dtBossProgress.Rows[0]["maxbc"].ToString() + "，剩余血量(推测)=" + strHpRemain.Substring(0, strHpRemain.Length - 4) + "万";
                        MsgMessage += new Message(strOutput2 + "\r\n--------------------\r\n");
                    }
                    else if (strHpRemain.Length > 0)
                    {
                        strOutput2 = "目前进度：" + dtBossProgress.Rows[0]["maxround"].ToString() + "周目，B" + dtBossProgress.Rows[0]["maxbc"].ToString() + "，剩余血量(推测)=" + strHpRemain;
                        MsgMessage += new Message(strOutput2 + "\r\n--------------------\r\n");
                    }
                    Console.WriteLine(strOutput2);
                    //订阅提醒功能
                    int intHpNow = int.Parse(strHpRemain);
                    int intRoundNow = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                    int intBCNow = int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
                    int intProgType;
                    if (intHpNow > 3000000)
                    {
                        //提醒到订阅类型0
                        intProgType = 0;
                    }
                    else if (intHpNow > 1000000)
                    {
                        //提醒到订阅类型1
                        intProgType = 1;
                    }
                    else
                    {
                        //提醒到下一个的订阅类型0
                        intProgType = 2;
                    }
                    if (SubscribeDAL.BossReminder(strGrpID, intRoundNow, intBCNow, intProgType, out DataTable dtSubsMembers))
                    {
                        if (dtSubsMembers.Rows.Count > 0)
                        {
                            string strRemindContext = "[公会战进度提醒]\r\n您所在群："+strGrpID+"，BOSS进度已到B"+intBCNow+"，目前血量："+strHpRemain+"\r\n如时间方便，请做好本战准备。";
                            if (strHpRemain.Length > 4 && !strHpRemain.Contains("-"))
                            {
                                strRemindContext = "[公会战进度提醒]\r\n您所在群：" + strGrpID + "，BOSS进度已到B" + intBCNow + "，目前血量：" + strHpRemain.Substring(0, strHpRemain.Length - 4) + "万\r\n如时间方便，请做好本战准备。";
                            }
                            else if (strHpRemain.Length > 0)
                            {
                                strRemindContext = "[公会战进度提醒]\r\n您所在群：" + strGrpID + "，BOSS进度已到B" + intBCNow + "，目前血量：" + strHpRemain + "\r\n如时间方便，请做好本战准备。";
                            }
                            for (int i = 0; i < dtSubsMembers.Rows.Count; i++)
                            {
                                long lUserID = long.Parse(dtSubsMembers.Rows[i]["USERID"].ToString());
                                ApiProperties.HttpApi.SendPrivateMessageAsync(lUserID, strRemindContext);
                                Console.WriteLine("已私聊通知" + lUserID.ToString() + "(" + strGrpID + ")");
                                SubscribeDAL.UpdateRemindFlag(strGrpID, lUserID.ToString(), intRoundNow, intBCNow, intProgType);
                                Console.WriteLine("已更新通知状态" + lUserID.ToString() + "(" + strGrpID + ")");
                            }
                        }
                        else
                        {
                            //无人订阅
                        }
                    }
                    else
                    {
                        Console.WriteLine("提醒查询失败");
                    }
                }
                catch (Exception ex)
                {
                    MsgMessage += new Message("遇到未知错误，查询剩余HP失败。\r\n");
                    Console.WriteLine(ex);
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询剩余HP失败。\r\n");
                Console.WriteLine("与数据库失去连接，查询剩余HP失败。\r\n");
            }
        }
    }
}
