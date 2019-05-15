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
        private static void GroupVerification(string strGrpID)
        {
            if (QueueDAL.GroupRegVerify(strGrpID, out DataTable dtVfyResult))
            {
                if (dtVfyResult.Rows.Count == 1)
                {
                    int intGrpStat = int.Parse(dtVfyResult.Rows[0]["ORG_STAT"].ToString());
                    int intGrpType = int.Parse(dtVfyResult.Rows[0]["ORG_TYPE"].ToString());
                    if (intGrpStat != 1)
                    {
                        MsgMessage += new Message("本群已关闭bot功能，请联系bot维护团队。\r\n");
                        Console.WriteLine("群：" + strGrpID + "进行群有效性查询时，查询结果不为1");
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                        return;
                    }
                    if (intGrpType != 0)
                    {
                        //非公主连接，等待下一个程序响应
                        return;
                    }
                }
                else
                {
                    MsgMessage += new Message("本群激活状态有误，请联系bot维护团队。\r\n");
                    Console.WriteLine("群：" + strGrpID + "进行群有效性查询时，查询结果不为1");
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                    return;
                }
            }
            else
            {
                MsgMessage += new Message("验证时连接数据库失败，请联系bot维护团队。\r\n");
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
        }
        /// <summary>
        /// 消息
        /// </summary>
        protected static Message MsgMessage;

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
            string strGrpID = receivedMessage.GetType().GetProperty("GroupId").GetValue(receivedMessage, null).ToString();
            if (strGrpID == "569396886" || strGrpID == "877184755")
            //if (strGrpID == "877184755")
            {
                //2019.05.15
                //GroupMsgBLL4D2.D2MsgHandler(receivedMessage, memberInfo);
                return;
            }
            if (strRawcontext.Contains(cmdAtMeAlone))
            {
                var message = new Message("");
                GroupVerification(strGrpID);
                Console.WriteLine("接收到一条来自群：" + strGrpID + "的Notice，开始解析内容");
                //分离命令头和命令体，命令头：功能识别区，命令体：数据包含区。
                string strCmdHead = strRawcontext.Replace(cmdAtMeAlone, "").Trim().Split(' ')[0];
                string strCmdContext = strRawcontext.Replace(cmdAtMeAlone, "").Replace(strCmdHead, "").Trim();
                string strUserID = receivedMessage.UserId.ToString();
                string strUserGrpCard = memberInfo.InGroupName.ToString().Trim();
                string strUserNickName = memberInfo.Nickname.ToString().Trim();
                if (strUserGrpCard == null || strUserGrpCard == "")
                {
                    strUserGrpCard = strUserNickName;
                }
                string cmdType = "";
                if (strCmdHead.ToLower() == "c1" || strCmdHead.ToLower() == "排队")
                {
                    cmdType = "queueadd";
                    Console.WriteLine("识别为开始排刀");
                }
                else if (strCmdHead.ToLower() == "c2" || strCmdHead.ToLower() == "查看排队")
                {
                    cmdType = "queueshow";
                    Console.WriteLine("识别为查询排刀");
                }
                else if (strCmdHead.ToLower() == "c3" || strCmdHead.ToLower() == "退出排队")
                {
                    cmdType = "queuequit";
                    Console.WriteLine("识别为退出排刀");
                }
                else if (strCmdHead.ToLower() == "清空队列")
                {
                    //管理功能，不设快捷键
                    cmdType = "clear";
                    Console.WriteLine("识别为清空指令");
                }
                else if (strCmdHead.ToLower() == "dmg" || strCmdHead.ToLower() == "伤害")
                {
                    cmdType = "debrief";
                    Console.WriteLine("识别为伤害上报");
                }
                else if (strCmdHead.ToLower() == "help")
                {
                    cmdType = "help";
                    Console.WriteLine("识别为说明书呈报");
                }
                else if (strCmdHead.ToLower() == "掉线")
                {
                    cmdType = "timeout";
                    Console.WriteLine("识别为掉线");
                }
                else if (strCmdHead.ToLower() == "mod" || strCmdHead.ToLower() == "修改")
                {
                    cmdType = "dmgmod";
                    Console.WriteLine("识别为伤害修改");
                }
                else if (strCmdHead.ToLower() == "show" || strCmdHead.ToLower() == "查看")
                {
                    cmdType = "dmgshow";
                    Console.WriteLine("识别为伤害查看");
                }
                else if (strCmdHead.ToLower() == "f1" || strCmdHead.ToLower() == "查刀")
                {
                    cmdType = "remainshow";
                    Console.WriteLine("识别为未出满三刀的成员查询");
                }
                else if (strCmdHead.ToLower() == "f2" || strCmdHead.ToLower() == "提醒出刀")
                {
                    cmdType = "remainnotice";
                    Console.WriteLine("识别为提醒未出满三刀的成员");
                }
                else if (strCmdHead.ToLower() == "nla" || strCmdHead.ToLower() == "报名")
                {
                    cmdType = "namelistalt";
                    Console.WriteLine("识别为名单列表增加指定人或更新指定人");
                }
                else if (strCmdHead.ToLower() == "nls" || strCmdHead.ToLower() == "查看报名")
                {
                    cmdType = "namelistshow";
                    Console.WriteLine("识别为展示名单列表");
                }
                else if (strCmdHead.ToLower() == "nld" || strCmdHead.ToLower() == "删除报名")
                {
                    cmdType = "namelistdel";
                    Console.WriteLine("识别为名单列表删除指定人");
                }
                else if (strCmdHead.ToLower() == "s1" || strCmdHead.ToLower() == "订阅")
                {
                    //需要另外参数
                    cmdType = "bosssubsadd";
                    Console.WriteLine("识别为新增BOSS订阅");
                }
                else if (strCmdHead.ToLower() == "s2" || strCmdHead.ToLower() == "查看订阅")
                {
                    cmdType = "bosssubsshow";
                    Console.WriteLine("识别为查看BOSS订阅");
                }
                else if (strCmdHead.ToLower() == "s3" || strCmdHead.ToLower() == "退订")
                {
                    //需要另外参数
                    cmdType = "bosssubscancel";
                    Console.WriteLine("识别为取消boss订阅");
                }
                else if (strCmdHead.ToLower() == "测试")
                {
                    cmdType = "test";
                    Console.WriteLine("test");
                }
                else
                {
                    cmdType = "unknown";
                    Console.WriteLine("识别失败，未从已知功能中发现特征");
                }
                switch (cmdType)
                {
                    case "queueadd":
                        {
                            CaseQueue.QueueAdd(strGrpID, strUserID, strUserGrpCard);
                        }
                        break;
                    case "queueshow":
                        {
                            CaseQueue.QueueShow(strGrpID, strUserID);
                        }
                        break;
                    case "queuequit":
                        {
                            CaseQueue.QueueQuit(strGrpID, strUserID);
                        }
                        break;
                    case "clear":
                        {
                            CaseQueue.QueueClear(strGrpID, strUserID, memberInfo);
                        }
                        break;
                    case "debrief":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += new Message("无法读取上限值设置，请联系维护人员");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            CaseDamage.DmgRecAdd(strGrpID, strUserID, strCmdContext);
                        }
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
                            message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), message).Wait();
                        }
                        break;
                    case "timeout":
                        {
                            CaseDamage.DmgTimeOut(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "dmgmod":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += new Message("无法读取上限值设置，请联系维护人员");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            CaseDamage.DmgModify(strGrpID, strUserID, strCmdContext, memberInfo);
                        }
                        break;
                    case "dmgshow":
                        {
                            CaseDamage.RecordQuery(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "remainshow":
                        {
                            CaseRemind.ShowRemainStrikes(strGrpID, strUserID);
                        }
                        break;
                    case "remainnotice":
                        {
                            CaseRemind.NoticeRemainStrikers(strGrpID, strUserID, memberInfo);
                        }
                        break;
                    case "namelistalt":
                        {
                            CaseNameList.NameListAdd(strGrpID, strUserID, strUserGrpCard);
                        }
                        break;
                    case "namelistshow":
                        {
                            CaseNameList.NameListShow(strGrpID, strUserID);
                        }
                        break;
                    case "namelistdel":
                        {
                            CaseNameList.NameListDelete(strGrpID, strUserID, strCmdContext, memberInfo);
                        }
                        break;
                    case "bosssubsadd":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += new Message("无法读取上限值设置，请联系维护人员");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            CaseSubscribe.SubsAdd(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "bosssubsshow":
                        {
                            CaseSubscribe.SubsShow(strGrpID, strUserID);
                        }
                        break;
                    case "bosssubscancel":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += new Message("无法读取上限值设置，请联系维护人员");
                                MsgMessage += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                                return;
                            }
                            CaseSubscribe.SubsDel(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "test":
                        {
                            //CaseHelp.test();
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
