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
        public static void GrpMsgReco(MessageContext receivedMessage,GroupMemberInfo memberInfo)
        {
            string strRawcontext = receivedMessage.RawMessage.ToString().Trim();
            string cmdAtMeAlone = "[CQ:at,qq=" + SelfProperties.SelfID + "]";
            string cmdContext = "";
            string strGrpID = receivedMessage.GetType().GetProperty("GroupId").GetValue(receivedMessage, null).ToString();
            string strUserID = receivedMessage.UserId.ToString();
            string strUserGrpCard = memberInfo.InGroupName.ToString();
            int vfyCode = GroupMsgDAL.GroupRegVerify(strGrpID);
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
                else if (cmdContext.Contains("伤害"))
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
                        if (GroupMsgDAL.AddQueue(strGrpID, strUserID, strUserGrpCard))
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
                        if (GroupMsgDAL.ShowQueue(strGrpID, out DataTable dtQueue))
                        {
                            if (dtQueue.Rows.Count > 0)
                            {
                                message += new Message("目前队列：\r\n");
                                for (int i = 0; i < dtQueue.Rows.Count; i++)
                                {
                                    string strOutput = "顺序：" + dtQueue.Rows[i]["seq"].ToString() + "    " + dtQueue.Rows[i]["name"].ToString() + "(" + dtQueue.Rows[i]["id"].ToString() + ")";
                                    string strMark = "←";
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
                        if (GroupMsgDAL.QuitQueue(strGrpID, strUserID, out int deletedCount))
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
                                if (GroupMsgDAL.ClearQueue(strGrpID, out deletedCount))
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
                                else if (e.Contains("补时"))
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
                                if (GroupMsgDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode,intExTime, out int intEID))
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
                            message += new Message("掉线记录：【@MahoBot 掉线 (是否补时)】可记录一次掉刀或额外时间掉刀\r\n");
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
                            if (GroupMsgDAL.DamageDebrief(strGrpID, strUserID, intDMG, intRound, intBossCode,intExTime, out int intEID))
                            {
                                message = new Message("掉刀已记录，档案号为： " + intEID.ToString() + "\r\n--------------------\r\n");
                                goto case "queuequit";
                            }
                            else
                            {
                                message += new Message("与数据库失去连接，掉刀记录失败。\r\n");
                                message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                            }
                        }
                        break;
                    case "dmgmod": { }break;
                    case "dmgshow":
                        {
                            int intEID = 0;
                            string[] sArray = cmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string e in sArray)
                            {
                                if (e.ToLower().Contains("e"))
                                {
                                    intEID = int.Parse(e.ToLower().Replace("e", ""));
                                    if (GroupMsgDAL.QueryDamageRecord(intEID, strGrpID, out DataTable dtDmgRec) && dtDmgRec.Rows.Count == 1)
                                    {
                                        string strRUID = dtDmgRec.Rows[0]["userid"].ToString();
                                        string strRDmg = dtDmgRec.Rows[0]["dmg"].ToString();
                                        string strRRound = dtDmgRec.Rows[0]["round"].ToString();
                                        string strRBC = dtDmgRec.Rows[0]["bc"].ToString();
                                        string strEXT = dtDmgRec.Rows[0]["extime"].ToString();
                                        string resultString = "";
                                        if (dtDmgRec.Rows[0]["dmg"].ToString() == "0")
                                        {
                                            resultString = "掉刀，无伤害";
                                        }
                                        else if (strEXT == "1")
                                        {
                                            resultString = strRRound + "周目，B" + strRBC + "，伤害：" + strRDmg + " 补时";
                                        }
                                        else
                                        {
                                            resultString = strRRound + "周目，B" + strRBC + "，伤害：" + strRDmg;
                                        }
                                        message += new Message("读出档案号：" + intEID + " 数据为：" + resultString + "\r\n");
                                    }
                                    else
                                    {
                                        message += new Message("与数据库失去连接，查询失败。\r\n");
                                    }
                                }
                            }
                            if (intEID > 0)
                            {
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
                            GroupMsgDAL.RecordUnknownContext(strGrpID, strUserID, cmdContext);
                        }
                        break;
                }
            }
        }
    }
}
