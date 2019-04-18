using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Marchen.DAL
{
    class SubscribeDAL
    {
        /// <summary>
        /// 订阅BOSS的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intBossCode"></param>
        /// <returns></returns>
        public static bool AddBossSubs(string strGrpID, string strUserID,int intBossCode)
        {
            string sqlAddSubs = "insert into TTL_BOSSSUBS(userid,grpid,bc) values('" + strUserID + "','" + strGrpID + "'," + intBossCode + ")";
            try
            {
                DBHelper.ExecCmdNoCount(sqlAddSubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("添加BOSS订阅状态时发生错误，SQL：" + sqlAddSubs + "。\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 获取订阅状态的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intBossCode">BOSS代码，为0时查阅全部订阅</param>
        /// <param name="dtSubsStatus"></param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GetSubsStatus(string strGrpID, string strUserID, out DataTable dtSubsStatus)
        {
            string sqlQrySubs = "select * from TTL_BOSSSUBS where GRPID='" + strGrpID + "' and USERID='" + strUserID + "'";
            try
            {
                dtSubsStatus = DBHelper.GetDataTable(sqlQrySubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询BOSS订阅状态时发生错误，SQL：" + sqlQrySubs + "。\r\n" + oex);
                dtSubsStatus = null;
                return false;
            }
        }
    }
}
