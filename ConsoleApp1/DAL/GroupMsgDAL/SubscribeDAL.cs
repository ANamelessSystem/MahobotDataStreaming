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
            string sqlAddSubs = "insert into TTL_BOSSSUBS(userid,grpid,bc,round,progress) values('" + strUserID + "','" + strGrpID + "'," + intBossCode + ",0,0)";
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

        /// <summary>
        /// 删除BOSS订阅的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intBossCode"></param>
        /// <param name="intDelCount"></param>
        /// <returns></returns>
        public static bool DelBossSubs(string strGrpID, string strUserID, int intBossCode,out int intDelCount)
        {
            string sqlDelSubs = "delete from TTL_BOSSSUBS where GRPID='" + strGrpID + "' and USERID='" + strUserID + "' and BC=" + intBossCode + ")";
            try
            {
                intDelCount = DBHelper.ExecuteCommand(sqlDelSubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("删除BOSS订阅状态时发生错误，SQL：" + sqlDelSubs + "。\r\n" + oex);
                intDelCount = 0;
                return false;
            }
        }

        /// <summary>
        /// 查询已订阅该状态BOSS的成员
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="intRound"></param>
        /// <param name="intBossCode"></param>
        /// <param name="intProgType">进度，0:大于300w血，1:大于150w血，2:小于150w血</param>
        public static bool BossReminder(string strGrpID, int intRound, int intBossCode, int intProgType, out DataTable dtSubsMembers)
        {
            string sqlQrySubs = "";
            if (intProgType == 0)
            {
                //已到达提醒
                sqlQrySubs = "select USERID from TTL_BOSSSUBS where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and ROUND < " + intRound + " and SUBSTYPE = 0 and FINISHFLAG != 1";
            }
            if (intProgType == 1)
            {
                //尾刀撞刀专用提醒
                sqlQrySubs = "select USERID from TTL_BOSSSUBS where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and ROUND < " + intRound + " and SUBSTYPE = 1 and FINISHFLAG != 1";
            }
            if (intProgType == 2)
            {
                //下一个BOSS的预提醒
                intBossCode += 1;
                if (intBossCode > 5)
                {
                    intBossCode = intBossCode - 5;
                    intRound += 1;
                }
                sqlQrySubs = "select USERID from TTL_BOSSSUBS where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and ROUND < " + intRound + " and SUBSTYPE = 0 and FINISHFLAG != 0";
            }
            try
            {
                dtSubsMembers = DBHelper.GetDataTable(sqlQrySubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询预约表时出现错误，SQL：" + sqlQrySubs + "\r\n" + oex);
                dtSubsMembers = null;
                return false;
            }
        }
    }
}
