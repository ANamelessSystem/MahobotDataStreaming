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
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);
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
            if (QueueDAL.AddQueue(strGrpID, strUserID))
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
                HpShowAndSubsCheck(strGrpID, strUserID);
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
                    for (int i = 0; i < dtQueue.Rows.Count; i++)
                    {
                        string strOutput = "";
                        if (dtQueue.Rows[i]["sosflag"].ToString() == "1")
                        {
                            strOutput = "【等救】" + dtQueue.Rows[i]["MBRNAME"].ToString() + "(" + dtQueue.Rows[i]["ID"].ToString() + ")    【挂于B" + dtQueue.Rows[i]["BC"].ToString() + "(周目" + dtQueue.Rows[i]["ROUND"].ToString() + ")】";
                        }
                        else
                        {
                            strOutput = "【" + dtQueue.Rows[i]["SEQ"].ToString() + "】" + dtQueue.Rows[i]["MBRNAME"].ToString() + "(" + dtQueue.Rows[i]["ID"].ToString() + ")";
                        }
                        MsgMessage += new Message(strOutput + "\r\n");
                        Console.WriteLine(strOutput);
                    }
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
                MsgMessage += Message.At(long.Parse(strUserID));
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 退出队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intType">0：直接收到C3命令；1：来自其他方法的调用</param>
        public static void QueueQuit(string strGrpID, string strUserID, int intType)
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
                    if (intType == 0)
                    {
                        MsgMessage += new Message("未找到队列记录。\r\n--------------------\r\n");
                    }
                    if (intType == 1)
                    {
                        MsgMessage += new Message("未找到队列记录，这可能是一次未排刀的伤害上报。\r\n--------------------\r\n");
                    }
                }
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
                if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
                {
                    CommonVariables.IntRound = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                    if (CommonVariables.IntBossCode < int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) && int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) == 5)
                    {
                        CommonVariables.IntRound += 1;//BOSS显示进度还在B5，挂树着已经到了下B1或B2的情况
                    }
                }
            }
            if (QueueDAL.UpdateQueueToSos(strGrpID, strUserID, CommonVariables.IntBossCode, CommonVariables.IntRound, out int updCount))
            {
                if (updCount > 0)
                {
                    Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀置为等待救援状态。（B" + CommonVariables.IntBossCode + "，" + CommonVariables.IntRound + "周目）");
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
        /// 显示血量并检查是否有预定列表
        /// </summary>
        /// <param name="strGrpID">群号</param>
        public static void HpShowAndSubsCheck(string strGrpID,string strInputUserID = "0")
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
                //string strHpRemain = dtBossProgress.Rows[0]["hpremain"].ToString();
                int intHPNow = int.Parse(dtBossProgress.Rows[0]["hpremain"].ToString());
                int intRoundNow = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                int intBCNow = int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
                string strFormatedHP = "";
                try
                {
                    string strOutput2 = "";
                    if (intHPNow > 9999)
                    {
                        //5位正数以上自动转换为以万为单位优化显示
                        strFormatedHP = intHPNow.ToString().Substring(0, intHPNow.ToString().Length - 4) + "万";
                    }
                    else if (intHPNow <= 9999 && intHPNow >= -9999)
                    {
                        //正负4位数（小误差），自动跳到下个BOSS
                        if (intBCNow == ValueLimits.BossLimitMax)
                        {
                            //现在为B5，需要跳到下周目B1的情况
                            if (StatisticsDAL.GetBossMaxHP(1, intRoundNow + 1, out DataTable dtBossMaxHP))
                            {
                                Console.WriteLine("误差内跳到下个BOSS");
                                intBCNow = 1;
                                intRoundNow += 1;
                                intHPNow = int.Parse(dtBossMaxHP.Rows[0]["HP"].ToString());
                                strFormatedHP = intHPNow.ToString().Substring(0, intHPNow.ToString().Length - 4) + "万";
                            }
                            else
                            {
                                strFormatedHP = intHPNow.ToString();
                            }
                        }
                        else
                        {
                            if (StatisticsDAL.GetBossMaxHP(intBCNow + 1, intRoundNow, out DataTable dtBossMaxHP))
                            {
                                Console.WriteLine("误差内跳到下个BOSS");
                                intBCNow += 1;
                                intHPNow = int.Parse(dtBossMaxHP.Rows[0]["HP"].ToString());
                                strFormatedHP = intHPNow.ToString().Substring(0, intHPNow.ToString().Length - 4) + "万";
                            }
                            else
                            {
                                strFormatedHP = intHPNow.ToString();
                            }
                        }
                    }
                    else
                    {
                        //剩余情况应为长度超过4位的负数，偏差比较大，不简化会比较显眼
                        strFormatedHP = intHPNow.ToString();
                    }
                    strOutput2 = "目前进度：" + intRoundNow.ToString() + "周目，B" + intBCNow.ToString() + "，剩余血量(推测)=" + strFormatedHP;
                    MsgMessage += new Message(strOutput2 + "\r\n--------------------\r\n");
                    Console.WriteLine(strOutput2);
                    //订阅提醒
                    int intProgType;
                    if (intHPNow > 3000000)
                    {
                        //提醒到订阅类型0
                        intProgType = 0;
                    }
                    else if (intHPNow > 1000000)
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
                            string strRemindContext = "[公会战进度提醒]\r\n您所在群：" + strGrpID + "，BOSS进度已到B" + intBCNow + "，目前血量：" + strFormatedHP + "\r\n如时间方便，请做好本战准备。";
                            for (int i = 0; i < dtSubsMembers.Rows.Count; i++)
                            {
                                long lUserID = long.Parse(dtSubsMembers.Rows[i]["USERID"].ToString());
                                ApiProperties.HttpApi.SendPrivateMessageAsync(lUserID, strRemindContext);
                                Console.WriteLine("已私聊通知" + lUserID.ToString() + "(" + strGrpID + ")");
                                SubscribeDAL.UpdateRemindFlag(strGrpID, lUserID.ToString(), intRoundNow, intBCNow, intProgType);
                                Console.WriteLine("已更新通知状态" + lUserID.ToString() + "(" + strGrpID + ")");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("提醒查询失败（数据库错误）");
                    }
                    //下树提醒
                    if (QueueDAL.QuerySosList(strGrpID, intBCNow, intRoundNow, out DataTable dtSosList))
                    {
                        if (dtSosList != null && dtSosList.Rows.Count > 0)
                        {
                            if (!(dtSosList.Rows[0][0] is DBNull))
                            {
                                MsgMessage += new Message("下树提醒：");
                                for (int i = 0; i < dtSosList.Rows.Count; i++)
                                {
                                    //现在引用的类库（WUDILIB）在同一条信息at了多次同一个人时，显示效果会劣化，故在具备输入UID时避开第二次以上at该UID的情况发生
                                    if (dtSosList.Rows[i]["userid"].ToString() != strInputUserID)
                                    {
                                        if (i > 0 && i < dtSosList.Rows.Count)
                                        {
                                            MsgMessage += new Message("、");
                                        }
                                        string strUID = dtSosList.Rows[i]["userid"].ToString();
                                        MsgMessage += Message.At(long.Parse(strUID));
                                    }
                                }
                                MsgMessage += new Message("\r\n--------------------\r\n");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("下树查询失败（数据库错误）");
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
