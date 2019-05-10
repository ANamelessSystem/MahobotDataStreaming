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
            if (strRawcontext.ToLower().Trim().Split(' ')[0] == "#zt")
            {
                Console.WriteLine("启动天命2：孤独与影'零点时刻'节点反馈功能");
                string strCmdContext = strRawcontext.ToLower().Replace(d2StartTag, "").Trim();
                string strUserID = receivedMessage.UserId.ToString();
                try
                {
                    string[] sArray = strCmdContext.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (sArray.GetLength(0) == 1)//对应只输入了一组参数的情况
                    {
                        string strCR1 = strCmdContext.Trim().Split(' ')[0];
                        if (strCR1.Contains("-"))
                        {
                            DataTable dtCRResult1 = DBHelper.GetDataTable("select ID,NODENAME,NODECOLOR,NODENO,CS1,CS2,CS3 from SP_D2ZEROTIME where CS1='" + strCR1 + "' or CS2='" + strCR1 + "' or CS3='" + strCR1 + "'");
                            if (dtCRResult1.Rows.Count == 0 || dtCRResult1 is null)
                            {
                                Console.WriteLine("未找到任何结果");
                                D2Message += new Message("未找到任何结果\r\n");
                                D2Message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                                return;
                            }
                            string strColor = null;
                            int intLocation = 0;

                            //检查终端1是否为唯一解
                            DataRow[] drIsCS1 = dtCRResult1.Select("CS1='" + strCR1 + "'");
                            if (drIsCS1.Length == 1)
                            {
                                //唯一结果，显示图片
                                strColor = drIsCS1[0]["NODECOLOR"].ToString();
                                intLocation = int.Parse(drIsCS1[0]["NODENO"].ToString());
                                string strOutput1 = "屏1，屏2，屏3，位置：\r\n————————————\r\n" + drIsCS1[0]["CS1"].ToString() + "，" + drIsCS1[0]["CS2"].ToString() + "，" + drIsCS1[0]["CS3"].ToString() + "，" + drIsCS1[0]["NODECOLOR"].ToString() + drIsCS1[0]["NODENO"].ToString() + "；";
                                var imgColor = Message.LocalImage(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                var imgLocation = Message.LocalImage(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                Console.WriteLine(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                Console.WriteLine(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                D2Message += new Message("\r\n所输入的参数于【1号终端】内找到唯一结果：\r\n" + strOutput1 + "\r\n");
                                Console.WriteLine(strOutput1);
                                D2Message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), imgColor + imgLocation + D2Message).Wait();
                                return;
                            }
                            else
                            {
                                //如果终端1并非唯一解，检索整体结果是否已经是唯一解
                                if (dtCRResult1.Rows.Count == 1)
                                {
                                    //在除了终端1以外的终端发现了唯一解，显示指引图片并用文字注明这并非根据终端1检索
                                    strColor = dtCRResult1.Rows[0]["NODECOLOR"].ToString();
                                    intLocation = int.Parse(dtCRResult1.Rows[0]["NODENO"].ToString());
                                    string strOutput2 = "屏1，屏2，屏3，位置：\r\n————————————\r\n" + dtCRResult1.Rows[0]["CS1"].ToString() + "，" + dtCRResult1.Rows[0]["CS2"].ToString() + "，" + dtCRResult1.Rows[0]["CS3"].ToString() + "，" + dtCRResult1.Rows[0]["NODECOLOR"].ToString() + dtCRResult1.Rows[0]["NODENO"].ToString() + "；";
                                    var imgColor = Message.LocalImage(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                    var imgLocation = Message.LocalImage(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                    Console.WriteLine(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                    Console.WriteLine(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                    D2Message += new Message("\r\n所输入的参数于【2号终端或3号终端】内找到唯一结果：\r\n" + strOutput2 + "\r\n");
                                    Console.WriteLine(strOutput2);
                                    D2Message += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), imgColor + imgLocation + D2Message).Wait();
                                    return;
                                }
                                if (dtCRResult1.Rows.Count > 1)
                                {
                                    //总之返回的结果不止一个
                                    D2Message += new Message("\r\n返回多组结果，请确认：\r\n屏1，屏2，屏3，位置：\r\n————————————");
                                    for (int i = 0; i < dtCRResult1.Rows.Count; i++)
                                    {
                                        string strOutputM = "\r\n" + drIsCS1[i]["CS1"].ToString() + "，" + drIsCS1[i]["CS2"].ToString() + "，" + drIsCS1[i]["CS3"].ToString() + "，" + drIsCS1[i]["NODECOLOR"].ToString() + drIsCS1[i]["NODENO"].ToString() + "；";
                                        D2Message += new Message(strOutputM);
                                        Console.WriteLine(strOutputM);
                                    }
                                    var imgCnR = Message.LocalImage(@"C:\D2ZT\ZTCR.jpg");
                                    D2Message += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID),imgCnR + D2Message).Wait();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            D2Message += new Message("未识别所输入参数的格式\r\n");
                            D2Message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID),D2Message).Wait();
                            return;
                        }
                    }
                    else if (sArray.GetLength(0) > 1)
                    {
                        try
                        {
                            string strCR1 = strCmdContext.Trim().Split(' ')[0];
                            string strCR2 = strCmdContext.Trim().Split(' ')[1];
                            if (strCR1.Contains("-") && strCR2.Contains("-"))
                            {
                                DataTable dtCRResult3 = DBHelper.GetDataTable("select ID,NODENAME,NODECOLOR,NODENO,CS1,CS2,CS3 from (select * from SP_D2ZEROTIME where CS1='" + strCR1 + "' or CS2='" + strCR1 + "' or CS3='" + strCR1 + "') where CS1='" + strCR2 + "' or CS2='" + strCR2 + "' or CS3='" + strCR2 + "'");
                                string strColor = null;
                                int intLocation = 0;
                                if (dtCRResult3.Rows.Count == 1)
                                {
                                    //唯一结果，显示图片
                                    strColor = dtCRResult3.Rows[0]["NODECOLOR"].ToString();
                                    intLocation = int.Parse(dtCRResult3.Rows[0]["NODENO"].ToString());
                                    string strOutput1 = "屏1，屏2，屏3，位置：\r\n————————————\r\n" + dtCRResult3.Rows[0]["CS1"].ToString() + "，" + dtCRResult3.Rows[0]["CS2"].ToString() + "，" + dtCRResult3.Rows[0]["CS3"].ToString() + "，" + dtCRResult3.Rows[0]["NODECOLOR"].ToString() + dtCRResult3.Rows[0]["NODENO"].ToString() + "；";
                                    var imgColor = Message.LocalImage(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                    var imgLocation = Message.LocalImage(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                    Console.WriteLine(@"C:\D2ZT\ZTR_" + strColor + ".png");
                                    Console.WriteLine(@"C:\D2ZT\ZTC_" + intLocation.ToString() + ".png");
                                    D2Message += new Message("\r\n" + strOutput1 + "\r\n");
                                    Console.WriteLine(strOutput1);
                                    D2Message += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), imgColor + imgLocation + D2Message).Wait();
                                    return;
                                }
                                else if (dtCRResult3.Rows.Count == 0 || dtCRResult3 is null)
                                {
                                    Console.WriteLine("无结果");
                                    D2Message += new Message("无结果\r\n");
                                    D2Message += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                                    return;
                                }
                                else
                                {
                                    D2Message += new Message("\r\n返回多组结果，请确认：\r\n屏1，屏2，屏3，位置：\r\n————————————");
                                    for (int i = 0; i < dtCRResult3.Rows.Count; i++)
                                    {
                                        string strOutputM = "\r\n" + dtCRResult3.Rows[i]["CS1"].ToString() + "，" + dtCRResult3.Rows[i]["CS2"].ToString() + "，" + dtCRResult3.Rows[i]["CS3"].ToString() + "，" + dtCRResult3.Rows[i]["NODECOLOR"].ToString() + dtCRResult3.Rows[i]["NODENO"].ToString() + "；";
                                        D2Message += new Message(strOutputM + "\r\n");
                                        Console.WriteLine(strOutputM);
                                    }
                                    var imgCnR = Message.LocalImage(@"C:\D2ZT\ZTCR.jpg");
                                    D2Message += Message.At(long.Parse(strUserID));
                                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID),imgCnR + D2Message).Wait();
                                    return;
                                }
                            }
                            else
                            {
                                D2Message += new Message("参数不正确\r\n");
                                Console.WriteLine("参数不正确");
                                D2Message += Message.At(long.Parse(strUserID));
                                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                                return;
                            }
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine(ex2);
                            D2Message += new Message("缺少参数\r\n");
                            Console.WriteLine("缺少参数");
                            D2Message += Message.At(long.Parse(strUserID));
                            ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                            return;
                        }
                    }
                    else
                    {
                        D2Message += new Message("缺少参数\r\n");
                        Console.WriteLine("缺少参数");
                        D2Message += Message.At(long.Parse(strUserID));
                        ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    D2Message += new Message("无法解析\r\n");
                    Console.WriteLine("无法解析");
                    D2Message += Message.At(long.Parse(strUserID));
                    ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strGrpID), D2Message).Wait();
                    return;
                }
            }
        }
    }
}
