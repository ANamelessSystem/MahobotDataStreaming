using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using Sisters.WudiLib.Responses;
using Marchen.Helper;
using Oracle.ManagedDataAccess.Client;

namespace Marchen.BLL
{
    class CaseQueue : GroupMsgBLL
    {
        /// <summary>
        /// 加入队列
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户号</param>
        public static void QueueAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);
            #region member check and context splite error
            if (intMemberStatus == 0)
            {
                MsgMessage += new Message("尚未报名，无法加入队列，请先使用nla命令加入成员名单。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else if (intMemberStatus == -1)
            {
                MsgMessage += new Message("与数据库失去连接，查询名单失败。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("需要输入BOSS编号才可加入队列。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            #endregion
            if (InputVariables.IntEXT != 0 && InputVariables.IntEXT != 1)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("填入的队列类型不正确（类型留空或填补时）。\r\n");
                return;
            }
            try
            {
                QueueDAL.JoinQueue(strGrpID, InputVariables.IntBossCode, strUserID, InputVariables.IntEXT);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntEXT == 0)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("已加入B" + InputVariables.IntBossCode.ToString() + "队列，类型：通常\r\n");
            }
            if (InputVariables.IntEXT == 1)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("已加入B" + InputVariables.IntBossCode.ToString() + "队列，类型：补时\r\n");
            }
        }

        /// <summary>
        /// 展示队列
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户号</param>
        public static void QueueShow(string strGrpID, string strUserID, string strCmdContext)
        {
            //HpShowAndSubsCheck(strGrpID);
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                InputVariables.IntBossCode = 0;
            }
            DataTable dtQueue;
            try
            {
                QueueDAL.ShowQueue(strGrpID, InputVariables.IntBossCode, strUserID, InputVariables.IntIsAllFlag, out dtQueue);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (dtQueue.Rows.Count > 0)
            {
                int intBCBaseValue = 0;
                int intBCRange = ValueLimits.BossLimitMax + 1;
                string strOutput = "";
                if (InputVariables.IntIsAllFlag == 0 && InputVariables.IntBossCode > 0)
                {
                    intBCBaseValue = InputVariables.IntBossCode;
                    intBCRange = InputVariables.IntBossCode + 1;
                }
                else if (InputVariables.IntIsAllFlag == 1)
                {

                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString() + "执行队列请求时跳出错误\r\n" + strCmdContext + "\r\nALLFLAG=" + InputVariables.IntIsAllFlag.ToString() + ";BC=" + InputVariables.IntBossCode.ToString() );
                    MsgMessage += new Message("查询队列出现错误，请联系bot维护人员。");
                    MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
                }
                for (int i = intBCBaseValue; i < intBCRange; i++)
                {
                    int intCount = 0;
                    int intCount_Ext = 0;
                    int intCount_Sos = 0;
                    string strList_Ext = "";
                    string strList_Normal = "";
                    for (int j = 0; j < dtQueue.Rows.Count; j++)
                    {
                        if (dtQueue.Rows[j]["BC"].ToString() == i.ToString())
                        {
                            intCount += 1;
                            if (dtQueue.Rows[j]["JOINTYPE"].ToString() == "1")
                            {
                                intCount_Ext += 1;
                                strList_Ext += "【补时】" + dtQueue.Rows[j]["USERNAME"].ToString() + "(" + dtQueue.Rows[j]["USERID"].ToString() + ")\r\n";
                            }
                            else if (dtQueue.Rows[j]["JOINTYPE"].ToString() == "2")
                            {
                                intCount_Sos += 1;
                            }
                            else
                            {
                                strList_Normal += "【" + (intCount - intCount_Ext - intCount_Sos).ToString() + "】" + dtQueue.Rows[j]["USERNAME"].ToString() + "(" + dtQueue.Rows[j]["USERID"].ToString() + ")\r\n";
                            }
                        }
                    }
                    strOutput += "B" + i.ToString() + "队列：";
                    if (intCount > 0)
                    {
                        if (intCount_Sos == intCount)//只有人挂树而无有效队列的情况
                        {
                            strOutput += "（" + intCount_Sos + "人挂树）。\r\n目前队列中无人。\r\n";
                        }
                        else
                        {
                            strOutput += "（" + intCount_Sos + "人挂树）。\r\n" + strList_Ext + strList_Normal;
                        }
                    }
                    else
                    {
                        strOutput += "\r\n目前队列中无人。\r\n";
                    }
                }
                MsgMessage += new Message(strOutput);
                MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
            }
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
                    //Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀移出队列。");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message("已将较早一次队列记录退出。\r\n");
                }
                else
                {
                    //Console.WriteLine("群：" + strGrpID + "，" + strUserID + "移出队列失败：未找到记录。");
                    if (intType == 0)
                    {
                        MsgMessage += Message.At(long.Parse(strUserID));
                        MsgMessage += new Message("未找到队列记录。\r\n");
                    }
                    if (intType == 1)
                    {
                        MsgMessage += Message.At(long.Parse(strUserID));
                        MsgMessage += new Message("未找到队列记录，这可能是一次未排刀的伤害上报。\r\n");
                    }
                }
                //MsgSendHelper.UniversalMsgSender(0, 1, strGrpID, MsgMessage);
                //MsgMessage = new Message("");
                //QueueShow(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退出队列失败。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        //public static void QueueClear(string strGrpID, string strUserID, GroupMemberInfo memberInfo)
        //{
        //    if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
        //    {
        //        if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue_old))
        //        {
        //            if (dtQueue_old.Rows.Count > 0)
        //            {
        //                if (QueueDAL.ClearQueue(strGrpID, out int deletedCount))
        //                {
        //                    MsgMessage += new Message("已清空队列。\r\n");
        //                    Console.WriteLine("执行清空队列指令成功，共有" + deletedCount + "条记录受到影响");
        //                    MsgMessage += new Message("由于队列被清空，请以下成员重新排队：");
        //                    for (int i = 0; i < dtQueue_old.Rows.Count; i++)
        //                    {
        //                        string strUID = dtQueue_old.Rows[i]["id"].ToString();
        //                        MsgMessage += new Message("\r\nID：" + strUID + "， ") + Message.At(long.Parse(strUID));
        //                    }
        //                }
        //                else
        //                {
        //                    Console.WriteLine("与数据库失去连接，清空队列失败。");
        //                    MsgMessage += new Message("与数据库失去连接，清空队列失败。\r\n");
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine("执行清空队列指令失败，队列中无人。");
        //                MsgMessage += new Message("队列中无人，不需要清空。\r\n");
        //            }
        //        }
        //        else
        //        {
        //            MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("执行清空队列指令失败，由权限不足的人发起");
        //        MsgMessage += new Message("拒绝：仅有管理员或群主可执行队列清空指令。\r\n");
        //    }
        //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        //}

        /// <summary>
        /// 将一个队列记录修改为等待救援
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        public static void QueueAdd_Sos(string strGrpID, string strUserID,string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("未能找到BOSS编号。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntRound == -1)
            {
                if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
                {
                    InputVariables.IntRound = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                    if (InputVariables.IntBossCode < int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) && int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) == 5)
                    {
                        InputVariables.IntRound += 1;//BOSS显示进度还在B5，挂树着已经到了下B1或B2的情况
                    }
                }
            }
            if (QueueDAL.UpdateQueueToSos(strGrpID, strUserID, InputVariables.IntBossCode, InputVariables.IntRound, out int updCount))
            {
                if (updCount > 0)
                {
                    Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀置为等待救援状态。（B" + InputVariables.IntBossCode + "，" + InputVariables.IntRound + "周目）");
                    MsgMessage += new Message("已将较早一次队列记录置为等待救援状态。\r\n");
                }
                else
                {
                    Console.WriteLine("群：" + strGrpID + "，" + strUserID + "修改队列状态失败：未找到记录。");
                    MsgMessage += new Message("未找到队列记录，请先进入队列再改为等待救援状态。\r\n");
                }
                //QueueShow(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，修改队列状态失败。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }


        /// <summary>
        /// 经过优化的进度（剩余HP简略至万位）
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="_round"></param>
        /// <param name="_bc"></param>
        /// <param name="_hp"></param>
        /// <param name="_ratio"></param>
        /// <returns></returns>
        public static bool Format_Progress(string strGrpID,out int _round,out int _bc,out int _hp,out int _ratio)
        {
            //bool isCorrect = false;
            _round = 0;
            _bc = 0;
            _hp = 0;
            _ratio = 0;
            if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
            {
                if (dtBossProgress != null && dtBossProgress.Rows.Count > 0)
                {
                    if (dtBossProgress.Rows[0][0] is DBNull || dtBossProgress.Rows[0]["hpremain"] is DBNull || dtBossProgress.Rows[0]["maxround"] is DBNull || dtBossProgress.Rows[0]["maxbc"] is DBNull)
                    {
                        return false;
                    }
                }
                _hp = int.Parse(dtBossProgress.Rows[0]["hpremain"].ToString());
                _round = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                _bc = int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
                try
                {
                    if (_hp > 9999)
                    {
                        //5位正数以上自动转换为以万为单位优化显示
                        _hp = int.Parse(_hp.ToString()[0..^4]);
                        _ratio = 10000;
                        return true;
                    }
                    else if (_hp <= 9999 && _hp >= -9999)
                    {
                        //正负4位数（小误差），自动跳到下个BOSS
                        if (_bc == ValueLimits.BossLimitMax)
                        {
                            //现在为B5，需要跳到下周目B1的情况
                            if (StatisticsDAL.GetBossMaxHP(strGrpID, 1, _round + 1, out DataTable dtBossMaxHP))
                            {
                                //Console.WriteLine("误差内跳到下个BOSS");
                                _bc = 1;
                                _round += 1;
                                _hp = int.Parse(dtBossMaxHP.Rows[0]["HP"].ToString()[0..^4]);
                                _ratio = 10000;
                                return true;
                            }
                            else
                            {
                                //执行失败
                                return false;
                            }
                        }
                        else
                        {
                            if (StatisticsDAL.GetBossMaxHP(strGrpID, _bc + 1, _round, out DataTable dtBossMaxHP))
                            {
                                //Console.WriteLine("误差内跳到下个BOSS");
                                _bc += 1;
                                _hp = int.Parse(dtBossMaxHP.Rows[0]["HP"].ToString()[0..^4]);
                                _ratio = 10000;
                                return true;
                            }
                            else
                            {
                                //执行失败
                                return false;
                            }
                        }
                    }
                    else
                    {
                        //剩余情况应为长度超过4位的负数，偏差比较大，不简化会比较显眼
                        _ratio = 1;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 查挂树名单
        /// </summary>
        /// <param name="strGrpID">群号</param>
        //public static void QueueShow_Sos(string strGrpID)
        //{
        //    if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue))
        //    {
        //        if (dtQueue.Rows.Count > 0)
        //        {
        //            string strList_sos = "";
        //            for (int i = 0; i < dtQueue.Rows.Count; i++)
        //            {
        //                if (dtQueue.Rows[i]["sosflag"].ToString() == "1")
        //                {
        //                    strList_sos += "【挂树第"+i.ToString()+"名】" + dtQueue.Rows[i]["MBRNAME"].ToString() + "(" + dtQueue.Rows[i]["ID"].ToString() + ")    【挂于B" + dtQueue.Rows[i]["BC"].ToString() + "(周目" + dtQueue.Rows[i]["ROUND"].ToString() + ")】\r\n";
        //                }
        //            }
        //            string strOutput;
        //            if (strList_sos.Length != 0)
        //            {
        //                strOutput = "光   荣   榜：\r\n" + strList_sos;
        //            }
        //            else
        //            {
        //                strOutput = "目前队列无人挂树。太好了，继续保持。\r\n";
        //            }
        //            MsgMessage += new Message();
        //            MsgMessage += new Message(strOutput);
        //        }
        //        else
        //        {
        //            MsgMessage += new Message("目前队列中无人。\r\n");
        //        }
        //    }
        //    else
        //    {
        //        MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
        //    }
        //    MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
        //}

        /// <summary>
        /// 显示血量并检查是否有预定列表和下树提醒
        /// </summary>
        /// <param name="strGrpID">群号</param>
        public static void HpShowAndSubsCheck(string strGrpID)
        {
            if (Format_Progress(strGrpID, out int _round, out int _bc, out int _hp, out int _ratio))
            {
                string strOutput;
                if (_ratio == 10000)
                {
                    strOutput = "进度：" + _round.ToString() + "周目，B" + _bc.ToString() + "，剩余血量=" + _hp + "万";
                }
                else if (_ratio == 1)
                {
                    strOutput = "进度：" + _round.ToString() + "周目，B" + _bc.ToString() + "，剩余血量=" + _hp;
                }
                else
                {
                    MsgMessage += new Message("获取进度时发生预想外的错误，已关闭血量显示。\r\n");
                    return;
                }
                MsgMessage += new Message(strOutput + "\r\n");
            }
            else
            {
                //Message += new Message("获取进度时发生预想外的错误，已关闭血量显示。\r\n");
                return;
            }
            //订阅提醒
            int intProgType;
            if ((_hp * _ratio) > 3000000)
            {
                //提醒到订阅类型0
                intProgType = 0;
            }
            else
            {
                //提醒到下一个的订阅类型0
                intProgType = 2;
            }
            if (SubscribeDAL.BossReminder(strGrpID, _round, _bc, intProgType, out DataTable dtSubsMembers))
            {
                if (dtSubsMembers.Rows.Count > 0)
                {
                    string strUnit = "";
                    if (_ratio == 10000)
                    {
                        strUnit = "万";
                    }
                    string strRemindContext = "[公会战进度提醒]\r\n您所在群：" + strGrpID + "，BOSS进度已到B" + _bc + "，目前血量：" + _hp + strUnit + "\r\n如时间方便，请做好本战准备。";
                    for (int i = 0; i < dtSubsMembers.Rows.Count; i++)
                    {
                        long lUserID = long.Parse(dtSubsMembers.Rows[i]["USERID"].ToString());
                        ApiProperties.HttpApi.SendPrivateMessageAsync(lUserID, strRemindContext);
                        Console.WriteLine("已私聊通知" + lUserID.ToString() + "(" + strGrpID + ")");
                        SubscribeDAL.UpdateRemindFlag(strGrpID, lUserID.ToString(), _round, _bc, intProgType);
                        Console.WriteLine("已更新通知状态" + lUserID.ToString() + "(" + strGrpID + ")");
                    }
                }
            }
            else
            {
                Console.WriteLine("提醒查询失败（数据库错误）");
            }
            //下树提醒
            if (QueueDAL.QuerySosList(strGrpID, _bc, _round, out DataTable dtSosList))
            {
                if (dtSosList != null && dtSosList.Rows.Count > 0)
                {
                    if (!(dtSosList.Rows[0][0] is DBNull))
                    {
                        MsgMessage += new Message("下树提醒：");
                        for (int i = 0; i < dtSosList.Rows.Count; i++)
                        {
                            if (i > 0 && i < dtSosList.Rows.Count)
                            {
                                MsgMessage += new Message("、");
                            }
                            MsgMessage += Message.At(long.Parse(dtSosList.Rows[i]["userid"].ToString()));
                        }
                        MsgMessage += new Message("\r\n");
                    }
                }
            }
            else
            {
                Console.WriteLine("下树查询失败（数据库错误）");
            }
            MsgSendHelper.UniversalMsgSender(MsgSendType.Raw, MsgTargetType.Group, strGrpID, MsgMessage);
            MsgMessage = new Message("");
        }
    }
}
