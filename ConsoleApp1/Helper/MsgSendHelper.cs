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
    enum MsgSendType { Raw, Picture, Auto };
    enum MsgTargetType { Private, Group };
    class MsgSendHelper
    {
        /// <summary>
        /// 全局发送控制
        /// </summary>
        /// <param name="msgSendType"></param>
        /// <param name="msgTargetType"></param>
        /// <param name="strTargetID"></param>
        /// <param name="msgMessage"></param>
        public static void UniversalMsgSender(MsgSendType msgSendType, MsgTargetType msgTargetType, string strTargetID, Message msgMessage)
        {
            string strRawMessage = msgMessage.Raw.ToString();
            if (strRawMessage == "" || msgMessage is null)
            {
                return;
            }
            Message _outMessage = new Message("");
            //Judge by the number of content lines
            if (msgSendType is MsgSendType.Auto)
            {
                //this number of lines is also the maxium height for ranging the bitmap
                int _ContentHeight = Regex.Matches(strRawMessage, "\r\n").Count;
                //when it's too long, convert it to a picture
                if (_ContentHeight > 8)
                {
                    //_ContentHeight += 1;
                    ConvertText2Pic(strRawMessage, _ContentHeight, out _outMessage);
                }
                //or just pass through
                else
                {
                    _outMessage = msgMessage;
                }

            }
            //Convert to Image
            else if (msgSendType is MsgSendType.Picture)
            {
                ConvertText2Pic(strRawMessage, 0, out _outMessage);
            }
            else
            {
                _outMessage = msgMessage;
            }
            //Target type,0:private,1:group
            if (msgTargetType is MsgTargetType.Private)
            {
                ApiProperties.HttpApi.SendPrivateMessageAsync(long.Parse(strTargetID), _outMessage).Wait();
            }
            else
            {
                ApiProperties.HttpApi.SendGroupMessageAsync(long.Parse(strTargetID), _outMessage).Wait();
            }
        }

        /// <summary>
        /// 将指定文本转换为图片
        /// </summary>
        /// <param name="strContent">指定文本</param>
        /// <param name="msgMessage">理应只包含有一张图片的message</param>
        private static void ConvertText2Pic(string strContent,int _ContentHeight, out Message _message)
        {
            _message = new Message("");
            //Console.WriteLine("图片转换失败，内容为空");
            if (strContent == null || strContent.Trim() == string.Empty)
            { 
                return; 
            }
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
            if (_ContentHeight < 1)
            {
                _ContentHeight = Regex.Matches(strContent, "\r\n").Count;
            }
            //create bitmap base on the length and height
            Bitmap image = new Bitmap((int)Math.Ceiling((_ContentLength * 15.0)), (_ContentHeight * 25));
            Graphics g = Graphics.FromImage(image);
            byte[] _byteArray;
            try
            {
                g.Clear(Color.White);
                Font font = new Font("Microsoft YaHei", 12.5f, (FontStyle.Regular));
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                g.DrawString(strContent, font, drawBrush, 2, 2);
                using (MemoryStream _memStream = new MemoryStream())
                {
                    image.Save(_memStream, ImageFormat.Png);
                    _byteArray = new byte[_memStream.Length];
                    _memStream.Seek(0, SeekOrigin.Begin);
                    _memStream.Read(_byteArray, 0, (int)_memStream.Length);
                }
                _message += Message.ByteArrayImage(_byteArray);
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }
    }
}
