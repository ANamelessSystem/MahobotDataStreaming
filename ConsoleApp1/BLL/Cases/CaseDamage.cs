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
        /// 一般的上传伤害
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        public static void DmgRecAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            //检查是否成员
            int intMemberStatus = QueueDAL.MemberCheck(strGrpID, strUserID);
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
            //分拆命令
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                //识别出来的数据处理
                if (CommonVariables.IntEXT == -1)
                {
                    CommonVariables.IntEXT = 0;
                }
                if (CommonVariables.IntDMG == -1)
                {
                    MsgMessage += new Message("未能找到伤害值。\r\n");
                    isCorrect = false;
                }
                if (CommonVariables.IntRound == -1)
                {
                    MsgMessage += new Message("未能找到周目值。\r\n");
                    isCorrect = false;
                }
                if (CommonVariables.IntBossCode == -1)
                {
                    MsgMessage += new Message("未能找到BOSS编号。\r\n");
                    isCorrect = false;
                }
                //判断数据正误标记位
                if (!isCorrect)
                {
                    MsgMessage += new Message("伤害保存失败。\r\n输入【@MahoBot help】获取帮助。\r\n");
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
                //第一刀不检查，它有可能发生在凌晨，你来不及处理，所以你放过它吧
                if (intBC_Progress != 0 && intRound_Progress != 0)
                {
                    //检查是否跳周目（非B1但输了比目前高的周目）
                    if (intRound_Progress < CommonVariables.IntRound && CommonVariables.IntBossCode != 1)
                    {
                        MsgMessage += new Message("周目数可能有误（可能跨周目），已拒绝本次提交，请重新检查。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                    //检查是否跳了BOSS（输了比目前高的非连续的BOSS）
                    if ((intBC_Progress + 1) < CommonVariables.IntBossCode && CommonVariables.IntRound >= intRound_Progress)
                    {
                        MsgMessage += new Message("BOSS代码可能有误（可能跨BOSS），已拒绝本次提交，请重新检查。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
                //执行上传
                if (RecordDAL.DamageDebrief(strGrpID, strUserID, CommonVariables.IntDMG, CommonVariables.IntRound, CommonVariables.IntBossCode, CommonVariables.IntEXT, out int intEID))
                {
                    Console.WriteLine(DateTime.Now.ToString() + "伤害已保存，档案号=" + intEID.ToString() + "，B" + CommonVariables.IntBossCode.ToString() + "，" + CommonVariables.IntRound.ToString() + "周目，数值：" + CommonVariables.IntDMG.ToString() + "，补时标识：" + CommonVariables.IntEXT);
                    MsgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
                    //执行退订
                    CaseSubscribe.SubsDelAuto(strGrpID, strUserID, CommonVariables.IntBossCode);
                    //执行退队
                    CaseQueue.QueueQuit(strGrpID, strUserID);
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，伤害保存失败。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
            }
        }

        /// <summary>
        /// 记录掉线
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        public static void DmgTimeOut(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (CommonVariables.IntEXT == -1)
            {
                CommonVariables.IntEXT = 0;
            }
            if (CommonVariables.IntDMG == -1)
            {
                CommonVariables.IntDMG = 0;
            }
            if (RecordDAL.GetBossProgress(strGrpID,out DataTable dtBossProgress))
            {
                CommonVariables.IntRound = int.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
                CommonVariables.IntBossCode = int.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，掉线记录失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (RecordDAL.DamageDebrief(strGrpID, strUserID, CommonVariables.IntDMG, CommonVariables.IntRound, CommonVariables.IntBossCode, CommonVariables.IntEXT, out int intEID))
            {
                MsgMessage = new Message("掉线已记录，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                CaseQueue.QueueQuit(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，掉线记录失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
        }

        /// <summary>
        /// 修改伤害记录
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        /// <param name="memberInfo"></param>
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
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (CommonVariables.IntEID == -1)
            {
                MsgMessage += new Message("未识别出需要修改的档案号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (RecordDAL.QueryDmgRecByEID(CommonVariables.IntEID, strGrpID, out DataTable dtDmgRec))
            {
                if (dtDmgRec.Rows.Count < 1)
                {
                    Console.WriteLine("输入的档案号：" + CommonVariables.IntEID + " 未能找到数据。");
                    MsgMessage += new Message("输入的档案号：" + CommonVariables.IntEID + " 未能找到数据。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
                else if (dtDmgRec.Rows.Count > 1)
                {
                    Console.WriteLine("输入的档案号：" + CommonVariables.IntEID + " 返回非唯一结果。");
                    MsgMessage += new Message("输入的档案号：" + CommonVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
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
            if (CommonVariables.DouUID != -1)
            {
                strNewUID = CommonVariables.DouUID.ToString();
            }
            if (CommonVariables.IntDMG != -1)
            {
                intDMG = CommonVariables.IntDMG;
            }
            if (CommonVariables.IntRound != -1)
            {
                intRound = CommonVariables.IntRound;
            }
            if (CommonVariables.IntBossCode != -1)
            {
                intBossCode = CommonVariables.IntBossCode;
            }
            if (CommonVariables.IntEXT != -1)
            {
                intExTime = CommonVariables.IntEXT;
            }
            if (strUserID == strOriUID || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                //仅允许本人或管理员进行修改
                if (RecordDAL.DamageUpdate(strGrpID, strNewUID, intDMG, intRound, intBossCode, intExTime, CommonVariables.IntEID))
                {
                    MsgMessage += new Message("修改成功，");
                    string strQryEID = "E" + CommonVariables.IntEID.ToString();
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
                Console.WriteLine("只有本人或管理员以上可修改。修改者：" + strUserID + " 原记录：" + strOriUID + "EventID：" + CommonVariables.IntEID.ToString());
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
            if (CommonVariables.IntEID != -1)
            {
                Console.WriteLine("识别到EventID，优先使用EventID作为查询条件（唯一）");
                if (RecordDAL.QueryDmgRecByEID(CommonVariables.IntEID, strGrpID, out DataTable dtDmgRecEID))
                {
                    if (dtDmgRecEID.Rows.Count < 1)
                    {
                        Console.WriteLine("输入的档案号：" + CommonVariables.IntEID + " 未能找到数据。\r\n");
                        MsgMessage += new Message("输入的档案号：" + CommonVariables.IntEID + " 未能找到数据。\r\n");
                    }
                    else if (dtDmgRecEID.Rows.Count > 1)
                    {
                        Console.WriteLine("输入的档案号：" + CommonVariables.IntEID + " 返回非唯一结果。");
                        MsgMessage += new Message("输入的档案号：" + CommonVariables.IntEID + " 返回非唯一结果，请联系维护团队。\r\n");
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
                        Console.WriteLine("档案号" + CommonVariables.IntEID + "的数据为：\r\n" + resultString + "\r\n");
                        MsgMessage += new Message("档案号" + CommonVariables.IntEID + "的数据为：\r\n" + resultString + "\r\n");
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询记录失败。\r\n");
                }
            }
            else if ((CommonVariables.DouUID != -1 && CommonVariables.IntBossCode == -1 && CommonVariables.IntRound == -1)|| (CommonVariables.DouUID == -1 && CommonVariables.IntBossCode != -1 && CommonVariables.IntRound != -1))
            {
                Console.WriteLine("识别到UID或周目+BOSS");
                if (RecordDAL.QueryDmgRecords(CommonVariables.IntBossCode, CommonVariables.IntRound, CommonVariables.DouUID, strGrpID, out DataTable dtDmgRecords))
                {
                    for (int i = 0; i < dtDmgRecords.Rows.Count; i++)
                    {
                        string strRUID = dtDmgRecords.Rows[i]["userid"].ToString();
                        string strRDmg = dtDmgRecords.Rows[i]["dmg"].ToString();
                        string strRRound = dtDmgRecords.Rows[i]["round"].ToString();
                        string strRBC = dtDmgRecords.Rows[i]["bc"].ToString();
                        string strREXT = dtDmgRecords.Rows[i]["extime"].ToString();
                        string strREID = dtDmgRecords.Rows[i]["eventid"].ToString();
                        string resultString = "";
                        if (dtDmgRecords.Rows[i]["dmg"].ToString() == "0")
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
                        else if (dtDmgRecords.Rows[i]["dmg"].ToString() != "0")
                        {
                            if (strREXT == "1")
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + " （补时）";
                            }
                            else
                            {
                                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg;
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
                        MsgMessage += new Message("E" + strREID + "：" + resultString + "\r\n");
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
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            return;
        }
    }
}
