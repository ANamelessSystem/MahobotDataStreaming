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
            if (QueueDAL.UpdateNameList(strGrpID, strUserID, strUserGrpCard))
            {
                MsgMessage += new Message("已成功更新成员名单信息");
            }
            else
            {
                MsgMessage += new Message("数据库错误，更新成员名单失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 展示成员名单
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void NameListShow(string strGrpID, string strUserID)
        {
            if (QueueDAL.ShowNameList(strGrpID, out DataTable dtNameList))
            {
                if (dtNameList.Rows.Count > 0)
                {
                    MsgMessage += new Message("目前名单("+ dtNameList.Rows.Count + "/30)：\r\n");
                    for (int i = 0; i < dtNameList.Rows.Count; i++)
                    {
                        string strOutput = dtNameList.Rows[i]["name"].ToString() + "(" + dtNameList.Rows[i]["id"].ToString() + ")";
                        MsgMessage += new Message(strOutput + "\r\n");
                        Console.WriteLine(strOutput);
                    }
                }
                else
                {
                    Console.WriteLine("名单中无人");
                    MsgMessage += new Message("目前名单中无人。\r\n");
                }
            }
            else
            {
                MsgMessage += new Message("与数据库失去连接，查询名单失败。\r\n");
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 删除成员名单
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void NameListDelete(string strGrpID, string strUserID, string strCmdContext, GroupMemberInfo memberInfo)
        {
            if (!CmdHelper.CmdSpliter(strCmdContext))
            {
                MsgMessage += new Message("输入【@MahoBot help】获取帮助。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (CommonVariables.DouUID == -1)
            {
                MsgMessage += new Message("未识别出需要删除的QQ号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (strUserID == CommonVariables.DouUID.ToString() || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (QueueDAL.NameListDelete(strGrpID, CommonVariables.DouUID.ToString(), out int deletedCount))
                {
                    if (deletedCount > 0)
                    {
                        Console.WriteLine("已将群：" + strGrpID + "，" + CommonVariables.DouUID.ToString() + "移除名单。");
                        MsgMessage += new Message("已将"+ CommonVariables.DouUID.ToString() + "移出名单。");
                    }
                    else
                    {
                        Console.WriteLine("群：" + strGrpID + "，" + CommonVariables.DouUID.ToString() + "移出名单失败：未找到记录。");
                        MsgMessage += new Message("未找到对应人员的名单记录，无法删除。");
                    }
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，删除名单失败。\r\n");
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可删除对应名单。修改者：" + strUserID + " 原记录：" + CommonVariables.DouUID.ToString());
                MsgMessage += new Message("只有本人或管理员以上可删除对应的名单。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }
    }
}
