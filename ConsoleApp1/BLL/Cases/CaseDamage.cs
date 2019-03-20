﻿using System;
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
        /// 检查数据库的后台表是否已经创建
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="outMsgMessage"></param>
        /// <returns></returns>
        public static bool DmgTblCheck(string strGrpID)
        {
            if (RecordDAL.CheckClanDmgTable(strGrpID, out DataTable dtTableCount))
            {
                if (int.Parse(dtTableCount.Rows[0]["count"].ToString()) == 0)
                {
                    if (RecordDAL.CreateTablesForGuildDamage(strGrpID))
                    {
                        Console.WriteLine("已成功为公会群" + strGrpID + "建立伤害表。");
                        MsgMessage += new Message("(未找到本公会伤害后台数据表，已自动建立。)\r\n");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("为公会群" + strGrpID + "建立伤害表过程中失败。");
                        MsgMessage += new Message("(公会伤害后台数据表建立失败。)\r\n");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("伤害表检查pass");
                    return true;
                }
            }
            else
            {
                Console.WriteLine("读取公会群" + strGrpID + "的伤害表失败。");
                MsgMessage += new Message("与数据库失去连接，读取本公会伤害表失败。\r\n");
                return false;
            }
        }

        /// <summary>
        /// 伤害值的转换，以及有效性检测
        /// </summary>
        /// <param name="intUnitType"></param>
        /// <param name="e"></param>
        /// <param name="intResultDamage">0：无单位，1:千，2:万</param>
        /// <param name="outMsgMessage"></param>
        /// <returns></returns>
        public static bool DamageAnalyzation(int intUnitType, string e, out int intResultDamage)
        {
            int intMultiplier = 0;
            if (intUnitType == 0)
            {
                Console.WriteLine("伤害值单位为无");
                intMultiplier = 1;
            }
            else if (intUnitType == 1)
            {
                Console.WriteLine("伤害值单位为千");
                intMultiplier = 1000;
            }
            else if (intUnitType == 2)
            {
                Console.WriteLine("伤害值单位为万");
                intMultiplier = 10000;
            }
            else
            {
                Console.WriteLine("处理伤害值时收到了非指定的倍率类型，值为：" + intUnitType.ToString());
                intResultDamage = -1;
                return false;
            }
            if (!decimal.TryParse(Regex.Replace(e, @"[^\d.\d]", ""), out decimal dclOutDamage))
            {
                Console.WriteLine("无法识别伤害，输入值为：" + e);
                MsgMessage += new Message("无法识别伤害，请检查输入的伤害值。\r\n");
                intResultDamage = -1;
                return false;
            }
            else
            {
                if (!int.TryParse(decimal.Round(dclOutDamage * intMultiplier, 0).ToString(), out int intOutDamage))
                {
                    Console.WriteLine("无法识别伤害，输入值为：" + e);
                    MsgMessage += new Message("无法识别伤害，请检查输入的伤害值。\r\n");
                    intResultDamage = -1;
                    return false;
                }
                else
                {
                    if (intOutDamage > ValueLimits.DamageLimitMax)
                    {
                        Console.WriteLine("伤害值超出可信范围，输入字串为：" + e + "，上限值为：" + ValueLimits.DamageLimitMax.ToString());
                        MsgMessage += new Message("所填入的伤害值(" + intOutDamage.ToString() + ")高于目前设定的上限值（" + ValueLimits.DamageLimitMax.ToString() + "）。\r\n");
                        intResultDamage = -1;
                        return false;
                    }
                    else if (intOutDamage < 1)
                    {
                        Console.WriteLine("伤害值过低，输入字串为：" + e);
                        MsgMessage += new Message("所填入的伤害值（" + intOutDamage.ToString() + "）低于有效值（1）。\r\n");
                        intResultDamage = -1;
                        return false;
                    }
                    else
                    {
                        intResultDamage = intOutDamage;
                        Console.WriteLine("伤害识别与转换完成，输出为：" + intResultDamage);
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// 一般的上传伤害
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        public static void DmgRecAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!DmgTblCheck(strGrpID))
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (!CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            else
            {
                if (RecordDAL.DamageDebrief(strGrpID, strUserID, CommonVariables.IntDMG, CommonVariables.IntRound, CommonVariables.IntBossCode, CommonVariables.IntEXT, out int intEID))
                {
                    Console.WriteLine(DateTime.Now.ToString() + "伤害已保存，档案号=" + intEID.ToString() + "，B" + CommonVariables.IntBossCode.ToString() + "，" + CommonVariables.IntRound.ToString() + "周目，数值：" + CommonVariables.IntDMG.ToString() + "，补时标识：" + CommonVariables.IntEXT);
                    MsgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
                    //Console.WriteLine("伤害以保存，启动退刀");
                    //Console.WriteLine("退刀前的信息\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
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
            CmdSpliter(strCmdContext);
            if (RecordDAL.DamageDebrief(strGrpID, strUserID, 0, 0, 0, CommonVariables.IntEXT, out int intEID))
            {
                MsgMessage = new Message("掉线已记录，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                CaseQueue.QueueQuit(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，掉线记录失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
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
            int intEID = 0;
            int intRound = 0;
            int intDMG = -1;
            int intBossCode = 0;
            int intExTime = 0;
            string strOriUID = "";
            string strNewUID = "";
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {
                if (e.ToLower().Contains("e"))
                {
                    if (!int.TryParse(e.ToLower().Replace("e", ""), out int intOutEID))
                    {
                        Console.WriteLine("无法识别档案号。原始信息=" + e.ToString());
                        MsgMessage += new Message("无法识别档案号。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                    else
                    {
                        intEID = intOutEID;
                    }
                    if (RecordDAL.QueryDmgRecByEID(intEID, strGrpID, out DataTable dtDmgRec))
                    {
                        if (dtDmgRec.Rows.Count < 1)
                        {
                            Console.WriteLine("输入的档案号：" + intEID + " 未能找到数据。");
                            MsgMessage += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else if (dtDmgRec.Rows.Count > 1)
                        {
                            Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                            MsgMessage += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
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
                    else
                    {
                        MsgMessage += new Message("与数据库失去连接，修改失败。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
                else if (e.ToLower().Contains("u"))
                {
                    if (!double.TryParse(e.ToLower().Replace("u", ""), out double douOutUID))
                    {
                        Console.WriteLine("输入的qq号并非全数字或无法转换成double，输入内容：" + e);
                        MsgMessage += new Message("用户ID请填入数字QQ号。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                    else
                    {
                        strNewUID = douOutUID.ToString();
                    }
                }
                else if (e.ToLower().Contains("b"))
                {
                    if (e.ToLower() == "b1" || e.ToLower() == "b2" || e.ToLower() == "b3" || e.ToLower() == "b4" || e.ToLower() == "b5")
                    {
                        if (!int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                        {
                            Console.WriteLine("无法识别BOSS代码，输入字串为：" + strCmdContext);
                            MsgMessage += new Message("无法识别BOSS代码，请确保填入为b1~b5\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else
                        {
                            intBossCode = intOutBC;
                        }
                    }
                }
                else if (e.Contains("周目"))
                {
                    if (!int.TryParse(e.Replace("周目", ""), out int intOutRound))
                    {
                        Console.WriteLine("无法识别周目数，输入字串为：" + strCmdContext);
                        MsgMessage += new Message("无法识别周目数，请确保填入的周目数为阿拉伯数字。\r\n");
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                    else
                    {
                        if (intOutRound > ValueLimits.RoundLimitMax)
                        {
                            Console.WriteLine("周目数过高，输入字串为：" + strCmdContext + "，上限值为：" + ValueLimits.RoundLimitMax.ToString());
                            MsgMessage += new Message("所填入的周目数(" + intOutRound.ToString() + ")高于目前设定的上限值（" + ValueLimits.RoundLimitMax.ToString() + "）。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else if (intOutRound < 1)
                        {
                            Console.WriteLine("周目数过低，输入字串为：" + strCmdContext);
                            MsgMessage += new Message("所填入的周目数（" + intOutRound.ToString() + "）低于有效值（1）。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else
                        {
                            intRound = intOutRound;
                        }
                    }
                }
                else if (e == "补时")
                {
                    intExTime = 1;
                }
                else if (e == "非补时")
                {
                    intExTime = 0;
                }
                else if (e == "掉线")
                {
                    intDMG = 0;
                }
                else if (e.ToLower().Contains("w") || e.Contains("万"))
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
                else if (e.ToLower().Contains("k"))
                {
                    if (DamageAnalyzation(1, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 1)
                {
                    if (DamageAnalyzation(0, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        MsgMessage += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                }
            }
            if (strUserID == strOriUID || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                //仅允许本人或管理员进行修改
                if (RecordDAL.DamageUpdate(strGrpID, strNewUID, intDMG, intRound, intBossCode, intExTime, intEID))
                {
                    MsgMessage += new Message("修改成功，");
                    //goto case "dmgshow";
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，修改失败。\r\n");
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可修改。修改者：" + strUserID + " 原记录：" + strOriUID + "EventID：" + intEID.ToString());
                MsgMessage += new Message("只有本人或管理员以上可修改。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        public static void RecordQuery1(string strGrpID, string strUserID, string strCmdContext)
        {
            //按EID查询单条，或按周目、boss、周目+boss查询多条
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (strCmdContext.ToLower().Contains("e"))
            {
                //处理按EID查询部分
                int intEID = 0;
                foreach (string e in sArray)
                {
                    if (e.ToLower().Contains("e"))
                    {
                        if (!int.TryParse(e.ToLower().Replace("e", ""), out int intOutEID))
                        {
                            Console.WriteLine("无法识别档案号。原始信息=" + e.ToString());
                            MsgMessage += new Message("无法识别档案号。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else
                        {
                            intEID = intOutEID;
                        }
                    }
                    if (intEID > 0)
                    {
                        if (RecordDAL.QueryDmgRecByEID(intEID, strGrpID, out DataTable dtDmgRecEID))
                        {
                            if (dtDmgRecEID.Rows.Count < 1)
                            {
                                Console.WriteLine("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                MsgMessage += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                            }
                            else if (dtDmgRecEID.Rows.Count > 1)
                            {
                                Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                                MsgMessage += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
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
                                    MsgMessage += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                    return;
                                }
                                Console.WriteLine("档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                                MsgMessage += new Message("档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                            }
                        }
                    }
                }
            }
            else if (!strCmdContext.ToLower().Contains("e") && (strCmdContext.ToLower().Contains("b") && strCmdContext.Contains("周目")))
            {
                //处理按boss与周目查询部分
                Console.WriteLine("开始按周目、boss、UID查询");
                int intBossCode = 0;
                int intRound = 0;
                foreach (string e in sArray)
                {
                    if (e.ToLower().Contains("b"))
                    {
                        if (!int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                        {
                            Console.WriteLine("无法识别BOSS。原始信息=" + e.ToString());
                            MsgMessage += new Message("无法识别BOSS。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else
                        {
                            intBossCode = intOutBC;
                        }
                    }
                    if (e.Contains("周目"))
                    {
                        if (!int.TryParse(e.Replace("周目", ""), out int intOutRound))
                        {
                            Console.WriteLine("无法识别周目。原始信息=" + e.ToString());
                            MsgMessage += new Message("无法识别周目。\r\n");
                            MsgMessage += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                            return;
                        }
                        else
                        {
                            intRound = intOutRound;
                        }
                    }
                }
                if (intBossCode > 0 || intRound > 0)
                {
                    if (RecordDAL.QueryDmgRecByBCnRound(intBossCode, intRound, strGrpID, out DataTable dtDmgRecBCR))
                    {
                        for (int i = 0; i < dtDmgRecBCR.Rows.Count; i++)
                        {
                            string strRUID = dtDmgRecBCR.Rows[i]["userid"].ToString();
                            string strRDmg = dtDmgRecBCR.Rows[i]["dmg"].ToString();
                            string strRRound = dtDmgRecBCR.Rows[i]["round"].ToString();
                            string strRBC = dtDmgRecBCR.Rows[i]["bc"].ToString();
                            string strREXT = dtDmgRecBCR.Rows[i]["extime"].ToString();
                            string strREID = dtDmgRecBCR.Rows[i]["eventid"].ToString();
                            string resultString = "";
                            if (dtDmgRecBCR.Rows[i]["dmg"].ToString() == "0")
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
                            else if (dtDmgRecBCR.Rows[i]["dmg"].ToString() != "0")
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
                                Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRecBCR.Rows[i]["dmg"].ToString());
                                MsgMessage += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            Console.WriteLine("E" + strREID + "：" + resultString + "\r\n");
                            MsgMessage += new Message("E" + strREID + "：" + resultString + "\r\n");
                        }
                    }
                }
            }
            else
            {
                MsgMessage += new Message("未能识别查询内容。\r\n本功能支持单独按档案号查询，或同时按BOSS与周目查询。");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }
        public static void RecordQuery(string strGrpID, string strUserID, string strCmdContext)
        {
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {

            }
        }

        /// <summary>
        /// 命令内容中含有的参数提取器
        /// </summary>
        /// <param name="strCmdContext"></param>
        public static bool CmdSpliter(string strCmdContext)
        {
            bool isCorrect = true;
            CommonVariables.IntEID = 0;
            CommonVariables.DouUID = 0;
            CommonVariables.IntBossCode = 0;
            CommonVariables.IntRound = 0;
            CommonVariables.IntDMG = 0;
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {
                if (e == "补时")
                {
                    CommonVariables.IntEXT = 1;
                }
                else if (e.Contains("周目"))
                {
                    if (!int.TryParse(e.Replace("周目", ""), out int intOutRound))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别周目数，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别周目数，请确保填入的周目数为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        if (intOutRound > ValueLimits.RoundLimitMax)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + "周目数过高，输入字串为：" + e.ToString() + "，上限值为：" + ValueLimits.RoundLimitMax.ToString());
                            MsgMessage += new Message("所填入的周目数(" + intOutRound.ToString() + ")高于目前设定的上限值（" + ValueLimits.RoundLimitMax.ToString() + "）。\r\n");
                            isCorrect = false;
                        }
                        else if (intOutRound < 1)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + "周目数过低，输入字串为：" + e.ToString());
                            MsgMessage += new Message("所填入的周目数（" + intOutRound.ToString() + "）低于有效值（1）。\r\n");
                            isCorrect = false;
                        }
                        else
                        {
                            CommonVariables.IntRound = intOutRound;
                        }
                    }
                }
                else if (e.ToLower().Contains("e"))
                {
                    if (!int.TryParse(e.ToLower().Replace("e", ""), out int intOutEID))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别档案号，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别档案号，请确保填入的档案号为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        CommonVariables.IntEID = intOutEID;
                    }
                }
                else if (e.ToLower().Contains("u"))
                {
                    if (!double.TryParse(e.ToLower().Replace("u", ""), out double douOutUID))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别QQ号，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别QQ号，请确保填入的QQ号为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        CommonVariables.DouUID = douOutUID;
                    }
                }
                else if (e.ToLower().Contains("b"))
                {
                    if (!int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别BOSS编号，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别BOSS，请确保填入的BOSS编号为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        if (intOutBC > ValueLimits.BossLimitMax)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + "BOSS编号超限，输入字串为：" + e.ToString() + "，上限值为：" + ValueLimits.BossLimitMax.ToString());
                            MsgMessage += new Message("所填入的BOSS编号(" + intOutBC.ToString() + ")高于目前设定的上限值（" + ValueLimits.BossLimitMax.ToString() + "）。\r\n");
                            isCorrect = false;
                        }
                        else if (intOutBC < 1)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + "周目数过低，输入字串为：" + e.ToString());
                            MsgMessage += new Message("所填入的周目数（" + intOutBC.ToString() + "）低于有效值（1）。\r\n");
                            isCorrect = false;
                        }
                        else
                        {
                            CommonVariables.IntBossCode = intOutBC;
                        }
                    }
                }
                else if (e.ToLower().Contains("w") || e.Contains("万"))
                {
                    if (!DamageAnalyzation(2, e, out int intResultDamage))
                    {
                        isCorrect = false;
                    }
                    else
                    {
                        CommonVariables.IntDMG = intResultDamage;
                    }
                }
                else if (e.ToLower().Contains("k"))
                {
                    if (!DamageAnalyzation(1, e, out int intResultDamage))
                    {
                        isCorrect = false;
                    }
                    else
                    {
                        CommonVariables.IntDMG = intResultDamage;
                    }
                }
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 1)
                {
                    if (!DamageAnalyzation(0, e, out int intResultDamage))
                    {
                        isCorrect = false;
                    }
                    else
                    {
                        CommonVariables.IntDMG = intResultDamage;
                    }
                }
            }
            return isCorrect;
        }
    }
}