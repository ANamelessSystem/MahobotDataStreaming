using System;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Mirai_CSharp;
using Mirai_CSharp.Models;

namespace Marchen.BLL
{
    class CaseNameList : GroupMsgBLL
    {
        /// <summary>
        /// 更新或添加成员名单
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        public static void NameListAdd(string strGrpID, string strUserID, string strUserGrpCard)
        {
            IMessageBase[] chain;
            if (NameListDAL.UpdateNameList(strGrpID, strUserID, strUserGrpCard, out int intMemberCount))
            {
                MsgMessage += "已成功更新成员名单信息(" + intMemberCount.ToString() + "/30)。\r\n";
            }
            else if (intMemberCount == 30 || intMemberCount > 30)
            {
                MsgMessage += "名单已满30人，无法新增成员，请先清除无效成员。\r\n";
            }
            else
            {
                MsgMessage += "数据库错误，更新成员名单失败。\r\n";
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage),new AtMessage(long.Parse(strUserID),"") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }

        /// <summary>
        /// 展示成员名单
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void NameListShow(string strGrpID, string strUserID)
        {
            IMessageBase[] chain;
            if (NameListDAL.QryNameList(strGrpID, out DataTable dtNameList))
            {
                if (dtNameList.Rows.Count > 0)
                {
                    MsgMessage += "目前名单("+ dtNameList.Rows.Count + "/30)：\r\n";
                    for (int i = 0; i < dtNameList.Rows.Count; i++)
                    {
                        string strOutput = dtNameList.Rows[i]["MBRNAME"].ToString() + "(" + dtNameList.Rows[i]["MBRID"].ToString() + ")";
                        MsgMessage += strOutput + "\r\n";
                        Console.WriteLine(strOutput);
                    }
                }
                else
                {
                    Console.WriteLine("名单中无人");
                    MsgMessage += "目前名单中无人。\r\n";
                }
            }
            else
            {
                MsgMessage += "与数据库失去连接，查询名单失败。\r\n";
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }

        /// <summary>
        /// 删除成员名单
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void NameListDelete(string strGrpID, string strUserID, string strCmdContext, GroupPermission mbrAuth)
        {
            IMessageBase[] chain;
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += "输入【@MahoBot help】获取帮助。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (InputVariables.DouUID == -1)
            {
                MsgMessage += "未识别出需要删除的QQ号。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            if (strUserID == InputVariables.DouUID.ToString() || mbrAuth == GroupPermission.Owner || mbrAuth == GroupPermission.Administrator)
            {
                if (NameListDAL.QryNameList(strGrpID, out DataTable dtNameList))
                {
                    DataRow[] drExistsID = dtNameList.Select("MBRID='" + InputVariables.DouUID.ToString() + "'");
                    if (drExistsID.Length == 1)
                    {
                        if (NameListDAL.NameListDelete(strGrpID, InputVariables.DouUID.ToString()))
                        {
                            Console.WriteLine("已将群：" + strGrpID + "，" + InputVariables.DouUID.ToString() + "移除名单。");
                            MsgMessage += "已将" + InputVariables.DouUID.ToString() + "移出名单。";
                            //该功能已由触发器行级触发器完成
                            //if (SubscribeDAL.DelSubsAll(strGrpID, InputVariables.DouUID.ToString(), out int intDelCounts))
                            //{
                            //    Console.WriteLine("移除名单后清除订阅表成功。");
                            //}
                        }
                        else
                        {
                            MsgMessage += "与数据库失去连接，删除名单失败。\r\n";
                        }
                    }
                    else
                    {
                        MsgMessage += "未找到对应人员的名单记录，无法删除。\r\n";
                    }
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可删除对应名单。修改者：" + strUserID + " 原记录：" + InputVariables.DouUID.ToString());
                MsgMessage += "只有本人或管理员以上可删除对应的名单。\r\n";
                chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
                ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
                return;
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage), new AtMessage(long.Parse(strUserID), "") };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }

        /// <summary>
        /// 初始化(清空)成员名单并删除所有订阅
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void InitNameList(string strGrpID, GroupPermission mbrAuth)
        {
            IMessageBase[] chain;
            if (mbrAuth == GroupPermission.Owner || mbrAuth == GroupPermission.Administrator)
            {
                if (NameListDAL.NameListInit(strGrpID))
                {
                    MsgMessage += "已初始化名单。";
                }
                else
                {
                    MsgMessage += "与数据库失去连接，初始化名单失败。\r\n";
                }
            }
            else
            {
                Console.WriteLine("执行初始化名单指令失败，由权限不足的人发起");
                MsgMessage += "拒绝：仅有管理员或群主可执行初始化名单指令。\r\n";
            }
            chain = new IMessageBase[] { new PlainMessage(MsgMessage) };
            ApiProperties.session.SendGroupMessageAsync(long.Parse(strGrpID), chain).Wait();
        }
    }
}
