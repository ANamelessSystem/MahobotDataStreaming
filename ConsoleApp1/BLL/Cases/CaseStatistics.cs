//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Data;
//using Marchen.DAL;
//using Marchen.Model;
//using Message = Sisters.WudiLib.SendingMessage;
//using Sisters.WudiLib.Responses;

//namespace Marchen.BLL
//{
//    class CaseStatistics : GroupMsgBLL
//    {
//        /// <summary>
//        /// At未出满三刀的成员
//        /// </summary>
//        /// <param name="strGrpID">消息发起人所属群号</param>
//        /// <param name="strUserID">消息发起人QQ号</param>
//        /// <param name="memberInfo">消息发起人的成员资料</param>
//        public static void NoticeRemainStrikers(string strGrpID, string strUserID, GroupMemberInfo memberInfo)
//        {
//            if (!(memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager))
//            {
//                MsgMessage += new Message("拒绝：仅有管理员或群主可执行出刀提醒指令。\r\n");
//                MsgMessage += Message.At(long.Parse(strUserID));
//                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
//                return;
//            }
//            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
//            {
//                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
//                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
//                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
//                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
//                {
//                    //0点后日期变换，开始日期需查到昨天
//                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
//                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
//                }
//                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
//                {
//                    MsgMessage += new Message("请以下成员尽早出刀：");
//                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
//                    {
//                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
//                        string strCountMain = dtInsuff.Rows[i]["cmain"].ToString();
//                        if (int.Parse(strCountMain) < 3)
//                        {
//                            MsgMessage += new Message("\r\nID：" + strUID + "，剩余" + (3 - int.Parse(strCountMain)).ToString() + "刀 ") + Message.At(long.Parse(strUID));
//                        }
//                    }
//                }
//                else
//                {
//                    MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
//                    MsgMessage += Message.At(long.Parse(strUserID));
//                }
//            }
//            else
//            {
//                MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
//                MsgMessage += Message.At(long.Parse(strUserID));
//            }
//            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
//        }

//        /// <summary>
//        /// 查询未出满三刀的成员
//        /// </summary>
//        /// <param name="strGrpID">消息发起人所属群号</param>
//        /// <param name="strUserID">消息发起人QQ号</param>
//        public static void ShowRemainStrikes(string strGrpID, string strUserID)
//        {
//            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
//            {
//                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
//                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
//                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
//                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
//                {
//                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
//                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
//                }
//                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
//                {
//                    MsgMessage += new Message("截至目前尚有余刀的成员：");
//                    int intCountLeft = 0;
//                    int intCountUsed = 0;
//                    string strLeft1 = "";
//                    string strLeft2 = "";
//                    string strLeft3 = "";
//                    string strErr = "";
//                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
//                    {
//                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
//                        string strUName = dtInsuff.Rows[i]["MBRNAME"].ToString();
//                        int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
//                        if (intCountMain > 3)
//                        {
//                            intCountUsed += 3;
//                            strErr += "\r\n刀数异常：" + strUName + "(" + strUID + ")，非补时刀数大于3刀，请检查";
//                        }
//                        if (intCountMain == 3)
//                        {
//                            intCountUsed += 3;
//                        }
//                        if (intCountMain == 2)
//                        {
//                            intCountUsed += 2;
//                            intCountLeft += 1;
//                            strLeft1 += "\r\n剩余1刀：" + strUName + "(" + strUID + ")";
//                        }
//                        if (intCountMain == 1)
//                        {
//                            intCountUsed += 1;
//                            intCountLeft += 2;
//                            strLeft2 += "\r\n剩余2刀：" + strUName + "(" + strUID + ")";
//                        }
//                        if (intCountMain == 0)
//                        {
//                            intCountLeft += 3;
//                            strLeft3 += "\r\n剩余3刀：" + strUName + "(" + strUID + ")";
//                        }
//                    }
//                    MsgMessage += new Message("\r\n--------------------");
//                    if (strLeft1 != null && strLeft1 != "")
//                    {
//                        MsgMessage += new Message(strLeft1 + "\r\n--------------------");
//                    }
//                    if (strLeft2 != null && strLeft2 != "")
//                    {
//                        MsgMessage += new Message(strLeft2 + "\r\n--------------------");
//                    }
//                    if (strLeft3 != null && strLeft3 != "")
//                    {
//                        MsgMessage += new Message(strLeft3 + "\r\n--------------------");
//                    }
//                    //MsgMessage += new Message("\r\n--------------------" + strLeft1 + "\r\n--------------------" + strLeft2 + "\r\n--------------------" + strLeft3);
//                    MsgMessage += new Message("\r\n合计已出" + intCountUsed.ToString() + "刀\r\n合计剩余" + intCountLeft.ToString() + "刀");
//                    if (strErr != null && strErr != "")
//                    {
//                        MsgMessage += new Message("\r\n--------------------" + strErr);
//                    }
//                }
//                else
//                {
//                    MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
//                    MsgMessage += Message.At(long.Parse(strUserID));
//                }
//            }
//            else
//            {
//                MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
//                MsgMessage += Message.At(long.Parse(strUserID));
//            }
//            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
//        }

//        /// <summary>
//        /// 根据目前最新的进度算出分数
//        /// </summary>
//        public static void ShowScoreNow(string strGrpID,string strTesting)
//        {
//            double douBCNow = 0;
//            double douRoundNow = 0;
//            double douScore = 0;
//            double douHPNow = 0;
//            if (RecordDAL.GetBossProgress(strTesting, out DataTable dtBossProgress))
//            {
//                douRoundNow = double.Parse(dtBossProgress.Rows[0]["maxround"].ToString());
//                douBCNow = double.Parse(dtBossProgress.Rows[0]["maxbc"].ToString());
//                douHPNow = double.Parse(dtBossProgress.Rows[0]["hpremain"].ToString());
//                if (douRoundNow < 4)
//                {
//                    //血量：     600   ;800   ;1000  ;1200  ;2000
//                    //难度1分率：B1=1.0;B2=1.0;B3=1.3;B4=1.3;B5=1.5
//                    douScore = (douRoundNow - 1) * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
//                    if (douBCNow == 1)
//                    {
//                        douScore += (6000000 - douHPNow) * 1;
//                    }
//                    else if (douBCNow == 2)
//                    {
//                        douScore += 6000000 * 1;
//                        douScore += (8000000 - douHPNow) * 1;
//                    }
//                    else if (douBCNow == 3)
//                    {
//                        douScore += 6000000 * 1;
//                        douScore += 8000000 * 1;
//                        douScore += (10000000 - douHPNow) * 1.3;
//                    }
//                    else if (douBCNow == 4)
//                    {
//                        douScore += 6000000 * 1;
//                        douScore += 8000000 * 1;
//                        douScore += 10000000 * 1.3;
//                        douScore += (12000000 - douHPNow) * 1.3;
//                    }
//                    else
//                    {
//                        douScore += 6000000 * 1;
//                        douScore += 8000000 * 1;
//                        douScore += 10000000 * 1.3;
//                        douScore += 12000000 * 1.3;
//                        douScore += (20000000 - douHPNow) * 1.5;
//                    }
//                }
//                else if (douRoundNow < 11)
//                {
//                    //血量：     600   ;800   ;1000  ;1200  ;2000
//                    //难度2分率：B1=1.4;B2=1.4;B3=1.8;B4=1.8;B5=2.0
//                    douScore = 3 * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
//                    douScore += (douRoundNow - 4) * ((6000000 * 1.4) + (8000000 * 1.4) + (10000000 * 1.8) + (12000000 * 1.8) + (20000000 * 2.0));
//                    if (douBCNow == 1)
//                    {
//                        douScore += (6000000 - douHPNow) * 1.4;
//                    }
//                    else if (douBCNow == 2)
//                    {
//                        douScore += 6000000 * 1.4;
//                        douScore += (8000000 - douHPNow) * 1.4;
//                    }
//                    else if (douBCNow == 3)
//                    {
//                        douScore += 6000000 * 1.4;
//                        douScore += 8000000 * 1.4;
//                        douScore += (10000000 - douHPNow) * 1.8;
//                    }
//                    else if (douBCNow == 4)
//                    {
//                        douScore += 6000000 * 1.4;
//                        douScore += 8000000 * 1.4;
//                        douScore += 10000000 * 1.8;
//                        douScore += (12000000 - douHPNow) * 1.8;
//                    }
//                    else
//                    {
//                        douScore += 6000000 * 1.4;
//                        douScore += 8000000 * 1.4;
//                        douScore += 10000000 * 1.8;
//                        douScore += 12000000 * 1.8;
//                        douScore += (20000000 - douHPNow) * 2.0;
//                    }
//                }
//                else
//                {
//                    //血量：     600   ;800   ;1000  ;1200  ;2000
//                    //难度3分率：B1=2.0;B2=2.0;B3=2.5;B4=2.5;B5=3.0
//                    douScore = 3 * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
//                    douScore += 7 * ((6000000 * 1.4) + (8000000 * 1.4) + (10000000 * 1.8) + (12000000 * 1.8) + (20000000 * 2.0));
//                    douScore += (douRoundNow - 11) * ((6000000 * 2.0) + (8000000 * 2.0) + (10000000 * 2.5) + (12000000 * 2.5) + (20000000 * 3.0));
//                    if (douBCNow == 1)
//                    {
//                        douScore += (6000000 - douHPNow) * 2;
//                    }
//                    else if (douBCNow == 2)
//                    {
//                        douScore += 6000000 * 2;
//                        douScore += (8000000 - douHPNow) * 2;
//                    }
//                    else if (douBCNow == 3)
//                    {
//                        douScore += 6000000 * 2;
//                        douScore += 8000000 * 2;
//                        douScore += (10000000 - douHPNow) * 2.5;
//                    }
//                    else if (douBCNow == 4)
//                    {
//                        douScore += 6000000 * 2;
//                        douScore += 8000000 * 2;
//                        douScore += 10000000 * 2.5;
//                        douScore += (12000000 - douHPNow) * 2.5;
//                    }
//                    else
//                    {
//                        douScore += 6000000 * 2;
//                        douScore += 8000000 * 2;
//                        douScore += 10000000 * 2.5;
//                        douScore += 12000000 * 2.5;
//                        douScore += (20000000 - douHPNow) * 3;
//                    }
//                }
//            }
//            else
//            {
//                Console.WriteLine("获取BOSS进度时出错");
//            }
//            MsgMessage += new Message("目前分数为：" + douScore.ToString());
//            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
//        }
//    }
//}
