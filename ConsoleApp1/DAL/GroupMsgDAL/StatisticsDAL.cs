using System;
using System.Data;

namespace Marchen.DAL
{
    class StatisticsDAL
    {
        /// <summary>
        /// 获得BOSS初期HP值的方法
        /// </summary>
        /// <param name="intBossCode">BOSS代码</param>
        /// <param name="intRound">周目数</param>
        /// <param name="dtBossMaxHP">取回的dt格式的boss初期HP值</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GetBossMaxHP(string strGrpID, int intBossCode, int intRound, out DataTable dtBossMaxHP)
        {
            string sqlGetBossHpByRound = "select HP from (select ORG_REGION from TTL_ORGLIST where ORG_ID = '"+ strGrpID + "') a " +
                "left join " +
                "(select * from TTL_HPSET where BC = " + intBossCode + " and ROUNDMIN <= " + intRound + " and ROUNDMAX >= " + intRound + ") b " +
                "on a.ORG_REGION = b.REGIONCODE";
            try
            {
                dtBossMaxHP = DBHelper.GetDataTable(sqlGetBossHpByRound);
                if (dtBossMaxHP.Rows[0][0] is DBNull)
                {
                    Console.WriteLine("获取BOSS的初期HP时取回空值，SQL：" + sqlGetBossHpByRound + "。");
                    return false;
                }
                else if (dtBossMaxHP.Rows[0]["HP"] is DBNull)
                {
                    Console.WriteLine("获取BOSS的初期HP时取回空值，SQL：" + sqlGetBossHpByRound + "。");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("获取BOSS的初期HP时发生错误，SQL：" + sqlGetBossHpByRound + "。\r\n" + orex);
                dtBossMaxHP = null;
                return false;
            }
        }
    }
}