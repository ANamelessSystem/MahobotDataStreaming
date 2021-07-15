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
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);//后期人员鉴定做进存储过程
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
            int intJoinType;
            if (InputVariables.IntEXT == 0)
            {
                intJoinType = 0;
            }
            else if (InputVariables.IntEXT == 1)
            {
                intJoinType = 1;
            }
            else
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("填入的队列类型不正确（类型留空或填补时）。\r\n");
                return;
            }
            try
            {
                QueueDAL.JoinQueue(strGrpID, InputVariables.IntBossCode, strUserID, intJoinType);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            if (intJoinType == 0)
            {
                MsgMessage += new Message("已加入B" + InputVariables.IntBossCode.ToString() + "队列，类型：通常\r\n");
            }
            else
            {
                MsgMessage += new Message("已加入B" + InputVariables.IntBossCode.ToString() + "队列，类型：补时\r\n");
            }
            strCmdContext = "B" + InputVariables.IntBossCode.ToString();
            QueueShow(strGrpID, strUserID, strCmdContext);
        }

        /// <summary>
        /// 展示队列
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户号</param>
        public static void QueueShow(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                InputVariables.IntBossCode = 0;
            }
            int intBCBaseValue;
            int intBCRange;
            string strOutput = "";
            string strProcessRow = "";
            if (InputVariables.IntIsAllFlag == 1)
            {
                intBCBaseValue = 1;
                intBCRange = ValueLimits.BossLimitMax + 1;
            }
            else if (InputVariables.IntIsAllFlag == 0 && InputVariables.IntBossCode > 0)
            {
                intBCBaseValue = InputVariables.IntBossCode;
                intBCRange = InputVariables.IntBossCode + 1;
            }
            else if (InputVariables.IntIsAllFlag == 0 && InputVariables.IntBossCode == 0)
            {
                MsgMessage += new Message("查询队列请添加BOSS序号参数，或使用ALL参数查询所有BOSS。");
                MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
                return;
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行队列请求时出现未预料的错误\r\n" + strCmdContext + "\r\nALLFLAG=" + InputVariables.IntIsAllFlag.ToString() + ";BC=" + InputVariables.IntBossCode.ToString());
                MsgMessage += new Message("查询队列出现错误，请联系bot维护人员。");
                MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
                return;
            }

            DataTable dtQueue;
            DataTable dtProgress;
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
            try
            {
                RecordDAL.GetBossProgress(strGrpID, out dtProgress);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("获取进度时发生错误，请联系维护人员。");
                Console.WriteLine(DateTime.Now.ToString() + "获取进度时发生未预料的错误。" + ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
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
                if (dtProgress.Rows.Count > 0)
                {
                    DataRow[] drProgress = dtProgress.Select("BC = " + i.ToString());
                    MsgSendHelper.ProgressRowHandler(drProgress,out strProcessRow);
                }
                if (InputVariables.IntIsAllFlag == 1)
                {
                    strOutput += "B" + i.ToString() + strProcessRow + "队列：";
                }
                else
                {
                    strOutput += "B" + i.ToString() + strProcessRow + "队列：";
                }
                if (intCount > 0)
                {
                    if (intCount_Sos == intCount)//只有人挂树而无有效队列的情况
                    {
                        strOutput += "（" + intCount_Sos + "人挂树）\r\n目前队列中无人。\r\n";
                    }
                    else if (intCount_Sos != 0)
                    {
                        strOutput += "（" + intCount_Sos + "人挂树）\r\n" + strList_Ext + strList_Normal;
                    }
                    else
                    {
                        strOutput += "\r\n" + strList_Ext + strList_Normal;
                    }
                }
                else
                {
                    strOutput += "\r\n目前队列中无人。\r\n";
                }
                if (InputVariables.IntIsAllFlag == 1)
                {
                    strOutput += "\r\n";
                }
            }
            MsgMessage += new Message(strOutput);
            MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
        }




        /// <summary>
        /// 退出队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        /// <param name="bShowQueue">退出后是否显示对应BOSS的队列，目前设计为用户使用命令退出后不显示队列，但提交伤害等自动化退出指令后显示</param>
        public static void QueueQuit(string strGrpID, string strUserID, string strCmdContext, bool bShowQueue)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            int intQuitType;
            if (InputVariables.IntIsAllFlag == 1)
            {
                intQuitType = 1;
            }
            else
            {
                intQuitType = 0;
            }
            if (InputVariables.IntBossCode == -1)
            {
                InputVariables.IntBossCode = 0;
            }
            if (InputVariables.IntBossCode < 1 && intQuitType == 0)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message("请指定BOSS编号或使用ALL指令退出所有队伍。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                if (QueueDAL.QuitQueue(strGrpID, InputVariables.IntBossCode, strUserID, intQuitType))
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message("已退出队列。\r\n");
                    if (bShowQueue && InputVariables.IntBossCode > 0)
                    {
                        QueueShow(strGrpID,strUserID,"B" + InputVariables.IntBossCode);
                    }
                    else
                    {
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    }
                }
                else
                {
                    MsgMessage += new Message("执行失败，退出队列失败。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                }
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueClear(string strGrpID, string strUserID, GroupMemberInfo memberInfo,string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("清空队列需指定BOSS编码以清空特定BOSS的队列。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (QueueDAL.QuitQueue(strGrpID, InputVariables.IntBossCode, strUserID, 2))
                {
                    MsgMessage += new Message("已清空B" + InputVariables.IntBossCode.ToString() + "队列。\r\n");
                }
                //if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue_old))
                //{
                //    if (dtQueue_old.Rows.Count > 0)
                //    {
                //        if (QueueDAL.ClearQueue(strGrpID, out int deletedCount))
                //        {
                //            MsgMessage += new Message("已清空队列。\r\n");
                //            Console.WriteLine("执行清空队列指令成功，共有" + deletedCount + "条记录受到影响");
                //            MsgMessage += new Message("由于队列被清空，请以下成员重新排队：");
                //            for (int i = 0; i < dtQueue_old.Rows.Count; i++)
                //            {
                //                string strUID = dtQueue_old.Rows[i]["id"].ToString();
                //                MsgMessage += new Message("\r\nID：" + strUID + "， ") + Message.At(long.Parse(strUID));
                //            }
                //        }
                //        else
                //        {
                //            Console.WriteLine("与数据库失去连接，清空队列失败。");
                //            MsgMessage += new Message("与数据库失去连接，清空队列失败。\r\n");
                //        }
                //    }
                //else
                //{
                //    Console.WriteLine("执行清空队列指令失败，队列中无人。");
                //    MsgMessage += new Message("队列中无人，不需要清空。\r\n");
                //}
                //    }
                else
                {
                    MsgMessage += new Message("数据库错误，清空队列失败。\r\n");
                    Console.WriteLine(DateTime.Now.ToString() + "队列清空失败，数据库执行失败");
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
        public static void QueueAdd_Sos(string strGrpID, string strUserID,string strCmdContext)
        {
            //if (!CmdHelper.CmdSpliter(strCmdContext))
            //{
            //    MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
            //    //MsgMessage += Message.At(long.Parse(strUserID));
            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //    return;
            //}
            //if (InputVariables.IntBossCode == -1)
            //{
            //    MsgMessage += new Message("未能找到BOSS编号。\r\n");
            //    //MsgMessage += Message.At(long.Parse(strUserID));
            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //    return;
            //}
            //if (InputVariables.IntRound == -1)
            //{
            //    if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
            //    {
            //        InputVariables.IntRound = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
            //        if (InputVariables.IntBossCode < int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) && int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString()) == 5)
            //        {
            //            InputVariables.IntRound += 1;//BOSS显示进度还在B5，挂树着已经到了下B1或B2的情况
            //        }
            //    }
            //}
            //if (QueueDAL.UpdateQueueToSos(strGrpID, strUserID, InputVariables.IntBossCode, InputVariables.IntRound, out int updCount))
            //{
            //    if (updCount > 0)
            //    {
            //        Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀置为等待救援状态。（B" + InputVariables.IntBossCode + "，" + InputVariables.IntRound + "周目）");
            //        MsgMessage += new Message("已将较早一次队列记录置为等待救援状态。\r\n");
            //    }
            //    else
            //    {
            //        Console.WriteLine("群：" + strGrpID + "，" + strUserID + "修改队列状态失败：未找到记录。");
            //        MsgMessage += new Message("未找到队列记录，请先进入队列再改为等待救援状态。\r\n");
            //    }
            //    //QueueShow(strGrpID, strUserID);
            //}
            //else
            //{
            //    MsgMessage += new Message("与数据库失去连接，修改队列状态失败。\r\n");
            //    //MsgMessage += Message.At(long.Parse(strUserID));
            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //}
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
        public static bool Format_Progress(string strGrpID, out int _round, out int _bc, out int _hp, out int _ratio)
        {
            //bool isCorrect = false;
            _round = 0;
            _bc = 0;
            _hp = 0;
            _ratio = 0;
            RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress);
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
    }
}
