using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using Sisters.WudiLib.Responses;
using System.IO;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace Marchen.Helper
{
    class MsgSendHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iSendType">发送形式：0.文本；1.转换成图片</param>
        /// <param name="iTargetType">目标类型：0.个人；1.群</param>
        /// <param name="strTargetID">目标ID，不管个人还是群都是用的同一个标签</param>
        /// <param name="strContent">内容，文本</param>
        public static void  UniversalMsgSender(int iSendType,int iTargetType,string strTargetID,string strContent)
        {
            Message MsgMessage = new Message("");
            //Text
            if (iSendType == 0)
            {
                MsgMessage += new Message(strContent);
            }
            //Convert to Image
            else
            {
                if (strContent == null || strContent.Trim() == String.Empty)
                    return;
                //get the maxium length to range the bitmap
                int _ContentLength = 0;
                string[] strArr = strContent.Split("\r\n");
                for (int i = 0; i < strArr.Length; i++)
                {
                    if (strArr[i].Length > _ContentLength)
                    {
                        _ContentLength = strArr[i].Length;
                    }
                }
                //get the maxium height to range the bitmap
                int _ContentHeight = Regex.Matches(strContent, "\r\n").Count + 1;
                //create bitmap base on the length and height
                Bitmap image = new Bitmap((int)Math.Ceiling((_ContentLength * 18.0)), (_ContentHeight * 28));
                Graphics g = Graphics.FromImage(image);
                byte[] _byteArray;
                try
                {
                    g.Clear(Color.White);
                    Font font = new Font("Microsoft YaHei", 15.5f, (FontStyle.Bold));
                    //System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Black, Color.DarkRed, 1.2f, true);
                    SolidBrush drawBrush = new SolidBrush(Color.Black);
                    g.DrawString(strContent, font, drawBrush, 2, 2);
                    using (MemoryStream _memStream = new MemoryStream())
                    {
                        image.Save(_memStream, ImageFormat.Png);
                        _byteArray = new byte[_memStream.Length];
                        _memStream.Seek(0, SeekOrigin.Begin);
                        _memStream.Read(_byteArray, 0, (int)_memStream.Length);
                    }
                    MsgMessage += Message.ByteArrayImage(_byteArray);
                }
                finally
                {
                    g.Dispose();
                    image.Dispose();
                }
            }
            if (iTargetType == 0)
            {
                ApiProperties.HttpApi.SendPrivateMessageAsync(long.Parse(strTargetID), MsgMessage).Wait();
            }
            else
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strTargetID), MsgMessage).Wait();
            }
        }
    }
}
