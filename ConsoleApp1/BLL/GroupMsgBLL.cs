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
                else if (strCmdContext.ToLower() == "nla")
                {
                    cmdType = "namelistalt";
                    Console.WriteLine("识别为名单列表增加指定人或更新指定人");
                }
                else if (strCmdContext.ToLower() == "nls")
                {
                    cmdType = "namelistshow";
                    Console.WriteLine("识别为展示名单列表");
                }
                else if (strCmdContext.Contains("nld"))
                {
                    cmdType = "namelistdel";
                    Console.WriteLine("识别为名单列表删除指定人");
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
