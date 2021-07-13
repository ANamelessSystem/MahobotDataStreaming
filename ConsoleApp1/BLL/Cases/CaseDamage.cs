using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using System.Text.RegularExpressions;
using Sisters.WudiLib.Responses;
using Marchen.Helper;

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
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else if (intMemberStatus == -1)
            {
                MsgMessage += new Message("与数据库失去连接，查询名单失败。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            bool isCorrect = true;//数据正误标记位
            string strRecUID = strUserID;//代刀标记
            bool isProxyRecord = false;//代刀标记
            //分拆命令
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                //识别出来的数据处理
                if (InputVariables.IntEXT != 2)
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
                    if (InputVariables.IntBossCode == -1)
                    {
                        MsgMessage += new Message("未能找到BOSS编号。\r\n");
                        isCorrect = false;
                    }
                }
            }
            if (!isCorrect)
            {
                //判断数据正误标记位
                MsgMessage += new Message("伤害记录退回。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            int intRound_Calculate = 0;
            //请求目前进度
            if (CaseQueue.Format_Progress(strGrpID, out int _round, out int _bc, out int _hp, out int _ratio))
            {
                intRound_Calculate = _round;
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询进度失败。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntTimeOutFlag != 1)
            {
                if (InputVariables.IntBossCode > (_bc + 1) || (InputVariables.IntBossCode + 1) < _bc)
                {
                    //输入了和现在进度非相邻关系的BOSS
                    if (_bc == 5 && InputVariables.IntBossCode == 1)
                    {
                        //进入下一周目
                        intRound_Calculate += 1;
                    }
                    else if (_bc == 1 && InputVariables.IntBossCode == 5 && _round > 1)
                    {
                        //填回上周目
                        intRound_Calculate += -1;
                    }
                    else
                    {
                        //如非以上两种情况则认为输错
                        if (_bc == 1)
                        {
                            MsgMessage += new Message("所提交的BOSS有误，已拒绝本次提交。\r\n目前进度为" + _round + "周目B1，可报B1、B2或上周目的B5。如需补报更早的记录，请先报成本BOSS再使用记录修改功能进行更改。\r\n");
                        }
                        else if (_bc == 5)
                        {
                            MsgMessage += new Message("所提交的BOSS有误，已拒绝本次提交。\r\n目前进度为" + _round + "周目B5，可报B4、B5或下周目的B1。如需补报更早的记录，请先报成本BOSS再使用记录修改功能进行更改。\r\n");
                        }
                        else
                        {
                            MsgMessage += new Message("所提交的BOSS有误，已拒绝本次提交。\r\n目前进度为" + _round + "周目B" + _bc + "，可报B" + (_bc - 1) + "、B" + _bc + "或B" + (_bc + 1) + "。如需补报更早的记录，请先报成本BOSS再使用记录修改功能进行更改。\r\n");
                        }
                        //MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
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
                InputVariables.IntRound = _round;
                InputVariables.IntBossCode = _bc;
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
            if (RecordDAL.DamageDebrief(strGrpID, strRecUID, InputVariables.IntDMG, intRound_Calculate, InputVariables.IntBossCode, InputVariables.IntEXT, out int intEID))
            {
                string strDmg_Type = "";
                if (InputVariables.IntEXT == 0)
                {
                    strDmg_Type = "通常";
                }
                else if (InputVariables.IntEXT == 1)
                {
                    strDmg_Type = "补时";
                }
                else
                {
                    strDmg_Type = "尾刀";
                }
                Console.WriteLine(DateTime.Now.ToString() + "伤害已保存，档案号=" + intEID.ToString() + "，B" + InputVariables.IntBossCode.ToString() + "，" + intRound_Calculate.ToString() + "周目，数值：" + InputVariables.IntDMG.ToString() + "，补时标识：" + InputVariables.IntEXT);
                //吞消息比较频繁，先发送保存的消息，再进行队列操作的输出
                MsgMessage = new Message("伤害已保存，类型：" + strDmg_Type + "，周目：" + intRound_Calculate + "，档案号=" + intEID.ToString() + "\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                MsgMessage = new Message("");
                //如果是尾刀，自动订阅下个周目的相同BOSS
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
                //CaseQueue.QueueQuit(strGrpID, strUserID, 1);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，伤害保存失败。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
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
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntEID == -1)
            {
                MsgMessage += new Message("未识别出需要修改的档案号。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (RecordDAL.QueryDmgRecByEID(InputVariables.IntEID, strGrpID, out DataTable dtDmgRecOriginal))
            {
                if (dtDmgRecOriginal.Rows.Count < 1)
                {
                    Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。");
                    MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                else if (dtDmgRecOriginal.Rows.Count > 1)
                {
                    Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果。");
                    MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                else
                {
                    //给结果值先赋上原始数据，再用需要修改的部分覆盖掉这些数值，以简化判断
                    strOriUID = dtDmgRecOriginal.Rows[0]["userid"].ToString();
                    strNewUID = strOriUID;
                    intDMG = int.Parse(dtDmgRecOriginal.Rows[0]["dmg"].ToString());
                    intRound = int.Parse(dtDmgRecOriginal.Rows[0]["round"].ToString());
                    intBossCode = int.Parse(dtDmgRecOriginal.Rows[0]["bc"].ToString());
                    intExTime = int.Parse(dtDmgRecOriginal.Rows[0]["extime"].ToString());
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
                    MsgMessage += new Message("修改成功。");
                    if (DmgOutputUniform(dtDmgRecOriginal,0, out string strOriOutput))
                    {
                        MsgMessage += new Message("\r\n原记录：\r\n" + strOriOutput);
                    }
                    string strQryEID = "E" + InputVariables.IntEID.ToString();
                    MsgMessage += new Message("\r\n修改后：");
                    RecordQuery(strGrpID, strUserID, strQryEID);
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，修改失败。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可修改。修改者：" + strUserID + " 原记录：" + strOriUID + "EventID：" + InputVariables.IntEID.ToString());
                MsgMessage += new Message("只有本人或管理员以上可修改。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
        }

        /// <summary>
        /// 查询伤害记录
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">查询人QQ号</param>
        /// <param name="strCmdContext">命令内容</param>
        public static void RecordQuery(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntEID != -1)
            {
                Console.WriteLine("识别到EventID，优先使用EventID作为查询条件（唯一）");
                if (RecordDAL.QueryDmgRecByEID(InputVariables.IntEID, strGrpID, out DataTable dtDmgRecords))
                {
                    if (dtDmgRecords.Rows.Count < 1)
                    {
                        Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                        MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 未能找到数据。\r\n");
                    }
                    else if (dtDmgRecords.Rows.Count > 1)
                    {
                        Console.WriteLine("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果。");
                        MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
                    }
                    else
                    {
                        if (DmgOutputUniform(dtDmgRecords,0, out string strOutput))
                        {
                            MsgMessage += new Message("档案号E" + InputVariables.IntEID + "的记录：\r\n");
                            MsgMessage += new Message(strOutput);
                        }
                        else
                        {
                            MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                            //MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
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
                //Console.WriteLine("识别为按UID");
                string strRUID = InputVariables.DouUID.ToString();
                string strRName = "";
                if (NameListDAL.GetMemberName(strGrpID, InputVariables.DouUID.ToString(), out string strResult))
                {
                    strRName = strResult;
                }
                else
                {
                    MsgMessage += new Message("该用户未在名单中注册。\r\n");
                }
                if (!ClanInfoDAL.GetClanTimeOffset(strGrpID, out int intHourSet))
                {
                    MsgMessage += new Message("与数据库失去连接，查询区域时间设定失败。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                if (intHourSet < 0)
                {
                    MsgMessage += new Message("每日更新小时设定小于0，尚未验证这种形式的时间格式是否正常，已退回本功能。\r\n");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
                {
                    DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                    DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(intHourSet);//当天结算时间开始
                    DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(intHourSet);//第二天结算时间结束
                    if (InputVariables.IntIsAllFlag == 1)
                    {
                        if (RecordDAL.QueryDmgRecords_All(InputVariables.DouUID, strGrpID, out DataTable dtDmgRecords))
                        {
                            if (InputVariables.IntIsAllFlag == 0)
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：本日)\r\n");
                            }
                            else
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：整期)\r\n");
                            }
                            if (dtDmgRecords.Rows.Count == 0)
                            {
                                MsgMessage += new Message("\r\n尚无伤害记录。");
                            }
                            else
                            {
                                if (DmgOutputUniform(dtDmgRecords, 1, out string strOutput))
                                {
                                    MsgMessage += new Message(strOutput);
                                }
                                else
                                {
                                    MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                    //MsgMessage += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                        }
                    }
                    else if (dtNow.Hour >= 0 && dtNow.Hour < intHourSet)
                    {
                        //0点后日期变换，开始日期需查到昨天
                        dtStart = dtStart.AddDays(-1);//当天结算时间开始
                        dtEnd = dtEnd.AddDays(-1);//第二天结算时间结束
                        if (RecordDAL.QueryDmgRecords(InputVariables.DouUID, strGrpID, dtStart, dtEnd, out DataTable dtDmgRecords))
                        {
                            if (InputVariables.IntIsAllFlag == 0)
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：本日)\r\n");
                            }
                            else
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：整期)\r\n");
                            }
                            if (dtDmgRecords.Rows.Count == 0)
                            {
                                MsgMessage += new Message("\r\n尚无伤害记录。");
                            }
                            else
                            {
                                if (DmgOutputUniform(dtDmgRecords, 1, out string strOutput))
                                {
                                    MsgMessage += new Message(strOutput);
                                }
                                else
                                {
                                    MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                    //MsgMessage += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                    return;
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
                        if (RecordDAL.QueryDmgRecords(InputVariables.DouUID, strGrpID, dtStart, dtEnd, out DataTable dtDmgRecords))
                        {
                            if (InputVariables.IntIsAllFlag == 0)
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：本日)\r\n");
                            }
                            else
                            {
                                MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：整期)\r\n");
                            }
                            if (dtDmgRecords.Rows.Count == 0)
                            {
                                MsgMessage += new Message("\r\n尚无伤害记录。");
                            }
                            else
                            {
                                if (DmgOutputUniform(dtDmgRecords, 1, out string strOutput))
                                {
                                    MsgMessage += new Message(strOutput);
                                }
                                else
                                {
                                    MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                    //MsgMessage += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                        }
                    }
                }
            }
            else if ((InputVariables.DouUID == -1 && InputVariables.IntBossCode != -1 && InputVariables.IntRound != -1) || (InputVariables.DouUID == -1 && InputVariables.IntRound != -1))
            {
                //按周目+BOSS查询
                Console.WriteLine("识别为按周目+BOSS或单独周目");
                if (RecordDAL.QueryDmgRecords(InputVariables.IntBossCode, InputVariables.IntRound, strGrpID, out DataTable dtDmgRecords))
                {
                    if (dtDmgRecords.Rows.Count == 0)
                    {
                        MsgMessage += new Message("\r\n尚无伤害记录。\r\n");
                    }
                    else
                    {
                        string strOutput = "";
                        if (InputVariables.IntBossCode != -1)
                        {
                            if (DmgOutputUniform(dtDmgRecords, 3, out strOutput))
                            {
                                MsgMessage += new Message(InputVariables.IntRound + "周目B" + InputVariables.IntBossCode + "的记录：\r\n");
                            }
                            else
                            {
                                MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                //MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                        }
                        else
                        {
                            if (DmgOutputUniform(dtDmgRecords, 2, out strOutput))
                            {
                                MsgMessage += new Message(InputVariables.IntRound + "周目的记录：\r\n");
                            }
                            else
                            {
                                MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                //MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                        }
                        MsgMessage += new Message(strOutput);
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("不支持的查询模式。仅支持以下四种条件查询：\r\n1.单独按档案号查询\r\n2.单独按QQ号查询\r\n3.单独按周目查询\r\n4.同时按BOSS与周目查询。\r\n");
            }
            //MsgMessage += Message.At(long.Parse(strUserID));
            MsgSendHelper.UniversalMsgSender(MsgSendType.Auto, MsgTargetType.Group, strGrpID, MsgMessage);
            return;
        }

        /// <summary>
        /// 统一管理查询结果回复消息格式的地方
        /// </summary>
        /// <param name="dtInput">dt查询结果表</param>
        /// <param name="intLayoutType">0：查询EID；1：查询UID；2：查询周目；3：查询BOSS+周目</param>
        /// <param name="strOutput">输出回复消息内容</param>
        /// <returns></returns>
        private static bool DmgOutputUniform(DataTable dtInput,int intLayoutType,out string strOutput)
        {
            strOutput = "";
            try
            {
                for (int i = 0; i < dtInput.Rows.Count; i++)
                {
                    string strRDmg = dtInput.Rows[i]["dmg"].ToString();
                    string strRRound = dtInput.Rows[i]["round"].ToString();
                    string strRBC = dtInput.Rows[i]["bc"].ToString();
                    string strREID = dtInput.Rows[i]["eventid"].ToString();
                    string strRTime = dtInput.Rows[i]["time"].ToString();
                    string strRUID = dtInput.Rows[i]["userid"].ToString();
                    string strRName = dtInput.Rows[i]["name"].ToString();
                    if (dtInput.Rows[i]["extime"].ToString() == "2")
                    {
                        strRDmg += " （尾刀）";
                    }
                    else if (dtInput.Rows[i]["extime"].ToString() == "1")
                    {
                        strRDmg += " （补时）";
                    }
                    else
                    {
                        strRDmg += " （通常）";
                    }
                    string resultString = "";
                    if (intLayoutType == 0)//查询EID（抬头显示EID）
                    {
                        resultString = strRName + "(" + strRUID + ")：" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + "；\r\n          记录时间：" + strRTime;
                    }
                    else if (intLayoutType == 1)//查询UID（抬头显示UID+昵称）
                    {
                        resultString = "E" + strREID + "：" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + "；\r\n          记录时间：" + strRTime;
                    }
                    else if (intLayoutType == 2)//查询周目（抬头显示周目）
                    {
                        resultString = "B" + strRBC + "；" + strRName + "(" + strRUID + ")：伤害=" + strRDmg + "；\r\n       记录时间：" + strRTime + " 【E" + strREID + "】";
                    }
                    else //3 查询周目+BOSS（抬头显示周目+BOSS）
                    {
                        resultString = "E" + strREID + "：" + strRName + "(" + strRUID + ")：伤害=" + strRDmg + "；\r\n          记录时间：" + strRTime;
                    }
                    strOutput += resultString + "\r\n";
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "将查询结果dt转换为消息时弹出错误" + ex);
                return false;
            }
        }
    }
}
