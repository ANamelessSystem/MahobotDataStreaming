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
    class CaseQueue : GroupMsgBLL
    {
        /// <summary>
        /// 加入队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueAdd(string strGrpID, string strUserID, string strUserGrpCard)
        {
            if (QueueDAL.AddQueue(strGrpID, strUserID, strUserGrpCard))
            {
                MsgMessage += new Message("已加入队列\r\n--------------------\r\n");
                QueueShow(strGrpID, strUserID);
            }
            else
            {
                Console.WriteLine("与数据库失去连接，加入队列失败。\r\n");
                MsgMessage += new Message("与数据库失去连接，加入队列失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }

        /// <summary>
        /// 展示队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueShow(string strGrpID, string strUserID)
        {
            if (ConsoleProperties.IsHpShow)
            {
                //Console.WriteLine("查询HP前的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                HpShow(strGrpID, strUserID);
                //Console.WriteLine("查询HP后的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            }
            else
            {
                Console.WriteLine("未打开HP计算功能。");
            }
            if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue))
            {
                if (dtQueue.Rows.Count > 0)
                {
                    MsgMessage += new Message("目前队列：\r\n");
                    //Console.WriteLine("队列查询循环开始的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                    for (int i = 0; i < dtQueue.Rows.Count; i++)
                    {
                        string strOutput = "顺序：" + dtQueue.Rows[i]["seq"].ToString() + "    " + dtQueue.Rows[i]["name"].ToString() + "(" + dtQueue.Rows[i]["id"].ToString() + ")";
                        MsgMessage += new Message(strOutput + "\r\n");
                        Console.WriteLine(strOutput);
                    }
                    //Console.WriteLine("队列查询循环结束后的信息：\r\n" + MsgMessage.Raw.ToString()+"(信息结束)");
                }
                else
                {
                    Console.WriteLine("队列中无人");
                    MsgMessage += new Message("目前队列中无人。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
            }
            //Console.WriteLine("发送最终结果前的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            //Console.WriteLine("发送最终结果后的信息：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
            //throw new Exception("调试，强制中断程序");
        }

        /// <summary>
        /// 退出队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueQuit(string strGrpID, string strUserID)
        {
            if (QueueDAL.QuitQueue(strGrpID, strUserID, out int deletedCount))
            {
                if (deletedCount > 0)
                {
                    Console.WriteLine("已将群：" + strGrpID + "，" + strUserID + "较早一刀移出队列。");
                    MsgMessage += new Message("已将较早一次队列记录退出。\r\n--------------------\r\n");
                }
                else
                {
                    Console.WriteLine("群：" + strGrpID + "，" + strUserID + "移出队列失败：未找到记录。");
                    MsgMessage += new Message("未找到队列记录，这可能是一次未排刀的伤害上报。\r\n--------------------\r\n");
                }
                //Console.WriteLine("展示队列前的信息输出：\r\n" + MsgMessage.Raw.ToString() + "(信息结束)");
                QueueShow(strGrpID, strUserID);
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，退出队列失败。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void QueueClear(string strGrpID, string strUserID, GroupMemberInfo memberInfo)
        {
            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (QueueDAL.ShowQueue(strGrpID, out DataTable dtQueue_old))
                {
                    if (dtQueue_old.Rows.Count > 0)
                    {
                        if (QueueDAL.ClearQueue(strGrpID, out int deletedCount))
                        {
                            MsgMessage += new Message("已清空队列。\r\n--------------------\r\n");
                            Console.WriteLine("执行清空队列指令成功，共有" + deletedCount + "条记录受到影响");
                            MsgMessage += new Message("由于队列被清空，请以下成员重新排队：");
                            for (int i = 0; i < dtQueue_old.Rows.Count; i++)
                            {
                                string strUID = dtQueue_old.Rows[i]["id"].ToString();
                                MsgMessage += new Message("\r\nID：" + strUID + "， ") + Message.At(long.Parse(strUID));
                            }
                        }
                        else
                        {
                            Console.WriteLine("与数据库失去连接，清空队列失败。");
                            MsgMessage += new Message("与数据库失去连接，清空队列失败。\r\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("执行清空队列指令失败，队列中无人。");
                        MsgMessage += new Message("队列中无人，不需要清空。\r\n");
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，查询队列失败。\r\n");
                }
            }
            else
            {
                Console.WriteLine("执行清空队列指令失败，由权限不足的人发起");
                MsgMessage += new Message("拒绝：仅有管理员或群主可执行队列清空指令。\r\n");
            }
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 显示血量
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void HpShow(string strGrpID, string strUserID)
        {
            if (RecordDAL.GetBossProgress(strGrpID, out DataTable dtBossProgress))
            {
                try
                {
                    string strHpRemain = dtBossProgress.Rows[0]["hpremain"].ToString();
                    string strOutput2 = "";
                    if (strHpRemain.Length > 4 && !strHpRemain.Contains("-"))
                    {
                        strOutput2 = "目前进度：" + dtBossProgress.Rows[0]["maxround"].ToString() + "周目，B" + dtBossProgress.Rows[0]["maxbc"].ToString() + "，剩余血量(推测)=" + strHpRemain.Substring(0, strHpRemain.Length - 4) + "万";
                        MsgMessage += new Message(strOutput2 + "\r\n--------------------\r\n");
                    }
                    else if (strHpRemain.Length > 0)
                    {
                        strOutput2 = "目前进度：" + dtBossProgress.Rows[0]["maxround"].ToString() + "周目，B" + dtBossProgress.Rows[0]["maxbc"].ToString() + "，剩余血量(推测)=" + strHpRemain;
                        MsgMessage += new Message(strOutput2 + "\r\n--------------------\r\n");
                    }
                    Console.WriteLine(strOutput2);
                }
                catch (Exception ex)
                {
                    MsgMessage += new Message("遇到未知错误，查询剩余HP失败。\r\n");
                    Console.WriteLine(ex);
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询剩余HP失败。\r\n");
                Console.WriteLine("与数据库失去连接，查询剩余HP失败。\r\n");
            }
        }
    }
}
