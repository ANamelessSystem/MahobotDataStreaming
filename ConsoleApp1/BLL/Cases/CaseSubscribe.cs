using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using Sisters.WudiLib.Responses;


namespace Marchen.BLL
{
    class CaseSubscribe : GroupMsgBLL
    {
        /// <summary>
        /// 订阅Boss提醒（普通订阅）（用户调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">用户输入的命令内容</param>
        public static void SubsAdd(string strGrpID, string strUserID, string strCmdContext)
        {
            int intMemberStatus = NameListDAL.MemberCheck(strGrpID, strUserID);
            int intSubsType = 0;
            if (intMemberStatus == 0)
            {
                MsgMessage += new Message("尚未报名，订阅BOSS。\r\n");
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
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                //MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
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
            if (InputVariables.IntEXT == -1)
            {
                InputVariables.IntEXT = 0;
            }
            if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
            {
                DataRow[] drExistsBoss = dtSubsStatus.Select("BC='" + InputVariables.IntBossCode + "'");
                int isLastAtk = 0;
                if (drExistsBoss.Length == 0)
                {
                    if (InputVariables.IntEXT != 0)
                    {
                        DataRow[] drExistsExtSubs = dtSubsStatus.Select("SUBSTYPE='1'");
                        RecordDAL.CheckLastAttack(strGrpID, strUserID, out isLastAtk);
                        if (isLastAtk == 1 && drExistsExtSubs.Length == 0)
                        {
                            //满足唯一补时刀持有条件
                            intSubsType = 1;
                        }
                        else if (isLastAtk == 1 && drExistsExtSubs.Length != 0)
                        {
                            //改定为其他BOSS为尾刀订阅
                            intSubsType = 1;
                            if (SubscribeDAL.UpdateChangeExtSubs(strGrpID, strUserID,InputVariables.IntBossCode))
                            {
                                //订阅成功
                                MsgMessage += new Message("成功将补时刀订阅改为B" + InputVariables.IntBossCode + "。\r\n");
                            }
                            else
                            {
                                //数据库失败
                                MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
                            }
                        }
                        else
                        {
                            MsgMessage += new Message("上一刀不是尾刀，无法添加补时刀订阅。\r\n");
                        }
                    }
                    else
                    {
                        if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, InputVariables.IntBossCode, intSubsType))
                        {
                            MsgMessage += new Message("已成功订阅B" + InputVariables.IntBossCode + "。\r\n");
                        }
                        else
                        {
                            MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
                        }
                    }
                }
                else
                {
                    MsgMessage += new Message("已订阅过本BOSS，无法重复订阅，如需改为补刀订阅请先取消后再重新订阅。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，订阅BOSS失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        ///// <summary>
        ///// 订阅Boss提醒（尾刀专用）（自动调用）
        ///// </summary>
        ///// <param name="strGrpID">群号</param>
        ///// <param name="strUserID">QQ号</param>
        ///// <param name="intBossCode">BOSS代码</param>
        //public static void SubsAdd(string strGrpID, string strUserID, int intBossCode, int intRound)
        //{
        //    int intSubsType = 1;
        //    if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
        //    {
        //        DataRow[] drExistsBoss = dtSubsStatus.Select("BC='" + intBossCode + "'");
        //        if (drExistsBoss.Length == 0)
        //        {
        //            if (SubscribeDAL.AddBossSubs(strGrpID, strUserID, intBossCode, intSubsType, intRound))
        //            {
        //                MsgMessage += new Message("【已自动将补时刀订阅至下周目B" + intBossCode + "。】\r\n");

        //                Console.WriteLine("成功添加补时刀订阅");
        //            }
        //            else
        //            {
        //                MsgMessage += new Message("【补时刀自动订阅失败。】\r\n");
        //                Console.WriteLine("补时刀订阅时数据库失败");
        //            }
        //        }
        //        else
        //        {
        //            //boss already subs,just change substype
        //            if (SubscribeDAL.UpdateSubsType(strGrpID, strUserID, intRound, intBossCode, intSubsType))
        //            {
        //                MsgMessage += new Message("【已自动将补时刀订阅至下周目B" + intBossCode + "。】\r\n");
        //                Console.WriteLine("成功通过修改添加补时刀订阅");
        //            }
        //            else
        //            {
        //                MsgMessage += new Message("【补时刀自动订阅失败。】\r\n");
        //                Console.WriteLine("补时刀订阅时数据库失败");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        MsgMessage += new Message("补时刀自动订阅失败。\r\n");
        //        Console.WriteLine("获取订阅时数据库失败");
        //    }
        //}

        /// <summary>
        /// 查看已订阅的BOSS
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        public static void SubsShow(string strGrpID, string strUserID, string strCmdContext)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                //MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (InputVariables.IntIsAllFlag == 0)
            {
                if (SubscribeDAL.GetSubsStatus(strGrpID, strUserID, out DataTable dtSubsStatus))
                {
                    string strOutput = "";
                    for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                    {
                        if (i != 0)
                        {
                            strOutput += "、";
                        }
                        strOutput += "B" + dtSubsStatus.Rows[i]["BC"];
                        if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                        {
                            strOutput += "(补时)";
                        }
                    }
                    MsgMessage += new Message("目前正在订阅的BOSS为：" + strOutput + "\r\n");
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查看已订阅BOSS失败。\r\n");
                }
            }
            else
            {
                if (SubscribeDAL.GetSubsStatus(strGrpID, out DataTable dtSubsStatus))
                {
                    string strOutputB1 = "";
                    string strOutputB2 = "";
                    string strOutputB3 = "";
                    string strOutputB4 = "";
                    string strOutputB5 = "";
                    int intCountB1 = 0;
                    int intCountB2 = 0;
                    int intCountB3 = 0;
                    int intCountB4 = 0;
                    int intCountB5 = 0;
                    for (int i = 0; i < dtSubsStatus.Rows.Count; i++)
                    {
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "1")
                        {
                            intCountB1 += 1;
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutputB1 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ") (补时刀)";
                            }
                            else
                            {
                                strOutputB1 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")";
                            }
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "2")
                        {
                            intCountB2 += 1;
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutputB2 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ") (补时刀)";
                            }
                            else
                            {
                                strOutputB2 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")";
                            }
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "3")
                        {
                            intCountB3 += 1;
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutputB3 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ") (补时刀)";
                            }
                            else
                            {
                                strOutputB3 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")";
                            }
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "4")
                        {
                            intCountB4 += 1;
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutputB4 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ") (补时刀)";
                            }
                            else
                            {
                                strOutputB4 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")";
                            }
                        }
                        if (dtSubsStatus.Rows[i]["BC"].ToString() == "5")
                        {
                            intCountB5 += 1;
                            if (dtSubsStatus.Rows[i]["SUBSTYPE"].ToString() == "1")
                            {
                                strOutputB5 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ") (补时刀)";
                            }
                            else
                            {
                                strOutputB5 += "\r\n" + dtSubsStatus.Rows[i]["MBRNAME"].ToString() + "(" + dtSubsStatus.Rows[i]["USERID"].ToString() + ")";
                            }
                        }
                    }
                    if (strOutputB1 == "" || strOutputB1 == null)
                    {
                        strOutputB1 = "\r\n无";
                    }
                    if (strOutputB2 == "" || strOutputB2 == null)
                    {
                        strOutputB2 = "\r\n无";
                    }
                    if (strOutputB3 == "" || strOutputB3 == null)
                    {
                        strOutputB3 = "\r\n无";
                    }
                    if (strOutputB4 == "" || strOutputB4 == null)
                    {
                        strOutputB4 = "\r\n无";
                    }
                    if (strOutputB5 == "" || strOutputB5 == null)
                    {
                        strOutputB5 = "\r\n无";
                    }
                    MsgMessage += new Message("正在订阅B1(" + intCountB1 + "人)：" + strOutputB1 + "\r\n正在订阅B2(" + intCountB2 + "人)：" + strOutputB2 + "\r\n正在订阅B3(" + intCountB3 + "人)：" + strOutputB3 + "\r\n正在订阅B4(" + intCountB4 + "人)：" + strOutputB4 + "\r\n正在订阅B5(" + intCountB5 + "人)：" + strOutputB5 + "\r\n");
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查看已订阅BOSS失败。\r\n");
                }
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }


        /// <summary>
        /// 取消boss订阅（用户调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="strCmdContext">用户输入的命令内容</param>
        public static void SubsDel(string strGrpID, string strUserID, string strCmdContext)
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
            if (SubscribeDAL.DelBossSubs(strGrpID, strUserID, InputVariables.IntBossCode, out int intDelCount))
            {
                if (intDelCount > 0)
                {
                    MsgMessage += new Message("退订B"+ InputVariables.IntBossCode + "成功。\r\n");
                }
                else
                {
                    MsgMessage += new Message("尚未订阅该BOSS。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退订失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 取消boss订阅（自动调用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intBossCode">BOSS代码</param>
        public static void SubsDel(string strGrpID, string strUserID, int intBossCode)
        {
            if (SubscribeDAL.DelBossSubs(strGrpID, strUserID, intBossCode, out int intDelCount))
            {
                if (intDelCount > 0)
                {
                    MsgMessage += new Message("【已自动退订B" + intBossCode + "。】\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退订失败。\r\n");
            }
        }
    }
}
