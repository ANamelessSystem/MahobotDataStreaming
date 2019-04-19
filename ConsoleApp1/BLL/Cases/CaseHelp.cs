using System;
using System.Data;
using Marchen.DAL;
using Marchen.Model;
using Message = Sisters.WudiLib.SendingMessage;
using Sisters.WudiLib.Responses;

namespace Marchen.BLL
{
    class CaseHelp : GroupMsgBLL
    {
        public static void test()
        {
            ApiProperties.HttpApi.SendPrivateMessageAsync(1402453924,"testmessage");
        }
    }
}
