using System;
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
            if (NameListDAL.UpdateNameList(strGrpID, strUserID, strUserGrpCard, out int intMemberCount))
            {
                MsgMessage += new Message("已成功更新成员名单信息(" + intMemberCount.ToString() + "/30)。\r\n");
            }
            else if (intMemberCount == 30 || intMemberCount > 30)
            {
                MsgMessage += new Message("名单已满30人，无法新增成员，请先清除无效成员。\r\n");
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
            if (NameListDAL.QryNameList(strGrpID, out DataTable dtNameList))
            {
                if (dtNameList.Rows.Count > 0)
                {
                    MsgMessage += new Message("目前名单("+ dtNameList.Rows.Count + "/30)：\r\n");
                    for (int i = 0; i < dtNameList.Rows.Count; i++)
                    {
                        string strOutput = dtNameList.Rows[i]["MBRNAME"].ToString() + "(" + dtNameList.Rows[i]["MBRID"].ToString() + ")";
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
            if (InputVariables.DouUID == -1)
            {
                MsgMessage += new Message("未识别出需要删除的QQ号。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            if (strUserID == InputVariables.DouUID.ToString() || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (NameListDAL.QryNameList(strGrpID, out DataTable dtNameList))
                {
                    DataRow[] drExistsID = dtNameList.Select("MBRID='" + InputVariables.DouUID.ToString() + "'");
                    if (drExistsID.Length == 1)
                    {
                        if (NameListDAL.NameListDelete(strGrpID, InputVariables.DouUID.ToString()))
                        {
                            Console.WriteLine("已将群：" + strGrpID + "，" + InputVariables.DouUID.ToString() + "移除名单。");
                            MsgMessage += new Message("已将" + InputVariables.DouUID.ToString() + "移出名单。");
                            //该功能已由触发器行级触发器完成
                            //if (SubscribeDAL.DelSubsAll(strGrpID, InputVariables.DouUID.ToString(), out int intDelCounts))
                            //{
                            //    Console.WriteLine("移除名单后清除订阅表成功。");
                            //}
                        }
                        else
                        {
                            MsgMessage += new Message("与数据库失去连接，删除名单失败。\r\n");
                        }
                    }
                    else
                    {
                        MsgMessage += new Message("未找到对应人员的名单记录，无法删除。\r\n");
                    }
                }
            }
            else
            {
                Console.WriteLine("只有本人或管理员以上可删除对应名单。修改者：" + strUserID + " 原记录：" + InputVariables.DouUID.ToString());
                MsgMessage += new Message("只有本人或管理员以上可删除对应的名单。\r\n");
                MsgMessage += Message.At(long.Parse(strUserID));
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
                return;
            }
            MsgMessage += Message.At(long.Parse(strUserID));
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }

        /// <summary>
        /// 初始化(清空)成员名单并删除所有订阅
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        public static void InitNameList(string strGrpID, GroupMemberInfo memberInfo)
        {
            if (memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Leader || memberInfo.Authority == GroupMemberInfo.GroupMemberAuthority.Manager)
            {
                if (NameListDAL.NameListInit(strGrpID))
                {
                    MsgMessage += new Message("已初始化名单。");
                }
                else
                {
                    MsgMessage += new Message("与数据库失去连接，初始化名单失败。\r\n");
                }
            }
            else
            {
                Console.WriteLine("执行初始化名单指令失败，由权限不足的人发起");
                MsgMessage += new Message("拒绝：仅有管理员或群主可执行初始化名单指令。\r\n");
            }
            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), MsgMessage).Wait();
        }
    }
}
