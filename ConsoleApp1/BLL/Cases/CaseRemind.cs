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
            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                {
                    //0点后日期变换，开始日期需查到昨天
                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                }
                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                {
                    MsgMessage += new Message("请以下成员尽早出刀：");
                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                    {
                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
                        string strCountMain = dtInsuff.Rows[i]["cmain"].ToString();
                        if (int.Parse(strCountMain) < 3)
                        {
                            MsgMessage += new Message("\r\nID：" + strUID + "，剩余" + (3 - int.Parse(strCountMain)).ToString() + "刀 ") + Message.At(long.Parse(strUID));
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

        public static void RemainStrikes(string strGrpID, string strUserID,GroupMemberInfo memberInfo,int intType)
        {
            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                {
                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                }
                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                {
                    //MsgMessage += new Message("截至目前尚有余刀的成员：");
                    //int intCount = 0;
                    //string strLeft1 = "";
                    //string strLeft2 = "";
                    //string strLeft3 = "";
                    //for (int i = 0; i < dtInsuff.Rows.Count; i++)
                    //{
                    //    string strUID = dtInsuff.Rows[i]["userid"].ToString();
                    //    int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
                    //    if (intCountMain == 2)
                    //    {
                    //        intCount += 1;
                    //        strLeft1 += "\r\nID：" + strUID + "，剩余1刀";
                    //    }
                    //    if (intCountMain == 1)
                    //    {
                    //        intCount += 2;
                    //        strLeft2 += "\r\nID：" + strUID + "，剩余2刀";
                    //    }
                    //    if (intCountMain == 0)
                    //    {
                    //        intCount += 3;
                    //        strLeft3 += "\r\nID：" + strUID + "，剩余3刀";
                    //    }
                    //}
                    //MsgMessage += new Message(strLeft1 + "\r\n--------------------" + strLeft2 + "\r\n--------------------" + strLeft3);
                    //MsgMessage += new Message("\r\n合计剩余" + intCount.ToString() + "刀");
                }
                else
                {
                    //MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                    //MsgMessage += Message.At(long.Parse(strUserID));
                }
            }
            else
            {
                //MsgMessage += new Message("与数据库失去连接，查询失败。\r\n");
                //MsgMessage += Message.At(long.Parse(strUserID));
            }
            //ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }



        /// <summary>
        /// 查询未出满三刀的成员
        /// </summary>
        /// <param name="strGrpID">消息发起人所属群号</param>
        /// <param name="strUserID">消息发起人QQ号</param>
        public static void ShowRemainStrikes(string strGrpID, string strUserID)
        {
            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                DateTime dtStart = CmdHelper.GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
                DateTime dtEnd = CmdHelper.GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                {
                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                }
                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                {
                    MsgMessage += new Message("截至目前尚有余刀的成员：");
                    int intCountLeft = 0;
                    int intCountUsed = 0;
                    string strLeft1 = "";
                    string strLeft2 = "";
                    string strLeft3 = "";
                    string strErr = "";
                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                    {
                        string strUID = dtInsuff.Rows[i]["MBRID"].ToString();
                        string strUName = dtInsuff.Rows[i]["MBRNAME"].ToString();
                        int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
                        if (intCountMain > 3)
                        {
                            intCountUsed += 3;
                            strErr = "\r\n刀数异常：" + strUName + "(" + strUID + ")，非补时刀数大于3刀，请检查";
                        }
                        if (intCountMain == 3)
                        {
                            intCountUsed += 3;
                        }
                        if (intCountMain == 2)
                        {
                            intCountUsed += 2;
                            intCountLeft += 1;
                            strLeft1 += "\r\n剩余1刀：" + strUName + "(" + strUID + ")";
                        }
                        if (intCountMain == 1)
                        {
                            intCountUsed += 1;
                            intCountLeft += 2;
                            strLeft2 += "\r\n剩余2刀：" + strUName + "(" + strUID + ")";
                        }
                        if (intCountMain == 0)
                        {
                            intCountLeft += 3;
                            strLeft3 += "\r\n剩余3刀：" + strUName + "(" + strUID + ")";
                        }
                    }
                    MsgMessage += new Message("\r\n--------------------");
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
                    //MsgMessage += new Message("\r\n--------------------" + strLeft1 + "\r\n--------------------" + strLeft2 + "\r\n--------------------" + strLeft3);
                    MsgMessage += new Message("\r\n合计已出" + intCountUsed.ToString() + "刀\r\n合计剩余" + intCountLeft.ToString() + "刀");
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
