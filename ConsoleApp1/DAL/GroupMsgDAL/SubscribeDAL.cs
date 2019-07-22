using System;
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
        public static bool AddBossSubs(string strGrpID, string strUserID, int intBossCode, int intSubsType, int intRound = 0)
        {
            string sqlAddSubs = "insert into TTL_BOSSSUBS(userid,grpid,bc,round,substype,finishflag) values('" + strUserID + "','" + strGrpID + "'," + intBossCode + "," + intRound + "," + intSubsType + ",0)";
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
        /// 获取订阅状态的方法（个人）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="dtSubsStatus">返回dt格式的订阅状态</param>
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
        /// 获取订阅状态的方法（全群）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtSubsStatus">返回dt格式的订阅状态</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GetSubsStatus(string strGrpID, out DataTable dtSubsStatus)
        {
            string sqlQrySubs = "select * from " +
                "(select * from TTL_BOSSSUBS where GRPID='" + strGrpID + "') a " +
                "left join " +
                "(select MBRID,MBRNAME from TTL_MBRLIST where GRPID='" + strGrpID + "') b " +
                "on a.USERID = b.MBRID";
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
            string sqlDelSubs = "delete from TTL_BOSSSUBS where GRPID='" + strGrpID + "' and USERID='" + strUserID + "' and BC=" + intBossCode;
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
                sqlQrySubs = "select USERID,SUBSTYPE from TTL_BOSSSUBS where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and ROUND < " + (intRound+1) + " and FINISHFLAG != 2";
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
                sqlQrySubs = "select USERID from TTL_BOSSSUBS where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and ROUND < " + intRound + " and FINISHFLAG != 1";
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


        /// <summary>
        /// 更新已提醒过的标识位
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intRound">已提醒周目</param>
        /// <param name="intBossCode">已提醒BOSS</param>
        /// <param name="intProgType">已提醒进度</param>
        /// <returns></returns>
        public static bool UpdateRemindFlag(string strGrpID,string strUserID, int intRound, int intBossCode, int intProgType)
        {

            string sqlUpdateSubs = "";
            if (intProgType == 0)
            {
                sqlUpdateSubs = "update TTL_BOSSSUBS set FINISHFLAG = 2，ROUND = " + intRound + " where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and USERID = '" + strUserID + "'";
            }
            else if (intProgType == 1)
            {
                sqlUpdateSubs = "update TTL_BOSSSUBS set FINISHFLAG = 2，ROUND = " + intRound + " where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and USERID = '" + strUserID + "'";
            }
            else if (intProgType == 2)
            {
                intBossCode += 1;
                if (intBossCode > 5)
                {
                    intBossCode = intBossCode - 5;
                    intRound += 1;
                }
                sqlUpdateSubs = "update TTL_BOSSSUBS set FINISHFLAG = 1，ROUND = " + intRound + " where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and USERID = '" + strUserID + "'";
            }
            else
            {
                throw new Exception("收到非设计范围内的BOSS血量状态代码");
            }
            try
            {
                DBHelper.ExecCmdNoCount(sqlUpdateSubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("更新预约表已提醒状态时出现错误，SQL：" + sqlUpdateSubs + "\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 修改订阅状态（仅限补刀相关功能使用）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intRound">周目</param>
        /// <param name="intBossCode">BOSS代码</param>
        /// <param name="intSubsType">订阅类型：0普通订阅，1补时刀注册</param>
        /// <param name="intFinishFlag"></param>
        /// <returns></returns>
        public static bool UpdateSubsType(string strGrpID, string strUserID, int intRound, int intBossCode, int intSubsType, int intFinishFlag = 0)
        {
            string sqlUpdateSubs = "";
            sqlUpdateSubs = "update TTL_BOSSSUBS set FINISHFLAG = " + intFinishFlag + "，ROUND = " + intRound + ", SUBSTYPE = " + intSubsType + ", BC = " + intBossCode + " where GRPID = '" + strGrpID + "' and BC = " + intBossCode + " and USERID = '" + strUserID + "'";
            try
            {
                DBHelper.ExecCmdNoCount(sqlUpdateSubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("更新预约表预约类型时出现错误，SQL：" + sqlUpdateSubs + "\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 更改补时预定
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intNewBC">欲更改为的BOSS</param>
        /// <returns></returns>
        public static bool UpdateChangeExtSubs(string strGrpID, string strUserID, int intNewBC)
        {
            string sqlUpdateSubs = "";
            sqlUpdateSubs = "update TTL_BOSSSUBS set FINISHFLAG = 0，ROUND = 0, BC = " + intNewBC + " where GRPID = '" + strGrpID + "' and SUBSTYPE = 1 and USERID = '" + strUserID + "'";
            try
            {
                DBHelper.ExecCmdNoCount(sqlUpdateSubs);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("更新预约表预约类型时出现错误，SQL：" + sqlUpdateSubs + "\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 删除补时刀订阅的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="intBossCode"></param>
        /// <param name="intDelCount"></param>
        /// <returns></returns>
        public static bool DelExtSubs(string strGrpID, string strUserID, out int intDelCount)
        {
            string sqlDelSubs = "delete from TTL_BOSSSUBS where GRPID='" + strGrpID + "' and USERID='" + strUserID + "' and SUBSTYPE = 1";
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
    }
}
