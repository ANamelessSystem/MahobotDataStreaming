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
    class CaseRemind : GroupMsgBLL
    {
        /// <summary>
        /// At未出满三刀的成员
        /// </summary>
        /// <param name="strGrpID">消息发起人所属群号</param>
        /// <param name="strUserID">消息发起人QQ号</param>
        /// <param name="memberInfo">消息发起人的成员资料</param>
        public static void NoticeRemainStrikers(string strGrpID, string strUserID, GroupMemberInfo memberInfo)
        {
            if (!(memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager))
            {
                MsgMessage += new Message("拒绝：仅有管理员或群主可执行出刀提醒指令。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (!ClanInfoDAL.GetClanTimeOffset(strGrpID, out int intHourSet))
            {
                MsgMessage += new Message("与数据库失去连接，查询区域时间设定失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            //if (intHourSet < 0)
            //{
            //    MsgMessage += new Message("每日更新小时设定小于0，尚未验证这种形式的时间格式是否正常，已退回本功能。\r\n");
            //    MsgMessage += Message.At(long.Parse(strUserID));
            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //    return;
            //}
            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(intHourSet);//每天凌晨4点或5点开始
                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(intHourSet);//第二天凌晨4点或5点结束
                if (dtNow.Hour >= 0 && dtNow.Hour < intHourSet)
                {
                    //0点后日期变换，开始日期需切换到昨天的更新时间点
                    dtStart = dtStart.AddDays(-1);
                    dtEnd = dtEnd.AddDays(-1);
                }
                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                {
                    MsgMessage += new Message("请以下成员尽早出刀：");
                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                    {
                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
                        int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
                        int intCountLastAtk = int.Parse(dtInsuff.Rows[i]["cla"].ToString());
                        int intCountExTime = int.Parse(dtInsuff.Rows[i]["cex"].ToString());
                        if ((intCountMain + intCountLastAtk) < 3)
                        {
                            if (intCountLastAtk > intCountExTime)
                            {
                                MsgMessage += new Message("\r\nID：" + strUID + "，剩余" + (3 - (intCountMain + intCountLastAtk)).ToString() + "刀与补时刀 ") + Message.At(long.Parse(strUID));
                            }
                            else
                            {
                                MsgMessage += new Message("\r\nID：" + strUID + "，剩余" + (3 - (intCountMain + intCountLastAtk)).ToString() + "刀 ") + Message.At(long.Parse(strUID));
                            }
                        }
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
            }
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 查询成员出刀情况
        /// </summary>
        /// <param name="strGrpID">消息发起人所属群号</param>
        /// <param name="strUserID">消息发起人QQ号</param>
        /// <param name="memberInfo">消息发起人的成员资料</param>
        /// <param name="intType">命令种类：1=只显示不提醒；2=提醒全部没满三刀的；3=只提醒三刀都没出的</param>

        //public static void RemainStrikes(string strGrpID, string strUserID,GroupMemberInfo memberInfo,int intType)
        //{
        //    if (!ClanInfoDAL.GetClanTimeOffset(strGrpID, out int intHourSet))
        //    {
        //        MsgMessage += new Message("与数据库失去连接，查询区域时间设定失败。\r\n");
        //        MsgMessage += Message.At(long.Parse(strUserID));
        //        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        //        return;
        //    }
        //    if (intHourSet < 0)
        //    {
        //        MsgMessage += new Message("每日更新小时设定小于0，尚未验证这种形式的时间格式是否正常，已退回本功能。\r\n");
        //        MsgMessage += Message.At(long.Parse(strUserID));
        //        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        //        return;
        //    }
        //    if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
        //    {
        //        DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
        //        DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(intHourSet);//每天凌晨4点开始
        //        DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(intHourSet);//第二天凌晨4点结束
        //        if (dtNow.Hour >= 0 && dtNow.Hour < intHourSet)
        //        {
        //            dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
        //            dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
        //        }
        //        if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
        //        {
        //            //MsgMessage += new Message("截至目前尚有余刀的成员：");
        //            //int intCount = 0;
        //            //string strLeft1 = "";
        //            //string strLeft2 = "";
        //            //string strLeft3 = "";
        //            //for (int i = 0; i < dtInsuff.Rows.Count; i++)
        //            //{
        //            //    string strUID = dtInsuff.Rows[i]["userid"].ToString();
        //            //    int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
        //            //    if (intCountMain == 2)
        //            //    {
        //            //        intCount += 1;
        //            //        strLeft1 += "\r\nID：" + strUID + "，剩余1刀";
        //            //    }
        //            //    if (intCountMain == 1)
        //            //    {
        //            //        intCount += 2;
        //            //        strLeft2 += "\r\nID：" + strUID + "，剩余2刀";
        //            //    }
        //            //    if (intCountMain == 0)
        //            //    {
        //            //        intCount += 3;
        //            //        strLeft3 += "\r\nID：" + strUID + "，剩余3刀";
        //            //    }
        //            //}
        //            //MsgMessage += new Message(strLeft1 + "\r\n--------------------" + strLeft2 + "\r\n--------------------" + strLeft3);
        //            //MsgMessage += new Message("\r\n合计剩余" + intCount.ToString() + "刀");
        //        }
        //        else
        //        {
        //            //MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
        //            //MsgMessage += Message.At(long.Parse(strUserID));
        //        }
        //    }
        //    else
        //    {
        //        //MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
        //        //MsgMessage += Message.At(long.Parse(strUserID));
        //    }
        //    //ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        //}



        /// <summary>
        /// 查询未出满三刀的成员
        /// </summary>
        /// <param name="strGrpID">消息发起人所属群号</param>
        /// <param name="strUserID">消息发起人QQ号</param>
        public static void ShowRemainStrikes(string strGrpID, string strUserID)
        {
            if (!ClanInfoDAL.GetClanTimeOffset(strGrpID, out int intHourSet))
            {
                MsgMessage += new Message("与数据库失去连接，查询区域时间设定失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (intHourSet < 0)
            {
                MsgMessage += new Message("每日更新小时设定小于0，尚未验证这种形式的时间格式是否正常，已退回本功能。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(intHourSet);//每天凌晨4点开始
                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(intHourSet);//第二天凌晨4点结束
                if (dtNow.Hour >= 0 && dtNow.Hour < intHourSet)
                {
                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                }
                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                {
                    MsgMessage += new Message("截至目前尚有余刀的成员：");
                    int intCountLeft = 0;
                    int intCountUsed = 0;
                    int intCountExLeft = 0;
                    string strLeft1 = "";
                    string strLeft2 = "";
                    string strLeft3 = "";
                    string strLeft0_EX = "";
                    string strLeft1_EX = "";
                    string strLeft2_EX = "";
                    string strErr = "";
                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                    {
                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
                        string strUName = dtInsuff.Rows[i]["MBRNAME"].ToString();
                        int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
                        int intCountLastAtk = int.Parse(dtInsuff.Rows[i]["cla"].ToString());
                        int intCountExTime = int.Parse(dtInsuff.Rows[i]["cex"].ToString());
                        if ((intCountMain + intCountLastAtk) > 3)
                        {
                            //一种异常，主+尾刀超出了3，记为3刀
                            intCountUsed += 3;
                            strErr += "\r\n异常：" + strUName + "(" + strUID + ")，非补时刀数超过3，请检查";
                        }
                        if (intCountLastAtk < intCountExTime)
                        {
                            //一种异常，补时刀大于尾刀，记为主+补时刀
                            intCountUsed += (intCountMain + intCountLastAtk);
                            intCountLeft += 3 - (intCountMain + intCountLastAtk);
                            strErr += "\r\n异常：" + strUName + "(" + strUID + ")，补时刀数(" + intCountExTime + ")与尾刀数(" + intCountLastAtk + ")不匹配，请检查（推测剩余" + (3 - (intCountMain + intCountLastAtk)).ToString() + "刀）";
                        }
                        if ((intCountMain + intCountLastAtk) == 3)
                        {
                            if (intCountLastAtk > intCountExTime)
                            {
                                intCountUsed += 3;
                                intCountExLeft += 1;
                                strLeft0_EX += "\r\n仅剩补时刀：" + strUName + "(" + strUID + ")";
                            }
                            else
                            {
                                intCountUsed += 3;
                            }
                        }
                        if ((intCountMain + intCountLastAtk) == 2)
                        {
                            if (intCountLastAtk > intCountExTime)
                            {
                                intCountExLeft += 1;
                                intCountUsed += 2;
                                intCountLeft += 1;
                                strLeft1_EX += "\r\n剩余1刀+补时刀：" + strUName + "(" + strUID + ")";
                            }
                            else
                            {
                                intCountUsed += 2;
                                intCountLeft += 1;
                                strLeft1 += "\r\n剩余1刀：" + strUName + "(" + strUID + ")";
                            }
                        }
                        if ((intCountMain + intCountLastAtk) == 1)
                        {
                            if (intCountLastAtk > intCountExTime)
                            {
                                intCountExLeft += 1;
                                intCountUsed += 1;
                                intCountLeft += 2;
                                strLeft2_EX += "\r\n剩余2刀+补时刀：" + strUName + "(" + strUID + ")";
                            }
                            else
                            {
                                intCountUsed += 1;
                                intCountLeft += 2;
                                strLeft2 += "\r\n剩余2刀：" + strUName + "(" + strUID + ")";
                            }
                        }
                        if ((intCountMain + intCountLastAtk) == 0)
                        {
                            intCountLeft += 3;
                            strLeft3 += "\r\n剩余3刀：" + strUName + "(" + strUID + ")";
                        }
                    }
                    MsgMessage += new Message("\r\n--------------------");
                    if (strLeft0_EX != null && strLeft0_EX != "")
                    {
                        MsgMessage += new Message(strLeft0_EX + "\r\n--------------------");
                    }
                    if (strLeft1_EX != null && strLeft1_EX != "")
                    {
                        MsgMessage += new Message(strLeft1_EX + "\r\n--------------------");
                    }
                    if (strLeft2_EX != null && strLeft2_EX != "")
                    {
                        MsgMessage += new Message(strLeft2_EX + "\r\n--------------------");
                    }
                    if (strLeft1 != null && strLeft1 != "")
                    {
                        MsgMessage += new Message(strLeft1 + "\r\n--------------------");
                    }
                    if (strLeft2 != null && strLeft2 != "")
                    {
                        MsgMessage += new Message(strLeft2 + "\r\n--------------------");
                    }
                    if (strLeft3 != null && strLeft3 != "")
                    {
                        MsgMessage += new Message(strLeft3 + "\r\n--------------------");
                    }
                    MsgMessage += new Message("\r\n合计已出" + intCountUsed.ToString() + "刀\r\n合计剩余" + intCountLeft.ToString() + "刀，" + intCountExLeft.ToString() + "补时刀");
                    if (strErr != null && strErr != "")
                    {
                        MsgMessage += new Message("\r\n--------------------" + strErr);
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                    MsgMessage += Message.At(long.Parse(strUserID));
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
            }
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }
    }
}
