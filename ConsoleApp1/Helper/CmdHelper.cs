using System;
using System.Collections.Generic;
using System.Text;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using System.Text.RegularExpressions;
using Marchen.DAL;
using System.Data;

namespace Marchen.BLL
{
    class CmdHelper : GroupMsgBLL
    {
        /// <summary>
        /// 读取数据库中储存的上限值
        /// </summary>
        public static bool LoadValueLimits()
        {
            bool isCorrect = false;
            if (RecordDAL.QueryLimits(out DataTable dtLimits))
            {
                if (dtLimits.Rows.Count > 0)
                {
                    for (int i = 0; i < dtLimits.Rows.Count; i++)
                    {
                        if (dtLimits.Rows[i]["TYPE"].ToString() == "DAMAGE_MAX")
                        {
                            ValueLimits.DamageLimitMax = int.Parse(dtLimits.Rows[i]["VALUE"].ToString());
                        }
                        if (dtLimits.Rows[i]["TYPE"].ToString() == "ROUND_MAX")
                        {
                            ValueLimits.RoundLimitMax = int.Parse(dtLimits.Rows[i]["VALUE"].ToString());
                        }
                        if (dtLimits.Rows[i]["TYPE"].ToString() == "BOSS_MAX")
                        {
                            ValueLimits.BossLimitMax = int.Parse(dtLimits.Rows[i]["VALUE"].ToString());
                        }
                    }
                    if (ValueLimits.DamageLimitMax == 0)
                    {
                        Console.WriteLine("未能获取伤害上限，请检查TTL_LIMITS表中是否有DAMAGE_MAX项");
                        //return false;
                    }
                    else if (ValueLimits.RoundLimitMax == 0)
                    {
                        Console.WriteLine("未能获取周目上限，请检查TTL_LIMITS表中是否有ROUND_MAX项");
                        //return false;
                    }
                    else if (ValueLimits.BossLimitMax == 0)
                    {
                        Console.WriteLine("未能获取BOSS编号上限，请检查TTL_LIMITS表中是否有BOSS_MAX项");
                        //return false;
                    }
                    else
                    {
                        Console.WriteLine("获取上限值成功，以下是获取到的上限值：");
                        Console.WriteLine("伤害上限：" + ValueLimits.DamageLimitMax.ToString());
                        Console.WriteLine("周目上限：" + ValueLimits.RoundLimitMax.ToString());
                        Console.WriteLine("BOSS编号上限：" + ValueLimits.BossLimitMax.ToString());
                        isCorrect = true;
                    }
                }
                else
                {
                    Console.WriteLine("向数据库读取上限值时无返回条目，请检查TTL_LIMITS表。");
                    //return false;
                }
            }
            return isCorrect;
        }

        /// <summary>
        /// 获取指定日期的0点时间
        /// </summary>
        /// <param name="datetime">指定日期</param>
        /// <returns>返回指定日期的0点时间</returns>
        public static DateTime GetZeroTime(DateTime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, datetime.Day);
        }

        /// <summary>
        /// 提取命令内容的方法
        /// </summary>
        /// <param name="strCmdContext"></param>
        public static bool CmdSpliter(string strCmdContext)
        {
            Console.WriteLine("开始拆分元素");
            bool isCorrect = true;
            InputVariables.IntEID = -1;
            InputVariables.DouUID = -1;
            InputVariables.IntBossCode = -1;
            InputVariables.IntRound = -1;
            InputVariables.IntDMG = -1;
            InputVariables.IntEXT = -1;
            //CommonVariables.IntSubsType = -1;
            InputVariables.IntTimeOutFlag = 0;
            InputVariables.IntIsAllFlag = 0;
            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string e in sArray)
            {
                if (e == "补时" || e.ToLower() == "ext")
                {
                    InputVariables.IntEXT = 1;
                }
                else if (e == "非补时" || e.ToLower() == "noext" || e == "非尾刀" || e.ToLower() == "nolast" || e.ToLower() == "normal")
                {
                    InputVariables.IntEXT = 0;
                }
                else if (e == "尾刀" || e.ToLower() == "last")
                {
                    InputVariables.IntEXT = 2;
                }
                else if (e == "掉线" || e.ToLower() == "timeout")
                {
                    InputVariables.IntTimeOutFlag = 1;
                }
                else if (e == "全部" || e.ToLower() == "all")
                {
                    InputVariables.IntIsAllFlag = 1;
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
                            InputVariables.IntRound = int.Parse(Regex.Replace(intOutRound.ToString(), @"[^\d.\d]", ""));
                        }
                    }
                }
                else if (e.ToLower().Contains("r"))
                {
                    if (!int.TryParse(e.ToLower().Replace("r", ""), out int intOutRound))
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
                            InputVariables.IntRound = int.Parse(Regex.Replace(intOutRound.ToString(), @"[^\d.\d]", ""));
                        }
                    }
                }
                else if (e.ToLower().Contains("e") && !e.ToLower().Contains("ext"))
                {
                    if (!int.TryParse(e.ToLower().Replace("e", ""), out int intOutEID))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别档案号，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别档案号，请确保填入的档案号为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        if (intOutEID < 1)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + "无法识别档案号，元素为：" + e.ToString());
                            MsgMessage += new Message("所填入的档案号（" + intOutEID + "）低于有效值（1）。\r\n");
                            isCorrect = false;
                        }
                        else
                        {
                            InputVariables.IntEID = intOutEID;
                        }
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
                        InputVariables.DouUID = double.Parse(Regex.Replace(douOutUID.ToString(), @"[^\d.\d]", ""));
                    }
                }
                else if (e.Contains("[CQ:at,qq=") && e.Contains("]"))
                {
                    if (!double.TryParse(e.Substring(e.IndexOf("=") + 1, e.IndexOf("]") - (e.IndexOf("=") + 1)), out double douOutUID))
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "无法识别QQ号，元素为：" + e.ToString());
                        MsgMessage += new Message("无法识别QQ号，请确保填入的QQ号为数字。\r\n");
                        isCorrect = false;
                    }
                    else
                    {
                        InputVariables.DouUID = double.Parse(Regex.Replace(douOutUID.ToString(), @"[^\d.\d]", ""));
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
                            InputVariables.IntBossCode = int.Parse(Regex.Replace(intOutBC.ToString(), @"[^\d.\d]", ""));
                        }
                    }
                }
                else if (e.ToLower().Contains("w"))
                {
                    if (!DamageAnalyzation(2, e.ToLower().Replace("w", "")))
                    {
                        isCorrect = false;
                    }
                }
                else if (e.Contains("万"))
                {
                    if (!DamageAnalyzation(2, e.Replace("万", "")))
                    {
                        isCorrect = false;
                    }
                }
                else if (e.ToLower().Contains("k"))
                {
                    if (!DamageAnalyzation(1, e.ToLower().Replace("k", "")))
                    {
                        isCorrect = false;
                    }
                }
                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 0)
                {
                    if (!DamageAnalyzation(0, e))
                    {
                        isCorrect = false;
                    }
                }
            }
            Console.WriteLine("完成元素拆分\r\n结果：isCorrect=" + isCorrect.ToString());
            Console.WriteLine("IntEID=" + InputVariables.IntEID);
            Console.WriteLine("DouUID=" + InputVariables.DouUID);
            Console.WriteLine("IntBossCode=" + InputVariables.IntBossCode);
            Console.WriteLine("IntRound=" + InputVariables.IntRound);
            Console.WriteLine("IntDMG=" + InputVariables.IntDMG);
            Console.WriteLine("IntEXT=" + InputVariables.IntEXT);
            return isCorrect;
        }

        /// <summary>
        /// 伤害值的转换，以及有效性检测
        /// </summary>
        /// <param name="intUnitType"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool DamageAnalyzation(int intUnitType, string e)
        {
            int intMultiplier = 0;
            if (intUnitType == 0)
            {
                //Console.WriteLine("伤害值单位为无");
                intMultiplier = 1;
            }
            else if (intUnitType == 1)
            {
                //Console.WriteLine("伤害值单位为千");
                intMultiplier = 1000;
            }
            else if (intUnitType == 2)
            {
                //Console.WriteLine("伤害值单位为万");
                intMultiplier = 10000;
            }
            else
            {
                Console.WriteLine("处理伤害值时收到了非指定的倍率类型，值为：" + intUnitType.ToString());
                InputVariables.IntDMG = -1;
                return false;
            }
            Regex rgxPattern = new Regex(@"^\d+(\.\d+)?$");//检查取到的伤害值是否为正浮点数
            if (!rgxPattern.IsMatch(e.ToString()))
            {
                Console.WriteLine("无法识别伤害，输入值为：" + e);
                MsgMessage += new Message("无法识别伤害，请检查输入的伤害值(" + e + ")。\r\n");
                InputVariables.IntDMG = -1;
                return false;
            }
            else if (!decimal.TryParse(Regex.Replace(e, @"[^\d.\d]", ""), out decimal dclOutDamage))
            {
                Console.WriteLine("无法识别伤害，输入值为：" + e);
                MsgMessage += new Message("无法识别伤害，请检查输入的伤害值(" + e + ")。\r\n");
                InputVariables.IntDMG = -1;
                return false;
            }
            else
            {
                if (!int.TryParse(decimal.Round(dclOutDamage * intMultiplier, 0).ToString(), out int intOutDamage))
                {
                    Console.WriteLine("无法识别伤害，输入值为：" + e);
                    MsgMessage += new Message("无法识别伤害，请检查输入的伤害值(" + dclOutDamage.ToString() + ")。\r\n");
                    InputVariables.IntDMG = -1;
                    return false;
                }
                else
                {
                    if (intOutDamage > ValueLimits.DamageLimitMax)
                    {
                        Console.WriteLine("伤害值超出可信范围，输入字串为：" + e + "，上限值为：" + ValueLimits.DamageLimitMax.ToString());
                        MsgMessage += new Message("所填入的伤害值(" + intOutDamage.ToString() + ")高于目前设定的上限值（" + ValueLimits.DamageLimitMax.ToString() + "）。\r\n");
                        InputVariables.IntDMG = -1;
                        return false;
                    }
                    else if (intOutDamage < 1)
                    {
                        Console.WriteLine("伤害值过低，输入字串为：" + e);
                        MsgMessage += new Message("所填入的伤害值（" + intOutDamage.ToString() + "）低于有效值（1），如为掉线请使用掉线指令记录。\r\n");
                        InputVariables.IntDMG = -1;
                        return false;
                    }
                    else
                    {
                        InputVariables.IntDMG = intOutDamage;
                        Console.WriteLine("伤害识别与转换完成，输出为：" + InputVariables.IntDMG);
                        return true;
                    }
                }
            }
        }
    }
}
