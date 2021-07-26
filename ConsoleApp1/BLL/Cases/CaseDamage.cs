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
            string strRecUID = strUserID;//被代刀人ID，此处初始化
            string strUpldrID = strUserID;//原上传人ID，使用调用人ID
            int intTimeAddjust = 0;
            bool isProxy = false;//代刀标记
            int intEID;
            //分拆命令
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                //识别出来的数据处理
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
                        isProxy = true;
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

            if (InputVariables.IntTimeOutFlag == 1)
            {
                //掉线
                //防止计入尾刀掉线
                if (InputVariables.IntEXT == 2)
                {
                    InputVariables.IntEXT = 0;
                }
                InputVariables.IntDMG = 0;
                InputVariables.IntBossCode = 1;
            }

            if (isProxy)
            {
                strRecUID = InputVariables.DouUID.ToString();
            }

            //执行上传
            try
            {
                RecordDAL.AddDamageRecord(strGrpID, strRecUID, strUpldrID, InputVariables.IntDMG, InputVariables.IntBossCode, InputVariables.IntEXT, intTimeAddjust, out intEID);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            MsgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
            //根据返回EID，反查记录并显示
            //MsgMessage = new Message("伤害已保存，类型：" + strDmg_Type + "，周目：" + intRound_Calculate + "，档案号=" + intEID.ToString() + "\r\n");
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            MsgMessage = new Message("");
            //执行退队
            CaseQueue.QueueQuit(strGrpID, strRecUID, "B" + InputVariables.IntBossCode.ToString(), true);
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
            DataTable dtDmgRecOriginal;
            try
            {
                RecordDAL.GetDamageRecord(strGrpID,"", InputVariables.IntEID,0,0,0,1,out dtDmgRecOriginal);
            }
            catch (Exception ex)
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                MsgMessage += new Message(ex.Message.ToString());
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            int intRound;
            int intDMG;
            int intBossCode;
            int intExTime;
            string strOriUID;
            string strNewUID;
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
                MsgMessage += new Message("输入的档案号：" + InputVariables.IntEID + " 返回非唯一结果，请联系bot维护人员。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                //给结果值先赋上原始数据，再用需要修改的部分覆盖掉这些数值，以简化判断
                strOriUID = dtDmgRecOriginal.Rows[0]["USERID"].ToString();
                strNewUID = strOriUID;
                intDMG = int.Parse(dtDmgRecOriginal.Rows[0]["DMG"].ToString());
                intRound = int.Parse(dtDmgRecOriginal.Rows[0]["RECROUND"].ToString());
                intBossCode = int.Parse(dtDmgRecOriginal.Rows[0]["BC"].ToString());
                intExTime = int.Parse(dtDmgRecOriginal.Rows[0]["RECTYPE"].ToString());
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
                try
                {
                    RecordDAL.ModifyDamageRecord(strGrpID, strNewUID, intDMG, intRound, intBossCode, intExTime, InputVariables.IntEID);
                }
                catch (Exception ex)
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message(ex.Message.ToString());
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                MsgMessage += new Message("修改成功。");
                if (DmgOutputUniform(dtDmgRecOriginal, 0, out string strOriOutput))
                {
                    MsgMessage += new Message("\r\n原记录：\r\n" + strOriOutput);
                }
                string strQryEID = "E" + InputVariables.IntEID.ToString();
                MsgMessage += new Message("\r\n修改后：");
                RecordQuery(strGrpID, strUserID, strQryEID);
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
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            DataTable dtDmgRecords;
            if (InputVariables.IntEID != -1)
            {
                //Console.WriteLine("识别到EventID，优先使用EventID作为查询条件（唯一）");
                try
                {
                    RecordDAL.GetDamageRecord(strGrpID, strUserID, InputVariables.IntEID, 0, 0, 0, 1, out dtDmgRecords);
                }
                catch (Exception ex)
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message(ex.Message.ToString());
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                if (DmgOutputUniform(dtDmgRecords, 0, out string strOutput))
                {
                    MsgMessage += new Message("档案号E" + InputVariables.IntEID + "的记录：\r\n");
                    MsgMessage += new Message(strOutput);
                }
                else
                {
                    MsgMessage += new Message("出现意料外的错误，请联系bot维护人员。\r\n");
                    //MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
            }
            else if ((InputVariables.DouUID != -1 && InputVariables.IntBossCode == -1 && InputVariables.IntRound == -1))
            {
                //仅按UID查询
                //Console.WriteLine("识别为按UID");
                string strRUID = InputVariables.DouUID.ToString();
                try
                {
                    RecordDAL.GetDamageRecord(strGrpID, strRUID, 0, 0, 0, InputVariables.IntIsAllFlag, 2, out dtDmgRecords);
                }
                catch (Exception ex)
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message(ex.Message.ToString());
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                if (dtDmgRecords.Rows.Count == 0)
                {
                    MsgMessage += new Message("尚无伤害记录。");
                }
                else
                {
                    string strRName = dtDmgRecords.Rows[0]["NAME"].ToString();
                    if (InputVariables.IntIsAllFlag == 0)
                    {
                        MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：本日)\r\n");
                    }
                    else
                    {
                        MsgMessage += new Message(strRName + "(" + strRUID + ")的记录：\r\n(查询范围：整期)\r\n");
                    }
                    if (DmgOutputUniform(dtDmgRecords, 1, out string strOutput))
                    {
                        MsgMessage += new Message(strOutput);
                    }
                    else
                    {
                        MsgMessage += new Message("出现意料外的错误，请联系bot维护人员。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
            }
            else if (InputVariables.DouUID == -1 && InputVariables.IntBossCode != -1 && InputVariables.IntRound != -1)
            {
                //按周目+BOSS查询
                //Console.WriteLine("识别为按周目+BOSS或单独周目");
                try
                {
                    RecordDAL.GetDamageRecord(strGrpID, strUserID, 0, InputVariables.IntBossCode, InputVariables.IntRound, 0, 3, out dtDmgRecords);
                }
                catch (Exception ex)
                {
                    MsgMessage += Message.At(long.Parse(strUserID));
                    MsgMessage += new Message(ex.Message.ToString());
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                if (dtDmgRecords.Rows.Count == 0)
                {
                    MsgMessage += new Message("\r\n尚无伤害记录。\r\n");
                }
                else
                {
                    string strOutput;
                    if (InputVariables.IntBossCode != -1)
                    {
                        if (DmgOutputUniform(dtDmgRecords, 3, out strOutput))
                        {
                            MsgMessage += new Message(InputVariables.IntRound + "周目B" + InputVariables.IntBossCode + "的记录：\r\n");
                        }
                        else
                        {
                            MsgMessage += new Message("出现意料外的错误，请联系bot维护人员。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
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
                            MsgMessage += new Message("出现意料外的错误，请联系bot维护人员。\r\n");
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
                MsgMessage += new Message("不支持的查询模式。仅支持以下四种条件查询：\r\n1.单独按档案号查询\r\n2.单独按QQ号查询\r\n3.同时按BOSS与周目查询。\r\n");
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
                    string strRDmg = dtInput.Rows[i]["DMG"].ToString();
                    string strRRound = dtInput.Rows[i]["RECROUND"].ToString();
                    string strRBC = dtInput.Rows[i]["BC"].ToString();
                    string strREID = dtInput.Rows[i]["EVENTID"].ToString();
                    string strRTime = dtInput.Rows[i]["RECTIME"].ToString();
                    string strRUID = dtInput.Rows[i]["USERID"].ToString();
                    string strRName = dtInput.Rows[i]["NAME"].ToString();
                    if (dtInput.Rows[i]["RECTYPE"].ToString() == "2")
                    {
                        strRDmg += " （尾刀）";
                    }
                    else if (dtInput.Rows[i]["RECTYPE"].ToString() == "1")
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
