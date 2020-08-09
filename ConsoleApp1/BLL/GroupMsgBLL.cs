using System;
using System.Data;
using Marchen.Model;
using System.Text.RegularExpressions;
using Marchen.DAL;
using Mirai_CSharp.Models;
using System.Threading.Tasks;
using Mirai_CSharp;

namespace Marchen.BLL
{
    class GroupMsgBLL
    {
        private static bool GroupVerification(string strGrpID)
        {
            bool bResullt = true;
            if (QueueDAL.GroupRegVerify(strGrpID, out DataTable dtVfyResult))
            {
                if (dtVfyResult.Rows.Count == 1)
                {
                    int intGrpStat = int.Parse(dtVfyResult.Rows[0]["ORG_STAT"].ToString());
                    int intGrpType = int.Parse(dtVfyResult.Rows[0]["ORG_TYPE"].ToString());
                    if (intGrpStat != 1)
                    {
                        bResullt = false;
                    }
                    if (intGrpType != 0)
                    {
                        bResullt = false;//非公主连接，等待下一个程序响应
                    }
                }
                else
                {
                    bResullt = false;
                }
            }
            else
            {
                bResullt = false;
            }
            return bResullt;
        }
        /// <summary>
        /// 消息
        /// </summary>
        protected static string MsgMessage;
        

        /// <summary>
        /// 解析收到的来自群的内容
        /// </summary>
        /// <param name="receivedMessage">收到的消息内容</param>
        /// <param name="memberInfo">发出此消息的用户信息</param>
        public static async Task<bool> GrpMsgReco(GroupPermission mbrAuth, string strUserGrpCard, string strGrpID, string strUserID, string strMsgInput)
        {
            MsgMessage = "";
            string strRawcontext = strMsgInput.Trim();
            string cmdAtMeAlone = "[CQ:at,qq=" + SelfProperties.SelfID + "]";
            if (strRawcontext.Contains(cmdAtMeAlone))
            {
                if (!GroupVerification(strGrpID))
                {
                    MsgMessage += "管理者未在本群开放功能，请联系维护团队。";
                    IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                    await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                    return false;
                }
                Console.WriteLine("接收到一条来自群：" + strGrpID + "的Notice，开始解析内容");
                //分离命令头和命令体，命令头(strCmdHead)：功能识别区，命令体(strCmdContext)：数据包含区。
                string strCmdHead = strRawcontext.Replace(cmdAtMeAlone, "").Trim().Split(' ')[0];
                string strCmdContext = strRawcontext.Replace(cmdAtMeAlone, "").Replace(strCmdHead, "").Trim();
                string strSpecCmdText = strRawcontext.Replace(cmdAtMeAlone, "").Trim();
                string cmdType = "";
                if (strCmdHead.ToLower() == "c1" || strCmdHead == "排队")
                {
                    cmdType = "queueadd";
                    Console.WriteLine("识别为开始排刀");
                }
                else if (strCmdHead.ToLower() == "c2" || strCmdHead == "查看排队")
                {
                    cmdType = "queueshow";
                    Console.WriteLine("识别为查询排刀");
                }
                else if (strCmdHead.ToLower() == "c3" || strCmdHead == "退出排队")
                {
                    cmdType = "queuequit";
                    Console.WriteLine("识别为退出排刀");
                }
                else if (strCmdHead.ToLower() == "clear" || strCmdHead == "清空队列")
                {
                    cmdType = "clear";
                    Console.WriteLine("识别为清空指令");
                }
                else if (strCmdHead.ToLower() == "dmg" || strCmdHead == "伤害")
                {
                    cmdType = "debrief";
                    Console.WriteLine("识别为伤害上报");
                }
                else if (strCmdHead.ToLower() == "help")
                {
                    cmdType = "help";
                    Console.WriteLine("识别为说明书呈报");
                }
                else if (strCmdHead.ToLower() == "mod" || strCmdHead == "修改")
                {
                    cmdType = "dmgmod";
                    Console.WriteLine("识别为伤害修改");
                }
                else if (strCmdHead.ToLower() == "show" || strCmdHead == "查看")
                {
                    cmdType = "dmgshow";
                    Console.WriteLine("识别为伤害查看");
                }
                else if (strCmdHead.ToLower() == "f1" || strCmdHead == "查刀")
                {
                    cmdType = "remainshow";
                    Console.WriteLine("识别为未出满三刀的成员查询");
                }
                else if (strCmdHead.ToLower() == "f2" || strCmdHead == "提醒出刀")
                {
                    cmdType = "remainnotice";
                    Console.WriteLine("识别为提醒未出满三刀的成员");
                }
                else if (strCmdHead.ToLower() == "nla" || strCmdHead == "报名")
                {
                    cmdType = "namelistalt";
                    Console.WriteLine("识别为名单列表增加指定人或更新指定人");
                }
                else if (strCmdHead.ToLower() == "nls" || strCmdHead == "查看报名")
                {
                    cmdType = "namelistshow";
                    Console.WriteLine("识别为展示名单列表");
                }
                else if (strCmdHead.ToLower() == "nld" || strCmdHead == "删除报名")
                {
                    cmdType = "namelistdel";
                    Console.WriteLine("识别为名单列表删除指定人");
                }
                else if (strCmdHead.ToLower() == "s1" || strCmdHead == "订阅")
                {
                    cmdType = "bosssubsadd";
                    Console.WriteLine("识别为新增BOSS订阅");
                }
                else if (strCmdHead.ToLower() == "s2" || strCmdHead == "查看订阅")
                {
                    cmdType = "bosssubsshow";
                    Console.WriteLine("识别为查看BOSS订阅");
                }
                else if (strCmdHead.ToLower() == "s3" || strCmdHead == "退订")
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
                else if (strCmdHead.ToLower() == "sos" || strCmdHead == "救")
                {
                    cmdType = "sos";
                    Console.WriteLine("挂树等救");
                }
                //else if (strCmdHead.ToLower() == "score")
                //{
                //    cmdType = "score";
                //    Console.WriteLine("算分");
                //}
                else if (strSpecCmdText == "initialize member list")
                {
                    cmdType = "namelistinit";
                    Console.WriteLine("初始化(清空)名单");
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
                            CaseQueue.QueueAdd(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "queueshow":
                        {
                            CaseQueue.QueueShow(strGrpID, strUserID);
                        }
                        break;
                    case "queuequit":
                        {
                            CaseQueue.QueueQuit(strGrpID, strUserID, 0);
                        }
                        break;
                    case "clear":
                        {
                            CaseQueue.QueueClear(strGrpID, strUserID, mbrAuth);
                        }
                        break;
                    case "debrief":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
                            CaseDamage.DmgRecAdd(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "help":
                        {
                            MsgMessage +=
                                "加入队列：【@MahoBot c1】可进入队列\r\n" +
                                "查询队列：【@MahoBot c2】可查询当前在队列中的人\r\n" +
                                "退出队列：【@MahoBot c3】可离开队列\r\n" +
                                "伤害记录：【@MahoBot 伤害 B(n) （伤害值）】（如@MahoBot 伤害 B2 1374200）\r\n 伤害值可如137w等模糊格式\r\n" +
                                "尾刀的伤害记录：【@MahoBot 伤害 尾刀 B(n) （伤害值）】\r\n" +
                                "掉线记录：【@MahoBot 伤害 掉线】可记录一次掉线\r\n" +
                                "其他功能及用例请参考命令表\r\n https://docs.qq.com/sheet/DRGthS3JpS1ZibHlL?opendocxfrom=admin&preview_token=&coord=F27A0C0&tab=BB08J2 \r\n" +
                                "加入队列：【@MahoBot c1】可进入队列\r\n";
                            IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                            await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                        }
                        break;
                    case "dmgmod":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
                            CaseDamage.DmgModify(strGrpID, strUserID, strCmdContext, mbrAuth);
                        }
                        break;
                    case "dmgshow":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
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
                            //CaseRemind.NoticeRemainStrikers(strGrpID, strUserID, mbrAuth);
                            IMessageBase[] chain  = new IMessageBase[] { new PlainMessage("本功能暂时关闭中。") };
                            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
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
                            CaseNameList.NameListDelete(strGrpID, strUserID, strCmdContext, mbrAuth);
                        }
                        break;
                    case "bosssubsadd":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
                            CaseSubscribe.SubsAdd(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "bosssubsshow":
                        {
                            CaseSubscribe.SubsShow(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "bosssubscancel":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
                            CaseSubscribe.SubsDel(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    case "test":
                        {
                            //CaseHelp.test();
                        }
                        break;
                    case "sos":
                        {
                            if (!CmdHelper.LoadValueLimits())
                            {
                                Console.WriteLine("无法读取上限值设置，程序中断");
                                MsgMessage += "无法读取上限值设置，请联系维护人员。";
                                IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                                await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                                return false;
                            }
                            CaseQueue.QueueSos(strGrpID, strUserID, strCmdContext);
                        }
                        break;
                    //case "score":
                    //    {
                    //        CaseStatistics.ShowScoreNow(strGrpID,strCmdContext);
                    //    }
                    //    break;
                    case "namelistinit":
                        {
                            CaseNameList.InitNameList(strGrpID, mbrAuth);
                        }
                        break;
                    case "unknown":
                        {
                            MsgMessage += "无法识别内容,输入【@MahoBot help】以查询命令表。\r\n";
                            IMessageBase[] chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
                            await ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain);
                        }
                        break;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
