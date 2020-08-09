using Marchen.BLL;
using Mirai_CSharp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Marchen.Model;

namespace Mirai_CSharp.Example
{
    public partial class ExamplePlugin
    {
        public async Task<bool> GroupMessage(MiraiHttpSession session, IGroupMessageEventArgs e)
        {
            string strAtPart = "";
            string strCmdPart = "";
            for (int i = 0; i < e.Chain.Length; i++)
            {
                if (e.Chain[i].Type == "At")
                {
                    //Console.WriteLine("At pattern is " + e.Chain[i].ToString());
                    strAtPart += " [CQ:at,qq=" + e.Chain[i].GetType().GetProperty("Target").GetValue(e.Chain[i], null).ToString() + "] ";
                }
                if (e.Chain[i].Type == "Source")
                {
                    //Console.WriteLine("Source pattern is " + e.Chain[i].ToString());
                }
                if (e.Chain[i].Type == "Plain")
                {
                    //Console.WriteLine("Plain pattern is " + e.Chain[i].ToString());
                    strCmdPart += " " + e.Chain[i].ToString() + " ";
                }
            }
            GroupPermission grpMbrAuthrity = e.Sender.Permission;
            string strUsrGrpCard = e.Sender.Name;
            string strGrpID = e.Sender.Group.Id.ToString();
            string strUsrID = e.Sender.Id.ToString();
            string strMsgInject = strCmdPart + strAtPart;
            await GroupMsgBLL.GrpMsgReco(grpMbrAuthrity, strUsrGrpCard, strGrpID, strUsrID, strMsgInject);

            // 临时消息和群消息一致, 不多写例子了
            //IMessageBase[] chain = new IMessageBase[]
            //{
            //    new PlainMessage($"收到了来自{e.Sender.Name}[{e.Sender.Id}]{{{e.Sender.Permission}}}的群消息:{string.Join(null, (IEnumerable<IMessageBase>)e.Chain)}")
            //    //                          / 发送者群名片 /  / 发送者QQ号 /   /   发送者在群内权限   /                                                       / 消息链 /
            //};
            //await session.SendGroupMessageAsync(e.Sender.Group.Id, chain); // 向消息来源群异步发送由以上chain表示的消息
            return false; // 不阻断消息传递。如需阻断请返回true
        }
    }
}
