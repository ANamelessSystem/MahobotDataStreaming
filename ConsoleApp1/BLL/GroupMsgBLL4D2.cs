﻿using System;
using System.Data;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using MessageContext = Sisters.WudiLib.Posts.Message;
using System.Text.RegularExpressions;
using Marchen.DAL;
using Sisters.WudiLib.Responses;

namespace Marchen.BLL
{
    class GroupMsgBLL4D2
    {
        protected static Message D2Message;
        public static void D2MsgHandler(MessageContext receivedMessage, GroupMemberInfo memberInfo)
        {
            D2Message = new Message("");
            string strRawcontext = receivedMessage.RawMessage.ToString().Trim();
            string d2StartTag = "#zt";
            string strGrpID = receivedMessage.GetType().GetProperty("GroupId").GetValue(receivedMessage, null).ToString();
            var message = new Message("");
            Console.WriteLine("启动天命2：孤独与影'零点时刻'节点反馈功能");
            //分离命令头和命令体，命令头：功能识别区，命令体：数据包含区。
            //string strCmdHead = strRawcontext.Replace(d2StartTag, "").Trim().Split(' ')[0];
            //string strCmdContext = strRawcontext.Replace(d2StartTag, "").Replace(strCmdHead, "").Trim();
            string strCmdContext = strRawcontext.Replace(d2StartTag, "").Trim();
            string strUserID = receivedMessage.UserId.ToString();
            string strUserGrpCard = memberInfo.InGroupName.ToString().Trim();
            string strUserNickName = memberInfo.Nickname.ToString().Trim();
            if (strUserGrpCard == null || strUserGrpCard == "")
            {
                strUserGrpCard = strUserNickName;
            }
            string strCR1 = strCmdContext.Trim().Split(' ')[0];
            string strCR2 = strCmdContext.Trim().Split(' ')[1];
            if (strCR1.Contains("-") && strCR1.Contains("-"))
            {
                DataTable dtCRResult = DBHelper.GetDataTable("select id,nodename,nodecolor,nodeno,cs1,cs2,cs3 from (select * from SP_D2ZEROTIME where CS1='" + strCR1 + "' or CS2='" + strCR1 + "' or CS3='" + strCR1 + "') where CS1='" + strCR2 + "' or CS2='" + strCR2 + "' or CS3='" + strCR2 + "'");
                string strColor = null;
                int intLocation = 0;
                if (dtCRResult.Rows.Count == 1)
                {
                    //唯一结果，显示图片
                    strColor = dtCRResult.Rows[0]["nodecolor"].ToString();
                    intLocation = int.Parse(dtCRResult.Rows[0]["nodeno"].ToString());
                }
                else if (dtCRResult.Rows.Count == 0 || dtCRResult is null)
                {
                    //无结果
                }
                for (int i = 0; i < dtCRResult.Rows.Count; i++)
                {
                    D2Message += new Message("返回多组结果，请确认\r\n");
                    string strOutput = "显示屏1：" + dtCRResult.Rows[i]["CS1"].ToString() + "，显示屏2：" + dtCRResult.Rows[i]["CS2"].ToString() + "，显示屏3：" + dtCRResult.Rows[i]["CS2"].ToString() + "；颜色：" + dtCRResult.Rows[i]["nodecolor"].ToString() + "，位置：" + dtCRResult.Rows[i]["nodeno"].ToString();
                    D2Message += new Message(strOutput + "\r\n");
                    Console.WriteLine(strOutput);
                }
            }
            
        }
    }
}
