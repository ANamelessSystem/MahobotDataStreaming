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
        public static void GrpMsgReco(MessageContext receivedMessage, GroupMemberInfo memberInfo)
        {
            string strRawcontext = receivedMessage.RawMessage.ToString().Trim();
            string cmdAtMeAlone = "[CQ:at,qq=" + SelfProperties.SelfID + "]";
            string cmdContext = "";
            string strGrpID = receivedMessage.GetType().GetProperty("GroupId").GetValue(receivedMessage, null).ToString();
            string strUserID = receivedMessage.UserId.ToString();
            string strUserGrpCard = memberInfo.InGroupName.ToString().Trim();
            string strUserNickName = memberInfo.Nickname.ToString().Trim();
            if (strUserGrpCard == null || strUserGrpCard == "")
            {
                strUserGrpCard = strUserNickName;
            }
            int vfyCode = QueueDAL.GroupRegVerify(strGrpID);
            #region 意外情况
            if (strRawcontext.Contains(cmdAtMeAlone) && vfyCode == 0)
            {
                var message = new Message("");
                message += new Message("vfycode0：bot服务尚未在本群开启，请管理员联系bot维护人员。\r\n");
                message += Message.At(receivedMessage.UserId);
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                return;
            }
            if (strRawcontext.Contains(cmdAtMeAlone) && vfyCode == 10)
            {
                var message = new Message("");
                message += new Message("vfycode10：无法连接主数据库，请联系bot维护人员。\r\n");
                message += Message.At(receivedMessage.UserId);
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                return;
            }
            if (strRawcontext.Contains(cmdAtMeAlone) && vfyCode == 11)
            {
                var message = new Message("");
                message += new Message("vfycode11：本群的服务设置有误，请联系bot维护人员。\r\n");
                message += Message.At(receivedMessage.UserId);
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                return;
            }
            if (strRawcontext.Contains(cmdAtMeAlone) && vfyCode == 12)
            {
                var message = new Message("");
                message += new Message("vfycode12：业务流出现错误，请联系bot维护人员。\r\n");
                message += Message.At(receivedMessage.UserId);
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                return;
            }
            #endregion
            if (strRawcontext.Contains(cmdAtMeAlone) && vfyCode == 1)
            {
                var message = new Message("");
                Console.WriteLine("接收到一条来自群的单at，开始解析内容");
                cmdContext = strRawcontext.Replace(cmdAtMeAlone, "").Trim();
                string cmdType = "";
                if (cmdContext.ToLower() == "c1")
                {
                    cmdType = "queueadd";
                    Console.WriteLine("识别为开始排刀");
                }
                else if (cmdContext.ToLower() == "c2")
                {
                    cmdType = "queueshow";
                    Console.WriteLine("识别为查询排刀");
                }
                else if (cmdContext.ToLower() == "c3")
                {
                    cmdType = "queuequit";
                    Console.WriteLine("识别为退出排刀");
                }
                else if (cmdContext.Contains("清空队列"))
                {
                    cmdType = "clear";
                    Console.WriteLine("识别为清空指令");
                }
                else if (cmdContext.Contains("伤害") && !cmdContext.Contains("修改"))
                {
                    cmdType = "debrief";
                    Console.WriteLine("识别为伤害上报");
                }
                else if (cmdContext.ToLower() == "help")
                {
                    cmdType = "help";
                    Console.WriteLine("识别为说明书呈报");
                }
                else if (cmdContext == "掉刀" || cmdContext == "掉线")
                {
                    cmdType = "timeout";
                    Console.WriteLine("识别为掉线");
                }
                else if (cmdContext.Contains("修改"))
                {
                    cmdType = "dmgmod";
                    Console.WriteLine("识别为伤害修改");
                }
                else if (cmdContext.Contains("查看"))
                {
                    cmdType = "dmgshow";
                    Console.WriteLine("识别为伤害查看");
                }
                else
                {
                    cmdType = "unknown";
                    Console.WriteLine("识别失败，未从已知功能中发现特征");
                }
                switch (cmdType)
                {
                    case "queueadd":
                        if (QueueDAL.AddQueue(strGrpID, strUserID, strUserGrpCard))
                        {
                            message += new Message("已加入队列\r\n--------------------\r\n");
                            goto case "queueshow";
                        }
                        else
                        {
                            message += new Message("与数据库失去连接，加入队列失败。\r\n");
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "queueshow":
                        if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue))
                        {
                            if (dtQueue.Rows.Count > 0)
                            {
                                message += new Message("目前队列：\r\n");
                                for (int i = 0; i < dtQueue.Rows.Count; i++)
                                {
                                    string strOutput = "顺序：" + dtQueue.Rows[i]["seq"].ToString() + "    " + dtQueue.Rows[i]["name"].ToString() + "(" + dtQueue.Rows[i]["id"].ToString() + ")";
                                    string strMark = "←";//提醒用标识
                                    if (dtQueue.Rows[i]["id"].ToString() == strUserID)
                                    {
                                        message += new Message(strOutput + strMark + "\r\n");
                                    }
                                    else
                                    {
                                        message += new Message(strOutput + "\r\n");
                                    }
                                    Console.WriteLine(strOutput);
                                }
                            }
                            else
                            {
                                Console.WriteLine("队列中无人");
                                message += new Message("目前队列中无人。\r\n");
                            }
                        }
                        else
                        {
                            message += new Message("与数据库失去连接，查询队列失败。\r\n");
                        }
                        message += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        break;
                    case "queuequit":
                        if (QueueDAL.QuitQueue(strGrpID, strUserID, out int deletedCount))
                        {
                            if (deletedCount > 0)
                            {
                                Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀移出队列。");
                                message += new Message("已将较早一次队列记录退出。\r\n--------------------\r\n");
                            }
                            else
                            {
                                Console.WriteLine("群：" + strGrpID + "，" + strUserID + "移出队列失败：未找到记录。");
                                message += new Message("退出队列失败，未找到你的队列记录。\r\n--------------------\r\n");
                            }
                            goto case "queueshow";
                        }
                        else
                        {
                            message += new Message("与数据库失去连接，退出队列失败。\r\n");
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "clear":
                        {
                            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
                            {
                                if (QueueDAL.ClearQueue(strGrpID, out deletedCount))
                                {
                                    if (deletedCount > 0)
                                    {
                                        message += new Message("已清空队列。\r\n");
                                    }
                                    else
                                    {
                                        message += new Message("队列中无人。\r\n");
                                    }
                                }
                                else
                                {
                                    message += new Message("与数据库失去连接，清空队列失败。\r\n");
                                }
                            }
                            else
                            {
                                message += new Message("拒绝：仅有管理员或群主可执行队列清空指令。\r\n");
                            }
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "debrief":
                        {
                            int intBossCode = 0;
                            int intRound = 0;
                            int intDMG = -1;
                            bool isCorrect = true;
                            int intExTime = 0;
                            if (RecordDAL.CheckClanDmgTable(strGrpID, out DataTable dtTableCount))
                            {
                                if (int.Parse(dtTableCount.Rows[0]["count"].ToString()) != 1)
                                {
                                    if (RecordDAL.CreateTablesForGuildDamage(strGrpID))
                                    {
                                        message += new Message("(未找到本公会伤害后台数据表，已自动建立。)\r\n");
                                    }
                                    else
                                    {
                                        message += new Message("(公会伤害后台数据表建立失败。)\r\n");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                message += new Message("与数据库失去连接，读取本公会伤害表失败。\r\n");
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                return;
                            }
                            string[] sArray = cmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string e in sArray)
                            {
                                if (e.ToLower() == "b1" || e.ToLower() == "b2" || e.ToLower() == "b3" || e.ToLower() == "b4" || e.ToLower() == "b5")
                                {
                                    try
                                    {
                                        intBossCode = int.Parse(e.ToLower().Replace("b", ""));
                                        if (intBossCode > 5 || intBossCode < 1)
                                        {
                                            throw new Exception("由boss异常检测块抛出");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        message += new Message("boss代码有误，请确保填入为b1~b5\r\n");
                                        isCorrect = false;
                                    }
                                }
                                else if (e == "补时")
                                {
                                    intExTime = 1;
                                }
                                else if (e.Contains("周目"))
                                {
                                    try
                                    {
                                        intRound = int.Parse(e.Replace("周目", ""));
                                        if (intRound > 30)
                                        {
                                            throw new Exception("由周目过高检测块抛出");
                                        }
                                        if (intRound < 1)
                                        {
                                            throw new Exception("由周目过低检测块抛出");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        if (intRound > 30)
                                        {
                                            message += new Message("周目数过高，请确保填入周目正确\r\n");
                                        }
                                        else if (intRound < 1)
                                        {
                                            message += new Message("周目数过低，请确保填入周目正确\r\n");
                                        }
                                        else
                                        {
                                            message += new Message("无法识别周目，请确保填入周目正确\r\n");
                                        }
                                        isCorrect = false;
                                    }
                                }
                                else if (e.ToLower().Contains("w") || e.Contains("万"))
                                {
                                    try
                                    {
                                        decimal result = decimal.Parse(Regex.Replace(e, @"[^\d.\d]", ""));
                                        intDMG = int.Parse(decimal.Round((result * 10000), 0).ToString());
                                        if (intDMG > 3000000)
                                        {
                                            throw new Exception("由伤害过高检测块抛出");
                                        }
                                        if (intDMG < 1000)
                                        {
                                            throw new Exception("由伤害过低检测块抛出");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        if (intDMG > 3000000)
                                        {
                                            message += new Message("伤害过高，请确保填入伤害值正确\r\n");
                                        }
                                        else if (intDMG < 1000)
                                        {
                                            message += new Message("伤害过低，请确保填入伤害值高于1000\r\n");
                                        }
                                        else
                                        {
                                            message += new Message("无法识别伤害，请确保填入伤害值正确\r\n");
                                        }
                                        isCorrect = false;
                                    }
                                }
                                else if (e.ToLower().Contains("k"))
                                {
                                    try
                                    {
                                        decimal result = decimal.Parse(Regex.Replace(e, @"[^\d.\d]", ""));
                                        intDMG = int.Parse(decimal.Round((result * 1000), 0).ToString());
                                        if (intDMG > 3000000)
                                        {
                                            throw new Exception("由伤害过高检测块抛出");
                                        }
                                        if (intDMG < 1000)
                                        {
                                            throw new Exception("由伤害过低检测块抛出");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        if (intDMG > 3000000)
                                        {
                                            message += new Message("伤害过高，请确保填入伤害值正确\r\n");
                                        }
                                        else if (intDMG < 1000)
                                        {
                                            message += new Message("伤害过低，请确保填入伤害值高于1000\r\n");
                                        }
                                        else
                                        {
                                            message += new Message("无法识别伤害，请确保填入伤害值正确\r\n");
                                        }
                                        isCorrect = false;
                                    }
                                }
                                else if (Regex.Replace(e, @"[^0-9]+", "").Length > 4)//数字长度部分大于4
                                {
                                    try
                                    {
                                        intDMG = int.Parse(e);
                                        if (intDMG > 3000000)
                                        {
                                            throw new Exception("由伤害过高检测块抛出");
                                        }
                                        if (intDMG < 1000)
                                        {
                                            throw new Exception("由伤害过低检测块抛出");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex);
                                        if (intDMG > 3000000)
                                        {
                                            message += new Message("伤害过高，请确保填入伤害值正确\r\n");
                                        }
                                        else if (intDMG < 1000)
                                        {
                                            message += new Message("伤害过低，请确保填入伤害值高于1000\r\n");
                                        }
                                        else
                                        {
                                            message += new Message("无法识别伤害");
                                        }
                                        isCorrect = false;
                                    }
                                }
                            }
                            if (intDMG == -1)
                            {
                                message += new Message("无法识别伤害，可能由于格式错误\r\n");
                                isCorrect = false;
                            }
                            if (intBossCode == 0)
                            {
                                message += new Message("无法识别BOSS代码，可能由于格式错误\r\n");
                                isCorrect = false;
                            }
                            if (intRound == 0)
                            {
                                message += new Message("无法识别周目数，可能由于格式错误\r\n");
                                isCorrect = false;
                            }
                            if (isCorrect)
                            {
                                if (RecordDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode, intExTime, out int intEID))
                                {
                                    Console.WriteLine(intBossCode.ToString() + " " + intRound.ToString() + " " + intDMG.ToString());
                                    Console.WriteLine(intEID.ToString());
                                    message = new Message("伤害已保存，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                                    goto case "queuequit";
                                }
                                else
                                {
                                    message += new Message("与数据库失去连接，伤害保存失败。\r\n");
                                }
                            }
                            else
                            {
                                message += new Message("输入【@MahoBot help】获取帮助。\r\n");
                            }
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            break;
                        }
                    case "help":
                        {
                            message += new Message("加入队列：【@MahoBot c1】可进入队列\r\n");
                            message += new Message("查询队列：【@MahoBot c2】可查询当前在队列中的人\r\n");
                            message += new Message("退出队列：【@MahoBot c3】可离开队列\r\n");
                            message += new Message("清空队列：【@MahoBot 清空队列】可清空当前队列（仅限群主/管理员使用）\r\n");
                            message += new Message("伤害记录：【@MahoBot 伤害 B(n) (n)周目 （伤害值）】（如@MahoBot 伤害 B2 6周目 1374200）\r\n 伤害值可如137w等模糊格式\r\n");
                            message += new Message("额外时间的伤害记录：【@MahoBot 伤害 补时 B(n) (n)周目 （伤害值）】\r\n");
                            message += new Message("掉线记录：【@MahoBot 掉线 (是否补时)】可记录一次掉线或额外时间掉线\r\n");
                        }
                        message += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        break;
                    case "timeout":
                        {
                            int intDMG = 0;
                            int intRound = 0;
                            int intBossCode = 0;
                            int intExTime = 0;
                            if (cmdContext.Contains("补时"))
                            {
                                intExTime = 1;
                            }
                            if (RecordDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode, intExTime, out int intEID))
                            {
                                message = new Message("掉线已记录，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                                goto case "queuequit";
                            }
                            else
                            {
                                message += new Message("与数据库失去连接，掉线记录失败。\r\n");
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            }
                        }
                        break;
                    case "dmgmod":
                        {
                            int intEID = 0;
                            int intRound = 0;
                            int intDMG = -1;
                            int intBossCode = 0;
                            int intExTime = 0;
                            string strOriUID = "";
                            string strNewUID = "";
                            string[] sArray = cmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
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
                                    if (RecordDAL.QueryDamageRecord(intEID, strGrpID, out DataTable dtDmgRec))
                                    {
                                        if (dtDmgRec.Rows.Count < 1)
                                        {
                                            Console.WriteLine("输入的档案号：" + intEID + " 未能找到数据。");
                                            message += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else if (dtDmgRec.Rows.Count > 1)
                                        {
                                            Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                                            message += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            //给结果值先赋上原始数据，以简化判断
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
                                        message += new Message("与数据库失去连接，修改失败。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                }
                                else if (e.ToLower().Contains("u"))
                                {
                                    if (!double.TryParse(e.ToLower().Replace("u", ""), out double douOutUID))
                                    {
                                        Console.WriteLine("输入的qq号并非全数字或无法转换成double");
                                        message += new Message("用户ID请填入数字QQ号。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                    else
                                    {
                                        strNewUID = douOutUID.ToString();
                                    }
                                }
                                else if (e.ToLower().Contains("b"))
                                {
                                    if (int.TryParse(e.ToLower().Replace("b", ""), out int intOutBC))
                                    {
                                        if (intOutBC > 5 || intOutBC < 1)
                                        {
                                            Console.WriteLine("输入的BOSS代码数值超出范围（1~5），原始信息=" + e.ToString());
                                            message += new Message("输入的BOSS代码数值超出范围（1~5）。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            intBossCode = intOutBC;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("输入的BOSS代码含有非数字，原始信息=" + e.ToString());
                                        message += new Message("输入的BOSS代码无法识别。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                }
                                else if (e.Contains("周目"))
                                {
                                    if (int.TryParse(e.Replace("周目", ""), out int intOutRound))
                                    {
                                        if (intOutRound > 30 || intOutRound < 1)
                                        {
                                            Console.WriteLine("输入的周目数值超出范围（1~30），原始信息=" + e.ToString());
                                            message += new Message("输入的周目数值超出范围（1~30）。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            intRound = intOutRound;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("输入的周目无法识别，原始信息=" + e.ToString());
                                        message += new Message("输入的周目无法识别。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                }
                                else if (e.ToLower().Contains("w") || e.Contains("万") || e.ToLower().Contains("k"))
                                {
                                    if (int.TryParse(e.Replace("w", ""), out int intOutDMG))
                                    {
                                        if (intOutDMG > 300 || intOutDMG < 10)
                                        {
                                            Console.WriteLine("输入的伤害数值不符合范围（10w~300w），原始信息=" + e.ToString());
                                            message += new Message("输入的伤害数值不符合范围（10w~300w）。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            intDMG = intOutDMG * 10000;
                                        }
                                    }
                                    else if (int.TryParse(e.Replace("万", ""), out int intOutDMG2))
                                    {
                                        if (intOutDMG2 > 300 || intOutDMG2 < 10)
                                        {
                                            Console.WriteLine("输入的伤害数值不符合范围（10万~300万），原始信息=" + e.ToString());
                                            message += new Message("输入的伤害数值不符合范围（10万~300万）。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            intDMG = intOutDMG2 * 10000;
                                        }
                                    }
                                    else if (int.TryParse(e.Replace("k", ""), out int intOutDMG3))
                                    {
                                        if (intOutDMG3 > 3000 || intOutDMG3 < 100)
                                        {
                                            Console.WriteLine("输入的伤害数值不符合范围（100k~3000k），原始信息=" + e.ToString());
                                            message += new Message("输入的伤害数值不符合范围（100k~3000k）。\r\n");
                                            message += Message.At(long.Parse(strUserID));
                                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                            return;
                                        }
                                        else
                                        {
                                            intDMG = intOutDMG3 * 1000;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("输入的伤害数值无法识别，原始信息=" + e.ToString());
                                        message += new Message("输入的伤害数值无法识别。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                }
                                else if (int.TryParse(e, out int intOutDMG4))
                                {
                                    if (intOutDMG4 > 3000000 || intOutDMG4 < 100000)
                                    {
                                        Console.WriteLine("输入的伤害数值不符合范围（100000~3000000），原始信息=" + e.ToString());
                                        message += new Message("输入的伤害数值不符合范围（100000~3000000）。\r\n");
                                        message += Message.At(long.Parse(strUserID));
                                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                        return;
                                    }
                                    else
                                    {
                                        intDMG = intOutDMG4;
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
                            }
                            if (strUserID == strOriUID || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
                            {//仅允许本人或管理员进行修改
                                if (RecordDAL.DamageUpdate(strGrpID, strNewUID, intDMG, intRound, intBossCode, intExTime, intEID))
                                {
                                    message += new Message("修改成功\r\n");//尝试直接穿透至dmgshow以减少代码量
                                    goto case "dmgshow";//尝试直接穿透至dmgshow以减少代码量
                                    //if (RecordDAL.QueryDamageRecord(intEID, strGrpID, out DataTable dtDmgRec))
                                    //{
                                    //    if (dtDmgRec.Rows.Count < 1)
                                    //    {
                                    //        Console.WriteLine("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                    //        message += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                    //    }
                                    //    else if (dtDmgRec.Rows.Count > 1)
                                    //    {
                                    //        Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                                    //        message += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
                                    //    }
                                    //    else
                                    //    {
                                    //        string strRUID = dtDmgRec.Rows[0]["userid"].ToString();
                                    //        string strRDmg = dtDmgRec.Rows[0]["dmg"].ToString();
                                    //        string strRRound = dtDmgRec.Rows[0]["round"].ToString();
                                    //        string strRBC = dtDmgRec.Rows[0]["bc"].ToString();
                                    //        string strREXT = dtDmgRec.Rows[0]["extime"].ToString();
                                    //        string resultString = "";
                                    //        if (dtDmgRec.Rows[0]["dmg"].ToString() == "0")
                                    //        {
                                    //            if (strREXT == "1")
                                    //            {
                                    //                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害= 0(掉线) （补时）";
                                    //            }
                                    //            else
                                    //            {
                                    //                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害= 0(掉线)";
                                    //            }
                                    //        }
                                    //        else if (dtDmgRec.Rows[0]["dmg"].ToString() != "0")
                                    //        {
                                    //            if (strREXT == "1")
                                    //            {
                                    //                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg + " （补时）";
                                    //            }
                                    //            else
                                    //            {
                                    //                resultString = "UID=" + strRUID + "；" + strRRound + "周目；B" + strRBC + "；伤害=" + strRDmg;
                                    //            }
                                    //        }
                                    //        else
                                    //        {
                                    //            Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRec.Rows[0]["dmg"].ToString());
                                    //            message += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                    //            message += Message.At(long.Parse(strUserID));
                                    //            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                    //            return;
                                    //        }
                                    //        message += new Message("修改成功，目前档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    message += new Message("与数据库失去连接，查询失败。\r\n");
                                    //}
                                }
                                else
                                {
                                    message += new Message("与数据库失去连接，修改失败。\r\n");
                                }
                            }
                            else
                            {
                                Console.WriteLine("只有本人或管理员以上可修改。修改者：" + strUserID + " 原记录：" + strOriUID + "EventID：" + intEID.ToString());
                                message += new Message("只有本人或管理员以上可修改。\r\n");
                            }
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            //显示上传内容
                        }
                        break;
                    case "dmgshow":
                        {
                            int intEID = 0;
                            string[] sArray = cmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
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
                                    if (RecordDAL.QueryDamageRecord(intEID, strGrpID, out DataTable dtDmgRec))
                                    {
                                        if (dtDmgRec.Rows.Count < 1)
                                        {
                                            Console.WriteLine("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                            message += new Message("输入的档案号：" + intEID + " 未能找到数据。\r\n");
                                        }
                                        else if (dtDmgRec.Rows.Count > 1)
                                        {
                                            Console.WriteLine("输入的档案号：" + intEID + " 返回非唯一结果。");
                                            message += new Message("输入的档案号：" + intEID + " 返回非唯一结果，请联系维护团队。\r\n");
                                        }
                                        else
                                        {
                                            string strRUID = dtDmgRec.Rows[0]["userid"].ToString();
                                            string strRDmg = dtDmgRec.Rows[0]["dmg"].ToString();
                                            string strRRound = dtDmgRec.Rows[0]["round"].ToString();
                                            string strRBC = dtDmgRec.Rows[0]["bc"].ToString();
                                            string strREXT = dtDmgRec.Rows[0]["extime"].ToString();
                                            string resultString = "";
                                            if (dtDmgRec.Rows[0]["dmg"].ToString() == "0")
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
                                            else if (dtDmgRec.Rows[0]["dmg"].ToString() != "0")
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
                                                Console.WriteLine("写出伤害时出现意料外的错误，dtDmgRec.Rows[0][dmg].ToString()=" + dtDmgRec.Rows[0]["dmg"].ToString());
                                                message += new Message("出现意料外的错误，请联系维护团队。\r\n");
                                                message += Message.At(long.Parse(strUserID));
                                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                                                return;
                                            }
                                            message += new Message("档案号" + intEID + "的数据为：\r\n" + resultString + "\r\n");
                                        }
                                    }
                                }
                            }
                            if (intEID > 0)
                            {
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            }
                            else
                            {
                                Console.WriteLine("未能识别输入的档案号。" + cmdContext.ToString());
                                message += new Message("未能识别输入的档案号。\r\n");
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            }
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
