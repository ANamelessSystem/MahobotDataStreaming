using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using System.Text.RegularExpressions;
using Sisters.WudiLib.Responses;

namespace Marchen.BLL
{
    class CaseDamage : GroupMsgBLL
    {
        /// <summary>
        /// 上传伤害，并试图退出队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        public static void DmgRecAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);//检查会话人是否成员
            if (intMemberStatus == 0)
            {
                MsgMessage += new Message("尚未报名，无法上传伤害。\r\n");
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
            bool isCorrect = true;//数据正误标记位
            string strRecUID = strUserID;//代刀标记
            bool isProxyRecord = false;//代刀标记
            //分拆命令
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                //MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                //识别出来的数据处理
                if (InputVariables.IntEXT == -1)
                {
                    InputVariables.IntEXT = 0;
                }
                if (InputVariables.DouUID != -1)
                {
                    //代刀规则
                    int intProxyMbrStatus = NameListDAL.MemberCheck(strGrpID, InputVariables.DouUID.ToString());//检查被代理人是否成员
                    if (intProxyMbrStatus == 0)
                    {
                        MsgMessage += new Message("被代理人(" + InputVariables.DouUID + ")尚未报名，无法上传伤害。\r\n");
                        isCorrect = false;
                    }
                    else if (intProxyMbrStatus == -1)
                    {
                        MsgMessage += new Message("与数据库失去连接，查询名单失败。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        isProxyRecord = true;
                    }
                }
                if (InputVariables.IntTimeOutFlag != 1)
                {
                    //如果没掉线就要检查数据的正确性
                    if (InputVariables.IntDMG == -1)
                    {
                        MsgMessage += new Message("未能找到伤害值。\r\n");
                        isCorrect = false;
                    }
                    if (InputVariables.IntRound == -1)
                    {
                        MsgMessage += new Message("未能找到周目值。\r\n");
                        isCorrect = false;
                    }
                    if (InputVariables.IntBossCode == -1)
                    {
                        MsgMessage += new Message("未能找到BOSS编号。\r\n");
                        isCorrect = false;
                    }
                }
            }
            //判断数据正误标记位
            if (!isCorrect)
            {
                MsgMessage += new Message("伤害记录退回。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            //请求目前进度
            int intBC_Progress = 0;
            int intRound_Progress = 0;
            if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
            {
                if (dtBossProgress != null && dtBossProgress.Rows.Count > 0)
                {
                    if (!(dtBossProgress.Rows[0][0] is DBNull))
                    {
                        intBC_Progress = int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
                        intRound_Progress = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                    }
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询进度失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntTimeOutFlag != 1)
            {
                //如果非掉线，检查是否跨周目跨BOSS
                //第一刀不检查
                if (!(intBC_Progress == 1 && intRound_Progress == 1))
                {
                    //检查是否跳周目
                    if (InputVariables.IntRound > intRound_Progress)
                    {
                        //唯一允许输入周目比目前大的情况：目前B5，输入B1，且输入的周目=目前周目+1
                        if (!(intBC_Progress == 5 && InputVariables.IntBossCode == 1 && intRound_Progress + 1 == InputVariables.IntRound))
                        {
                            MsgMessage += new Message("所提交的周目值有误(可能跨周目)，已拒绝本次提交，请重新检查。\r\n（如需补记较早的记录请先提交为目前进度，再使用记录修改功能进行修正。）\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                    }
                    if (InputVariables.IntRound < intRound_Progress)
                    {
                        //唯一允许输入周目比目前小的情况：目前B1，输入B5，且输入的周目=目前周目-1
                        if (!(intBC_Progress == 1 && InputVariables.IntBossCode == 5 && intRound_Progress - 1 == InputVariables.IntRound))
                        {
                            MsgMessage += new Message("所提交的周目值有误(可能跨周目)，已拒绝本次提交，请重新检查。\r\n（如需补记较早的记录请先提交为目前进度，再使用记录修改功能进行修正。）\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                    }
                    //检查是否跳BOSS
                    if (InputVariables.IntBossCode > (intBC_Progress + 1))
                    {
                        //唯一允许的提交比目前高的非连续的BOSS例外情况：有人先输入了下周目的B1，需要往前补一个B5记录的情况
                        if (!(intBC_Progress == 1 && InputVariables.IntBossCode == 5 && intRound_Progress == InputVariables.IntRound + 1))
                        {
                            MsgMessage += new Message("所提交的BOSS代码有误(可能跨BOSS)，已拒绝本次提交，请重新检查。\r\n（如需补记较早的记录请先提交为目前进度，再使用记录修改功能进行修正。）\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                    }
                    if ((InputVariables.IntBossCode + 1) < intBC_Progress)
                    {
                        //唯一允许的提交比目前低的非连续的BOSS例外情况：现在B5，跨到下周目B1
                        if (!(intBC_Progress == 5 && InputVariables.IntBossCode == 1 && intRound_Progress == (InputVariables.IntRound - 1)))
                        {
                            MsgMessage += new Message("所提交的BOSS代码有误(可能跨BOSS)，已拒绝本次提交，请重新检查。\r\n（如需补记较早的记录请先提交为目前进度，再使用记录修改功能进行修正。）\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                    }
                }
            }
            else
            {
                //如果掉线了就按当前进度记录0伤害的数据
                //防止计入尾刀掉线
                if (InputVariables.IntEXT == 2)
                {
                    InputVariables.IntEXT = 0;
                }
                InputVariables.IntDMG = 0;
                InputVariables.IntRound = intRound_Progress;
                InputVariables.IntBossCode = intBC_Progress;
            }
            if (isProxyRecord)
            {
                strRecUID = InputVariables.DouUID.ToString();
            }
            RecordDAL.CheckLastAttack(strGrpID, strRecUID, out int isLastAtk);
            if (isLastAtk == 1)
            {
                //上一刀为尾刀，本刀自动记为补时
                InputVariables.IntEXT = 1;
            }
            //执行上传
            if (RecordDAL.DamageDebrief(strGrpID, strRecUID, InputVariables.IntDMG, InputVariables.IntRound, InputVariables.IntBossCode, InputVariables.IntEXT, out int intEID))
            {
                Console.WriteLine(DateTime.Now.ToString() + "伤害已保存，档案号=" + intEID.ToString() + "，B" + InputVariables.IntBossCode.ToString() + "，" + InputVariables.IntRound.ToString() + "周目，数值：" + InputVariables.IntDMG.ToString() + "，补时标识：" + InputVariables.IntEXT);
                MsgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
                //如果是尾刀，自动订阅下个周目的相同BOSS
                //if (InputVariables.IntEXT == 2 && !isProxyRecord)
                //{
                //    //只有非代打的情况会给予补时刀预约
                //    CaseSubscribe.SubsAdd(strGrpID, strUserID, InputVariables.IntBossCode, (InputVariables.IntRound + 1));
                //}
                //else if (InputVariables.IntEXT == 1 && !isProxyRecord)
                //{
                //    SubscribeDAL.DelExtSubs(strGrpID, strUserID, out int intDelCount);
                //    if (intDelCount > 0)
                //    {
                //        MsgMessage += new Message("检测到已出补时刀，已自动删除补时刀预约。\r\n");
                //    }
                //}
                //else 
                if (isProxyRecord)
                {
                    //如果代打了，删除被代打人的预约
                    CaseSubscribe.SubsDel(strGrpID, strRecUID, InputVariables.IntBossCode);
                }
                else
                {
                    //其余情况删除会话人预约
                    CaseSubscribe.SubsDel(strGrpID, strUserID, InputVariables.IntBossCode);
                }
                //执行退队
                CaseQueue.QueueQuit(strGrpID, strUserID, 1);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，伤害保存失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
        }

        /// <summary>
        /// 修改伤害记录
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">命令原文</param>
        /// <param name="memberInfo">传入的用户信息</param>
        public static void DmgModify(string strGrpID, string strUserID, string strCmdContext, GroupMemberInfo memberInfo)
        {
            int intRound = 0;
            int intDMG = -1;
            int intBossCode = 0;
            int intExTime = 0;
            string strOriUID = "";
            string strNewUID = "";
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                //MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntEID == -1)
            {
                MsgMessage += new Message("未识别出需要修改的档案号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (RecordDAL.QueryDmgRecByEID(InputVariables.IntEID, strGrpID, out DataTable dtDmgRec))
            {
                if (dtDmgRec.Rows.Count < 1)
                {
                    Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。");
                    MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                else if (dtDmgRec.Rows.Count > 1)
                {
                    Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果。");
                    MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                else
                {
                    //给结果值先赋上原始数据，再用需要修改的部分覆盖掉这些数值，以简化判断
                    strOriUID = dtDmgRec.Rows[0]["userid"].ToString();
                    strNewUID = strOriUID;
                    intDMG = int.Parse(dtDmgRec.Rows[0]["dmg"].ToString());
                    intRound = int.Parse(dtDmgRec.Rows[0]["round"].ToString());
                    intBossCode = int.Parse(dtDmgRec.Rows[0]["bc"].ToString());
                    intExTime = int.Parse(dtDmgRec.Rows[0]["extime"].ToString());
                }
            }
            if (InputVariables.DouUID != -1)
            {
                strNewUID = InputVariables.DouUID.ToString();
            }
            if (InputVariables.IntDMG != -1)
            {
                intDMG = InputVariables.IntDMG;
            }
            if (InputVariables.IntRound != -1)
            {
                intRound = InputVariables.IntRound;
            }
            if (InputVariables.IntBossCode != -1)
            {
                intBossCode = InputVariables.IntBossCode;
            }
            if (InputVariables.IntEXT != -1)
            {
                intExTime = InputVariables.IntEXT;
            }
            if (strUserID == strOriUID || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                //仅允许本人或管理员进行修改
                if (RecordDAL.DamageUpdate(strGrpID, strNewUID, intDMG, intRound, intBossCode, intExTime, InputVariables.IntEID))
                {
                    MsgMessage += new Message("修改成功，");
                    string strQryEID = "E" + InputVariables.IntEID.ToString();
                    RecordQuery(strGrpID, strUserID, strQryEID);
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，修改失败。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可修改。修改者：" + strUserID + " 原记录：" + strOriUID + "EventID：" + InputVariables.IntEID.ToString());
                MsgMessage += new Message("只有本人或管理员以上可修改。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
        }

        public static void RecordQuery(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntEID != -1)
            {
                Console.WriteLine("识别到EventID，优先使用EventID作为查询条件（唯一）");
                if (RecordDAL.QueryDmgRecByEID(InputVariables.IntEID, strGrpID, out DataTable dtDmgRecEID))
                {
                    if (dtDmgRecEID.Rows.Count < 1)
                    {
                        Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                        MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                    }
                    else if (dtDmgRecEID.Rows.Count > 1)
                    {
                        Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果。");
                        MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
                    }
                    else
                    {
                        string strRUID = dtDmgRecEID.Rows[0]["userid"].ToString();
                        string strRDmg = dtDmgRecEID.Rows[0]["dmg"].ToString();
                        string strRRound = dtDmgRecEID.Rows[0]["round"].ToString();
                        string strRBC = dtDmgRecEID.Rows[0]["bc"].ToString();
                        string strREXT = dtDmgRecEID.Rows[0]["extime"].ToString();
                        string resultString = "";
                        if (dtDmgRecEID.Rows[0]["dmg"].ToString() == "0")
                        {
                            if (strREXT == "1")
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害= 0(掉线) （补时）";
                            }
                            else
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害= 0(掉线)";
                            }
                        }
                        else if (dtDmgRecEID.Rows[0]["dmg"].ToString() != "0")
                        {
                            if (strREXT == "1")
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + " （补时）";
                            }
                            else if (strREXT == "2")
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + " （尾刀）";
                            }
                            else
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg;
                            }
                        }
                        else
                        {
                            Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRecEID.Rows[0]["dmg"].ToString());
                            MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                        }
                        Console.WriteLine("档案号" + InputVariables.IntEID + "的数据为：\r\n" + resultString + "\r\n");
                        MsgMessage += new Message("档案号" + InputVariables.IntEID + "的数据为：\r\n" + resultString + "\r\n");
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                }
            }
            else if ((InputVariables.DouUID != -1 && InputVariables.IntBossCode == -1 && InputVariables.IntRound == -1))
            {
                //仅按UID查询
                Console.WriteLine("识别为按UID");
                string strRUID = InputVariables.DouUID.ToString();
                string strRName = "";
                if (NameListDAL.GetMemberName(strGrpID, InputVariables.DouUID.ToString(), out string strResult))
                {
                    strRName = strResult;
                }
                if (RecordDAL.QueryDmgRecords(InputVariables.DouUID, strGrpID, InputVariables.IntIsAllFlag, out DataTable dtDmgRecords))
                {
                    if (InputVariables.IntIsAllFlag == 0)
                    {
                        MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：本日)");
                    }
                    else
                    {
                        MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：整期)");
                    }
                    if (dtDmgRecords.Rows.Count == 0)
                    {
                        MsgMessage += new Message("\r\n尚无伤害记录。");
                    }
                    else
                    {
                        for (int i = 0; i < dtDmgRecords.Rows.Count; i++)
                        {
                            string strRDmg = dtDmgRecords.Rows[i]["dmg"].ToString();
                            string strRRound = dtDmgRecords.Rows[i]["round"].ToString();
                            string strRBC = dtDmgRecords.Rows[i]["bc"].ToString();
                            string strREXT = dtDmgRecords.Rows[i]["extime"].ToString();
                            string strREID = dtDmgRecords.Rows[i]["eventid"].ToString();
                            string strRTime = dtDmgRecords.Rows[i]["time"].ToString();
                            string resultString = "";
                            if (dtDmgRecords.Rows[i]["dmg"].ToString() == "0")
                            {
                                if (strREXT == "1")
                                {
                                    resultString = strRRound + "周目B" + strRBC + "；伤害= 0(掉线) （补时）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else
                                {
                                    resultString = strRRound + "周目B" + strRBC + "；伤害= 0(掉线)；\r\n      记录时间：[" + strRTime + "]";
                                }
                            }
                            else if (dtDmgRecords.Rows[i]["dmg"].ToString() != "0")
                            {
                                if (strREXT == "1")
                                {
                                    resultString = strRRound + "周目B" + strRBC + "；伤害=" + strRDmg + " （补时）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else if (strREXT == "2")
                                {
                                    resultString = strRRound + "周目B" + strRBC + "；伤害=" + strRDmg + " （尾刀）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else
                                {
                                    resultString = strRRound + "周目B" + strRBC + "；伤害=" + strRDmg + "；\r\n      记录时间：[" + strRTime + "]";
                                }
                            }
                            else
                            {
                                Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRecords.Rows[i]["dmg"].ToString());
                                MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            Console.WriteLine("E" + strREID + "：" + resultString + "\r\n");
                            MsgMessage += new Message("\r\nE" + strREID + "：" + resultString);
                        }
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                }
            }
            else if ((InputVariables.DouUID == -1 && InputVariables.IntBossCode != -1 && InputVariables.IntRound != -1))
            {
                //按周目+BOSS查询
                Console.WriteLine("识别为按周目+BOSS");
                if (RecordDAL.QueryDmgRecords(InputVariables.IntBossCode, InputVariables.IntRound, strGrpID, out DataTable dtDmgRecords))
                {
                    MsgMessage += new Message(InputVariables.IntRound + "周目B" + InputVariables.IntBossCode + "伤害记录：");
                    if (dtDmgRecords.Rows.Count == 0)
                    {
                        MsgMessage += new Message("\r\n尚无伤害记录。\r\n");
                    }
                    else
                    {
                        for (int i = 0; i < dtDmgRecords.Rows.Count; i++)
                        {
                            string strRDmg = dtDmgRecords.Rows[i]["dmg"].ToString();
                            string strRRound = dtDmgRecords.Rows[i]["round"].ToString();
                            string strRBC = dtDmgRecords.Rows[i]["bc"].ToString();
                            string strREXT = dtDmgRecords.Rows[i]["extime"].ToString();
                            string strREID = dtDmgRecords.Rows[i]["eventid"].ToString();
                            string strRTime = dtDmgRecords.Rows[i]["time"].ToString();
                            string strRUID = dtDmgRecords.Rows[i]["userid"].ToString();
                            string strRName = dtDmgRecords.Rows[i]["name"].ToString();
                            string resultString = "";
                            if (dtDmgRecords.Rows[i]["dmg"].ToString() == "0")
                            {
                                if (strREXT == "1")
                                {
                                    resultString = strRName + "(" + strRUID + ")： 伤害= 0(掉线) （补时）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else
                                {
                                    resultString = strRName + "(" + strRUID + ")： 伤害= 0(掉线)；\r\n      记录时间：[" + strRTime + "]";
                                }
                            }
                            else if (dtDmgRecords.Rows[i]["dmg"].ToString() != "0")
                            {
                                if (strREXT == "1")
                                {
                                    resultString = strRName + "(" + strRUID + ")： 伤害=" + strRDmg + " （补时）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else if (strREXT == "2")
                                {
                                    resultString = strRName + "(" + strRUID + ")： 伤害=" + strRDmg + " （尾刀）；\r\n      记录时间：[" + strRTime + "]";
                                }
                                else
                                {
                                    resultString = strRName + "(" + strRUID + ")： 伤害=" + strRDmg + "；\r\n      记录时间：[" + strRTime + "]";
                                }
                            }
                            else
                            {
                                Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRecords.Rows[i]["dmg"].ToString());
                                MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            Console.WriteLine("E" + strREID + "：" + resultString + "\r\n");
                            MsgMessage += new Message("\r\nE" + strREID + "：" + resultString);
                        }
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("目前支持单独按档案号查询、单独按QQ号查询以及同时按BOSS与周目查询。\r\n");
            }
            MsgMessage += new Message("\r\n") + Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            return;
        }
    }
}
