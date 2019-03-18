using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using System.Text.RegularExpressions;

namespace Marchen.BLL
{
    class CaseDamage
    {
        /// <summary>
        /// 检查数据库的后台表
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="outMsgMessage"></param>
        /// <returns></returns>
        public static bool DmgTblCheck(string strGrpID, string strUserID, out Message outMsgMessage)
        {
            Message msgMessage = new Message("");
            if (RecordDAL.CheckClanDmgTable(strGrpID, out DataTable dtTableCount))
            {
                if (int.Parse(dtTableCount.Rows[0]["count"].ToString()) == 0)
                {
                    if (RecordDAL.CreateTablesForGuildDamage(strGrpID))
                    {
                        Console.WriteLine("已成功为公会群" + strGrpID + "建立伤害表。");
                        msgMessage += new Message("(未找到本公会伤害后台数据表，已自动建立。)\r\n");
                        outMsgMessage = msgMessage;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("为公会群" + strGrpID + "建立伤害表过程中失败。");
                        msgMessage += new Message("(公会伤害后台数据表建立失败。)\r\n");
                        outMsgMessage = msgMessage;
                        return false;
                    }
                }
                else
                {
                    outMsgMessage = msgMessage;
                    return true;
                }
            }
            else
            {
                Console.WriteLine("读取公会群" + strGrpID + "的伤害表失败。");
                msgMessage += new Message("与数据库失去连接，读取本公会伤害表失败。\r\n");
                outMsgMessage = msgMessage;
                return false;
            }
        }

        /// <summary>
        /// 伤害检测、转换
        /// </summary>
        /// <param name="intUnitType"></param>
        /// <param name="e"></param>
        /// <param name="intResultDamage"></param>
        /// <param name="outMsgMessage"></param>
        /// <returns></returns>
        public static bool DamageAnalyzation(int intUnitType, string e, out int intResultDamage,out Message outMsgMessage)
        {
            var msgMessage = new Message("");
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
                msgMessage += new Message("无法识别伤害，请检查输入的伤害值。\r\n");
                intResultDamage = -1;
                outMsgMessage = msgMessage;
                return false;
            }
            else
            {
                if (!int.TryParse(decimal.Round((dclOutDamage * intMultiplier), 0).ToString(), out int intOutDamage))
                {
                    Console.WriteLine("无法识别伤害，输入值为：" + e);
                    msgMessage += new Message("无法识别伤害，请检查输入的伤害值。\r\n");
                    intResultDamage = -1;
                    outMsgMessage = msgMessage;
                    return false;
                }
                else
                { 
                    if (intOutDamage > ValueLimits.DamageLimitMax)
                    {
                        Console.WriteLine("伤害值超出可信范围，输入字串为：" + e + "，上限值为：" + ValueLimits.DamageLimitMax.ToString());
                        msgMessage += new Message("所填入的伤害值(" + intOutDamage.ToString() + ")高于目前设定的上限值（" + ValueLimits.DamageLimitMax.ToString() + "）。\r\n");
                        intResultDamage = -1;
                        outMsgMessage = msgMessage;
                        return false;
                    }
                    else if (intOutDamage < 1)
                    {
                        Console.WriteLine("伤害值过低，输入字串为：" + e);
                        msgMessage += new Message("所填入的伤害值（" + intOutDamage.ToString() + "）低于有效值（1）。\r\n");
                        intResultDamage = -1;
                        outMsgMessage = msgMessage;
                        return false;
                    }
                    else
                    {
                        intResultDamage = intOutDamage;
                        outMsgMessage = msgMessage;
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
            var msgMessage = new Message("");
            bool isCorrect = true;
            int intBossCode = 0;
            int intRound = 0;
            int intDMG = -1;
            int intExTime = 0;
            if (DmgTblCheck(strGrpID, strUserID, out Message outMsgMessageFromTableCheck))
            {
                msgMessage += outMsgMessageFromTableCheck;
            }
            else
            {
                msgMessage += outMsgMessageFromTableCheck;
                msgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), msgMessage).Wait();
                return;
            }
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {
                if (e.ToLower() == "b1" || e.ToLower() == "b2" || e.ToLower() == "b3" || e.ToLower() == "b4" || e.ToLower() == "b5")
                {
                    if (!int.TryParse(e.ToLower().Replace("e", ""), out int intOutBC))
                    {
                        Console.WriteLine("无法识别BOSS代码，输入字串为：" + strCmdContext);
                        msgMessage += new Message("无法识别BOSS代码，请确保填入为b1~b5\r\n");
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
                        msgMessage += new Message("无法识别周目数，请确保填入的周目数为阿拉伯数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        if (intOutRound > ValueLimits.RoundLimitMax)
                        {
                            Console.WriteLine("周目数过高，输入字串为：" + strCmdContext + "，上限值为：" + ValueLimits.RoundLimitMax.ToString());
                            msgMessage += new Message("所填入的周目数(" + intOutRound.ToString() + ")高于目前设定的上限值（" + ValueLimits.RoundLimitMax.ToString() + "）。\r\n");
                            isCorrect = false;
                        }
                        else if (intOutRound < 1)
                        {
                            Console.WriteLine("周目数过低，输入字串为：" + strCmdContext);
                            msgMessage += new Message("所填入的周目数（" + intOutRound.ToString() + "）低于有效值（1）。\r\n");
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
                    if (DamageAnalyzation(2, e, out int intResultDamage, out Message outMsgMessageFromDamageAnalyzation))
                    {
                        intDMG = intResultDamage;
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                    }
                    else
                    {
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                        isCorrect = false;
                    }
                }
                else if (e.ToLower().Contains("k"))
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage, out Message outMsgMessageFromDamageAnalyzation))
                    {
                        intDMG = intResultDamage;
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                    }
                    else
                    {
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                        isCorrect = false;
                    }
                }
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 1)
                {
                    if (DamageAnalyzation(2, e, out int intResultDamage, out Message outMsgMessageFromDamageAnalyzation))
                    {
                        intDMG = intResultDamage;
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                    }
                    else
                    {
                        msgMessage += outMsgMessageFromDamageAnalyzation;
                        isCorrect = false;
                    }
                }
            }
            if (intDMG == -1)
            {
                msgMessage += new Message("无法识别伤害，可能由于格式错误\r\n");
                isCorrect = false;
            }
            if (intBossCode == 0)
            {
                msgMessage += new Message("无法识别BOSS代码，可能由于格式错误\r\n");
                isCorrect = false;
            }
            if (intRound == 0)
            {
                msgMessage += new Message("无法识别周目数，可能由于格式错误\r\n");
                isCorrect = false;
            }
            if (isCorrect)
            {
                if (RecordDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode, intExTime, out int intEID))
                {
                    Console.WriteLine("伤害已保存，档案号=" + intEID.ToString() + "，B" + intBossCode.ToString() + "，" + intRound.ToString() + "周目，数值：" + intDMG.ToString() + "，补时标识：" + intExTime);
                    msgMessage = new Message("伤害已保存，档案号=" + intEID.ToString() + "\r\n");
                    CaseQueue.QueueQuit(strGrpID, strUserID);
                }
                else
                {
                    msgMessage += new Message("与数据库失去连接，伤害保存失败。\r\n");
                }
            }
            else
            {
                msgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
            }
            msgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), msgMessage).Wait();
        }
    }
}
