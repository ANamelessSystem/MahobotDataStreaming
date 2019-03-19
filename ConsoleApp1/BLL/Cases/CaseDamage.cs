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
    class CaseDamage:GroupMsgBLL
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
                intMultiplier = 1;
            }
            else if (intMultiplier == 1)
            {
                intMultiplier = 1000;
            }
            else if (intMultiplier == 2)
            {
                intMultiplier = 10000;
            }
            else
            {
                throw new Exception("处理伤害值时收到了非指定的倍率类型");
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
                if (!int.TryParse(decimal.Round((dclOutDamage * intMultiplier), 0).ToString(), out int intOutDamage))
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
        public static void DmgRecAdd(string strGrpID, string strUserID,string strCmdContext)
        {
            bool isCorrect = true;
            int intBossCode = 0;
            int intRound = 0;
            int intDMG = -1;
            int intExTime = 0;
            if (!DmgTblCheck(strGrpID))
            {
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {
                if (e.ToLower() == "b1" || e.ToLower() == "b2" || e.ToLower() == "b3" || e.ToLower() == "b4" || e.ToLower() == "b5")
                {
                    if (!int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                    {
                        Console.WriteLine("无法识别BOSS代码，输入字串为：" + strCmdContext);
                        MsgMessage += new Message("无法识别BOSS代码，请确保填入为b1~b5\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        intBossCode = intOutBC;
                    }
                }
                else if (e == "补时")
                {
                    intExTime = 1;
                }
                else if (e.Contains("周目"))
                {
                    if (!int.TryParse(e.Replace("周目", ""), out int intOutRound))
                    {
                        Console.WriteLine("无法识别周目数，输入字串为：" + strCmdContext);
                        MsgMessage += new Message("无法识别周目数，请确保填入的周目数为阿拉伯数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        if (intOutRound > ValueLimits.RoundLimitMax)
                        {
                            Console.WriteLine("周目数过高，输入字串为：" + strCmdContext + "，上限值为：" + ValueLimits.RoundLimitMax.ToString());
                            MsgMessage += new Message("所填入的周目数(" + intOutRound.ToString() + ")高于目前设定的上限值（" + ValueLimits.RoundLimitMax.ToString() + "）。\r\n");
                            isCorrect = false;
                        }
                        else if (intOutRound < 1)
                        {
                            Console.WriteLine("周目数过低，输入字串为：" + strCmdContext);
                            MsgMessage += new Message("所填入的周目数（" + intOutRound.ToString() + "）低于有效值（1）。\r\n");
                            isCorrect = false;
                        }
                        else
                        {
                            intRound = intOutRound;
                        }
                    }
                }
                else if (e.ToLower().Contains("w") || e.Contains("万"))
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        isCorrect = false;
                    }
                }
                else if (e.ToLower().Contains("k"))
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        isCorrect = false;
                    }
                }
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 1)
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage))
                    {
                        intDMG = intResultDamage;
                    }
                    else
                    {
                        isCorrect = false;
                    }
                }
            }
            if (intDMG == -1)
            {
                MsgMessage += new Message("可能由于格式错误，未能从输入中识别伤害。\r\n");
                isCorrect = false;
            }
            if (intBossCode == 0)
            {
                MsgMessage += new Message("可能由于格式错误，未能从输入中识别BOSS代码。\r\n");
                isCorrect = false;
            }
            if (intRound == 0)
            {
                MsgMessage += new Message("可能由于格式错误，未能从输入中识别周目。\r\n");
                isCorrect = false;
            }
            if (isCorrect)
            {
                if (RecordDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode, intExTime, out int intEID))
                {
                    Console.WriteLine("伤害已保存，档案号=" + intEID.ToString() + "，B" + intBossCode.ToString() + "，" + intRound.ToString() + "周目，数值：" + intDMG.ToString() + "，补时标识：" + intExTime);
                    MsgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
                    CaseQueue.QueueQuit(strGrpID, strUserID);
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，伤害保存失败。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 记录掉线
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strCmdContext"></param>
        public static void DmgTimeOut(string strGrpID, string strUserID, string strCmdContext)
        {
            var msgMessage = new Message("");
            int intDMG = 0;
            int intRound = 0;
            int intBossCode = 0;
            int intExTime = 0;
            if (strCmdContext.Contains("补时"))
            {
                intExTime = 1;
            }
            if (RecordDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode, intExTime, out int intEID))
            {
                msgMessage = new Message("掉线已记录，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                CaseQueue.QueueQuit(strGrpID, strUserID);
            }
            else
            {
                msgMessage += new Message("与数据库失去连接，掉线记录失败。\r\n");
                msgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), msgMessage).Wait();
            }
        }

        /// <summary>
        /// 
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
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 1)
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
    }
}
