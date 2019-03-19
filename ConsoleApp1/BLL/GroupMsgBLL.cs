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
                            //按EID查询单条，或按周目、boss、周目+boss查询多条
                            string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (strCmdContext.ToLower().Contains("e") && !(strCmdContext.ToLower().Contains("b") || strCmdContext.Contains("周目")))
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
                                            message += new Message("无法识别档案号。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
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
                                                message += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                            }
                                            else if (dtDmgRecEID.Rows.Count > 1)
                                            {
                                                Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                                                message += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
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
                                                    message += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                                    message += Message.At(long.Parse(strUserID));
                                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                                    return;
                                                }
                                                Console.WriteLine("档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                                                message += new Message("档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                                            }
                                        }
                                    }
                                }
                            }
                            else if (!strCmdContext.ToLower().Contains("e") && (strCmdContext.ToLower().Contains("b") && strCmdContext.Contains("周目")))
                            {
                                //处理按boss与周目查询部分
                                Console.WriteLine("开始按周目、boss查询");
                                int intBossCode = 0;
                                int intRound = 0;
                                foreach (string e in sArray)
                                {
                                    if (e.ToLower().Contains("b"))
                                    {
                                        if (!int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                                        {
                                            Console.WriteLine("无法识别BOSS。原始信息=" + e.ToString());
                                            message += new Message("无法识别BOSS。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
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
                                            message += new Message("无法识别周目。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
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
                                                message += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                                message += Message.At(long.Parse(strUserID));
                                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                                return;
                                            }
                                            Console.WriteLine("E" + strREID + "：" + resultString + "\r\n");
                                            message += new Message("E" + strREID + "：" + resultString + "\r\n");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                message += new Message("未能识别查询内容。\r\n本功能支持单独按档案号查询，或同时按BOSS与周目查询。");
                            }
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
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
