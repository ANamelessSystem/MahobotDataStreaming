using System;
using System.Data;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using MessageContext = Sisters.WudiLib.Posts.Message;
using System.Text.RegularExpressions;
using Marchen.DAL;
using Sisters.WudiLib.Responses;

namespace Marchen.BLL
{
    class GroupMsgBLL
    {
        /// <summary>
        /// 消息
        /// </summary>
        protected static Message MsgMessage;

        /// <summary>
        /// 读取上限值
        /// </summary>

        private static void LoadValueLimits()
        {
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
                        return;
                    }
                    else if (ValueLimits.RoundLimitMax == 0)
                    {
                        Console.WriteLine("未能获取周目上限，请检查TTL_LIMITS表中是否有ROUND_MAX项");
                        return;
                    }
                    else if (ValueLimits.BossLimitMax == 0)
                    {
                        Console.WriteLine("未能获取BOSS编号上限，请检查TTL_LIMITS表中是否有BOSS_MAX项");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("获取上限值成功，以下是获取到的上限值：");
                        Console.WriteLine("伤害上限：" + ValueLimits.DamageLimitMax.ToString());
                        Console.WriteLine("周目上限：" + ValueLimits.RoundLimitMax.ToString());
                        Console.WriteLine("BOSS编号上限：" + ValueLimits.BossLimitMax.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("向数据库读取上限值时无返回条目，请检查TTL_LIMITS表。");
                }
            }
            else
            {
                return;
            }
        }
        /// <summary>
        /// 获取指定日期的0点时间
        /// </summary>
        /// <param name="datetime">指定日期</param>
        /// <returns>返回指定日期的0点时间</returns>
        private static DateTime GetZeroTime(DateTime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, datetime.Day);
        }

        /// <summary>
        /// 解析收到的来自群的内容
        /// </summary>
        /// <param name="receivedMessage">收到的消息内容</param>
        /// <param name="memberInfo">发出此消息的用户信息</param>
        public static void GrpMsgReco(MessageContext receivedMessage, GroupMemberInfo memberInfo)
        {
            MsgMessage = new Message("");
            string strRawcontext = receivedMessage.RawMessage.ToString().Trim();
            string cmdAtMeAlone = "[CQ:at,qq=" + SelfProperties.SelfID + "]";
            if (strRawcontext.Contains(cmdAtMeAlone))
            {
                string strGrpID = receivedMessage.GetType().GetProperty("GroupId").GetValue(receivedMessage, null).ToString();
                var message = new Message("");
                int vfyCode = QueueDAL.GroupRegVerify(strGrpID);
                #region 有效性验证不通过
                if (vfyCode == 0)
                {
                    message += new Message("vfycode0：bot服务尚未在本群开启，请管理员联系bot维护团队。\r\n");
                    message += Message.At(receivedMessage.UserId);
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                    return;
                }
                if (vfyCode == 10)
                {
                    message += new Message("vfycode10：无法连接主数据库，请联系bot维护团队。\r\n");
                    message += Message.At(receivedMessage.UserId);
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                    return;
                }
                if (vfyCode == 11)
                {
                    message += new Message("vfycode11：本群的服务设置有误，请联系bot维护团队。\r\n");
                    message += Message.At(receivedMessage.UserId);
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                    return;
                }
                if (vfyCode == 12)
                {
                    message += new Message("vfycode12：业务流出现错误，请联系bot维护团队。\r\n");
                    message += Message.At(receivedMessage.UserId);
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                    return;
                }
                #endregion
                Console.WriteLine("接收到一条来自群：" + strGrpID + "的Notice，开始解析内容");
                string strCmdContext = "";
                string strUserID = receivedMessage.UserId.ToString();
                string strUserGrpCard = memberInfo.InGroupName.ToString().Trim();
                string strUserNickName = memberInfo.Nickname.ToString().Trim();
                LoadValueLimits();
                if (strUserGrpCard == null || strUserGrpCard == "")
                {
                    strUserGrpCard = strUserNickName;
                }
                strCmdContext = strRawcontext.Replace(cmdAtMeAlone, "").Trim();
                string cmdType = "";
                if (strCmdContext.ToLower() == "c1")
                {
                    cmdType = "queueadd";
                    Console.WriteLine("识别为开始排刀");
                }
                else if (strCmdContext.ToLower() == "c2")
                {
                    cmdType = "queueshow";
                    Console.WriteLine("识别为查询排刀");
                }
                else if (strCmdContext.ToLower() == "c3")
                {
                    cmdType = "queuequit";
                    Console.WriteLine("识别为退出排刀");
                }
                else if (strCmdContext.Contains("清空队列"))
                {
                    cmdType = "clear";
                    Console.WriteLine("识别为清空指令");
                }
                else if (strCmdContext.Contains("伤害") && !strCmdContext.Contains("修改"))
                {
                    cmdType = "debrief";
                    Console.WriteLine("识别为伤害上报");
                }
                else if (strCmdContext.ToLower() == "help")
                {
                    cmdType = "help";
                    Console.WriteLine("识别为说明书呈报");
                }
                else if (strCmdContext.Contains("掉线"))
                {
                    cmdType = "timeout";
                    Console.WriteLine("识别为掉线");
                }
                else if (strCmdContext.Contains("修改"))
                {
                    cmdType = "dmgmod";
                    Console.WriteLine("识别为伤害修改");
                }
                else if (strCmdContext.Contains("查看"))
                {
                    cmdType = "dmgshow";
                    Console.WriteLine("识别为伤害查看");
                }
                else if (strCmdContext.ToLower() == "f1")
                {
                    cmdType = "remainshow";
                    Console.WriteLine("识别为未出满三刀的成员查询");
                }
                else if (strCmdContext.ToLower() == "f2")
                {
                    cmdType = "remainnotice";
                    Console.WriteLine("识别为提醒未出满三刀的成员");
                }
                else if (strCmdContext == "导出统计表")
                {
                    cmdType = "fileoutput";
                    Console.WriteLine("识别为导出统计表");
                }
                else
                {
                    cmdType = "unknown";
                    Console.WriteLine("识别失败，未从已知功能中发现特征");
                }
                switch (cmdType)
                {
                    case "queueadd":
                        CaseQueue.QueueAdd(strGrpID, strUserID, strUserGrpCard);
                        break;
                    case "queueshow":
                        CaseQueue.QueueShow(strGrpID, strUserID);
                        break;
                    case "queuequit":
                        CaseQueue.QueueQuit(strGrpID, strUserID);
                        break;
                    case "clear":
                        CaseQueue.QueueClear(strGrpID, strUserID, memberInfo);
                        break;
                    case "debrief":
                        CaseDamage.DmgRecAdd(strGrpID, strUserID, strCmdContext);
                        break;
                    case "help":
                        {
                            message += new Message("加入队列：【@MahoBot c1】可进入队列\r\n");
                            message += new Message("查询队列：【@MahoBot c2】可查询当前在队列中的人\r\n");
                            message += new Message("退出队列：【@MahoBot c3】可离开队列\r\n");
                            message += new Message("伤害记录：【@MahoBot 伤害 B(n) (n)周目 （伤害值）】（如@MahoBot 伤害 B2 6周目 1374200）\r\n 伤害值可如137w等模糊格式\r\n");
                            message += new Message("额外时间的伤害记录：【@MahoBot 伤害 补时 B(n) (n)周目 （伤害值）】\r\n");
                            message += new Message("掉线记录：【@MahoBot 掉线 (是否补时)】可记录一次掉线或额外时间掉线\r\n");
                            message += new Message("其他功能及用例请参考群文件的命令表\r\n");
                        }
                        message += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        break;
                    case "timeout":
                        CaseDamage.DmgTimeOut(strGrpID, strUserID, strCmdContext);
                        break;
                    case "dmgmod":
                        CaseDamage.DmgModify(strGrpID, strUserID, strCmdContext, memberInfo);
                        break;
                    case "dmgshow":
                        {
                            
                        }
                        break;
                    case "remainshow":
                        {
                            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
                            {
                                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                                DateTime dtStart = GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
                                DateTime dtEnd = GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
                                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                                {
                                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                                }
                                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                                {
                                    message += new Message("截至目前尚有余刀的成员：");
                                    int intCount = 0;
                                    string strLeft1 = "";
                                    string strLeft2 = "";
                                    string strLeft3 = "";
                                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                                    {
                                        string strUID = dtInsuff.Rows[i]["userid"].ToString();
                                        int intCountMain = int.Parse(dtInsuff.Rows[i]["cmain"].ToString());
                                        if (intCountMain == 2)
                                        {
                                            intCount += 1;
                                            strLeft1 += "\r\nID：" + strUID + "，剩余1刀";
                                        }
                                        if (intCountMain == 1)
                                        {
                                            intCount += 2;
                                            strLeft2 += "\r\nID：" + strUID + "，剩余2刀";
                                        }
                                        if (intCountMain == 0)
                                        {
                                            intCount += 3;
                                            strLeft3 += "\r\nID：" + strUID + "，剩余3刀";
                                        }
                                    }
                                    message += new Message(strLeft1 + "\r\n--------------------" + strLeft2 + "\r\n--------------------" + strLeft3);
                                    message += new Message("\r\n合计剩余" + intCount.ToString() + "刀");
                                }
                                else
                                {
                                    message += new Message("与数据库失去连接，查询失败。\r\n");
                                    message += Message.At(long.Parse(strUserID));
                                }
                            }
                            else
                            {
                                message += new Message("与数据库失去连接，查询失败。\r\n");
                                message += Message.At(long.Parse(strUserID));
                            }
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "remainnotice":
                        {
                            if (!(memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager))
                            {
                                message += new Message("拒绝：仅有管理员或群主可执行出刀提醒指令。\r\n");
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                return;
                            }
                            if (RecordDAL.QueryTimeNowOnDatabase(out DataTable dtResultTime))
                            {
                                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                                DateTime dtStart = GetZeroTime(dtNow).AddHours(4);//每天凌晨4点开始
                                DateTime dtEnd = GetZeroTime(dtNow.AddDays(1)).AddHours(4);//第二天凌晨4点结束
                                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                                {
                                    //0点后日期变换，开始日期需查到昨天
                                    dtStart = dtStart.AddDays(-1);//每天凌晨4点开始
                                    dtEnd = dtEnd.AddDays(-1);//第二天凌晨4点结束
                                }
                                if (RecordDAL.QueryStrikeStatus(strGrpID, dtStart, dtEnd, out DataTable dtInsuff))
                                {
                                    message += new Message("请以下成员尽早出刀：");
                                    for (int i = 0; i < dtInsuff.Rows.Count; i++)
                                    {
                                        string strUID = dtInsuff.Rows[i]["userid"].ToString();
                                        string strCountMain = dtInsuff.Rows[i]["cmain"].ToString();
                                        if (int.Parse(strCountMain) < 3)
                                        {
                                            message += new Message("\r\nID：" + strUID + "，剩余" + (3 - int.Parse(strCountMain)).ToString() + "刀 ") + Message.At(long.Parse(strUID));
                                        }
                                    }
                                }
                                else
                                {
                                    message += new Message("与数据库失去连接，查询失败。\r\n");
                                    message += Message.At(long.Parse(strUserID));
                                }
                            }
                            else
                            {
                                message += new Message("与数据库失去连接，查询失败。\r\n");
                                message += Message.At(long.Parse(strUserID));
                            }
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "fileoutput":
                        {
                            //if (!(memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager))
                            //{
                            //    message += new Message("拒绝：仅有管理员或群主可执行导出伤害表。\r\n");
                            //    message += Message.At(long.Parse(strUserID));
                            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            //    return;
                            //}
                            //if (!(strGrpID == "877184755"))
                            //{
                            //    message += new Message("拒绝：调试功能只开放给特定群。\r\n");
                            //    message += Message.At(long.Parse(strUserID));
                            //    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            //    return;
                            //}
                            //if (RecordDAL.QueryDamageTable(strGrpID, out DataTable dtDmgReport))
                            //{
                            //    string fileName = "C:\\MahoBotOutput\\wdll.csv";
                            //    SaveCSV(dtDmgReport, fileName);
                            //    try
                            //    {
                            //        var fileMessage = Message.LocalImage(@"C:\MahoBotOutput\wdll.csv");
                            //        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), fileMessage).Wait();
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        Console.WriteLine(ex);
                            //        return;
                            //    }
                            //}
                        }
                        break;
                    case "unknown":
                        {
                            message += new Message("无法识别内容,输入【@MahoBot help】以查询命令表。\r\n");
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            //RecordDAL.RecordUnknownContext(strGrpID, strUserID, cmdContext);
                        }
                        break;
                }
            }
        }
    }
}
