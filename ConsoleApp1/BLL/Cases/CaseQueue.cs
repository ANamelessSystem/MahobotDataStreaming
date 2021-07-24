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
                RecordDAL.GetProgress(strGrpID, out dtProgress);
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
                        int intElapsedMinutes = (DateTime.Now - (DateTime)dtQueue.Rows[j]["JOINTIME"]).Minutes;
                        if (dtQueue.Rows[j]["JOINTYPE"].ToString() == "1")
                        {
                            intCount_Ext += 1;
                            strList_Ext += "【补时】" + dtQueue.Rows[j]["USERNAME"].ToString() + "(" + dtQueue.Rows[j]["USERID"].ToString() + ")";
                            if (intElapsedMinutes > 10)
                            {
                                strList_Ext += "\t(已等待" + intElapsedMinutes.ToString() + "分钟)";
                            }
                            strList_Ext += "\r\n";

                        }
                        else if (dtQueue.Rows[j]["JOINTYPE"].ToString() == "2")
                        {
                            intCount_Sos += 1;
                        }
                        else
                        {
                            strList_Normal += "【" + (intCount - intCount_Ext - intCount_Sos).ToString() + "】" + dtQueue.Rows[j]["USERNAME"].ToString() + "(" + dtQueue.Rows[j]["USERID"].ToString() + ")";
                            if (intElapsedMinutes > 10)
                            {
                                strList_Normal += "\t(已等待" + intElapsedMinutes.ToString() + "分钟)";
                            }
                            strList_Normal += "\r\n";
                        }
                    }
                }
                if (dtProgress.Rows.Count > 0)
                {
                    DataRow[] drProgress = dtProgress.Select("BC = " + i.ToString());
                    MsgSendHelper.ProgressRowHandler(drProgress,out strProcessRow);
                }
                strOutput += "B" + i.ToString() + strProcessRow;
                if (intCount > 0)
                {
                    if (intCount_Sos == intCount)//只有人挂树而无有效队列的情况
                    {
                        strOutput += "【" + intCount_Sos + "人等待救援】\r\n活动队列中无人。\r\n";
                    }
                    else if (intCount_Sos != 0)
                    {
                        strOutput += "【" + intCount_Sos + "人等待救援】\r\n" + strList_Ext + strList_Normal;
                    }
                    else
                    {
                        strOutput += "\r\n" + strList_Ext + strList_Normal;
                    }
                }
                else
                {
                    strOutput += "\r\n活动队列中无人。\r\n";
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
                MsgMessage += new Message("请指定BOSS编号或使用ALL指令退出所有队列。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                if (QueueDAL.QuitQueue(strGrpID, InputVariables.IntBossCode, strUserID, intQuitType))
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    if (intQuitType == 1)
                    {
                        MsgMessage += new Message("已退出所有队列。\r\n");
                    }
                    else
                    {
                        MsgMessage += new Message("已退出B" + InputVariables.IntBossCode.ToString() + "队列。\r\n");
                    }
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
                MsgMessage += new Message("请指定需要清空的BOSS队列。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (QueueDAL.QuitQueue(strGrpID, InputVariables.IntBossCode, strUserID, 2))
                {
                    MsgMessage += new Message("已清空B" + InputVariables.IntBossCode.ToString() + "队列。\r\n");
                }
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
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntBossCode == -1)
            {
                MsgMessage += new Message("未能找到BOSS编号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            try
            {
                QueueDAL.JoinQueue(strGrpID, InputVariables.IntBossCode, strUserID, 2);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            MsgMessage += new Message("已将较早一次B" + InputVariables.IntBossCode + "队列记录置为等待救援状态。");
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 查挂树名单
        /// </summary>
        /// <param name="strGrpID">群号</param>
        public static void QueueShow_Sos(string strGrpID)
        {
            DataTable dtSosQueue;
            string strList_sos = "";
            try
            {
                QueueDAL.ShowQueue(strGrpID, 0, "", 1, out dtSosQueue);
            }
            catch (Exception ex)
            {
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (dtSosQueue.Rows.Count > 0)
            {
                strList_sos += "等待救援列表：\r\n";
                for (int i = 0; i < dtSosQueue.Rows.Count; i++)
                {
                    if (dtSosQueue.Rows[i]["JOINTYPE"].ToString() == "2")
                    {
                        //预想效果：【B1】昵称(UID) 挂于X周目(已等待x分钟)
                        int intElapsedMinutes = (DateTime.Now - (DateTime)dtSosQueue.Rows[i]["JOINTIME"]).Minutes;
                        strList_sos += "【B" + dtSosQueue.Rows[i]["BC"].ToString() + "】" + dtSosQueue.Rows[i]["USERNAME"].ToString() + "(" + dtSosQueue.Rows[i]["USERID"].ToString() + ")\t挂于" + dtSosQueue.Rows[i]["JOINROUND"].ToString() + "周目(已等待" + intElapsedMinutes.ToString() + "分钟)\r\n";
                    }
                }
            }
            else
            {
                strList_sos += "目前无人等待救援";
            }
            MsgMessage += new Message(strList_sos);
            MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
        }
    }
}
