using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace Marchen.DAL
{
    class ClanInfoDAL
    {
        /// <summary>
        /// 获取每日更新时间
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="intTimeOffsetHour">返回的每日更新时间：小时</param>
        /// <returns></returns>
        public static bool GetClanTimeOffset(string strGrpID, out int intTimeOffsetHour)
        {
            string sqlGetHourSet = "select OFFSET_HOUR from " +
                "(select * from SET_TIMEOFFSET) a " +
                "left join " +
                "(select * from TTL_ORGLIST) b " +
                "on " +
                "a.REGION_CODE = b.ORG_REGION " +
                "where b.ORG_ID='" + strGrpID + "'";
            try
            {
                DataTable dt = DBHelper.GetDataTable(sqlGetHourSet);
                intTimeOffsetHour = int.Parse(dt.Rows[0]["OFFSET_HOUR"].ToString());
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex1)
            {
                Console.WriteLine(orex1);
                Console.WriteLine("群：" + strGrpID + "获取每日更新时间时失败。");
                intTimeOffsetHour = 4;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="intRegionCode"></param>
        /// <returns></returns>
        public static bool GetClanRegionCode(string strGrpID, out int intRegionCode)
        {
            string sqlGetHourSet = "select ORG_REGION from TTL_ORGLIST where ORG_ID='" + strGrpID + "'";
            try
            {
                DataTable dt = DBHelper.GetDataTable(sqlGetHourSet);
                intRegionCode = int.Parse(dt.Rows[0]["ORG_REGION"].ToString());
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex1)
            {
                Console.WriteLine(orex1);
                Console.WriteLine("群：" + strGrpID + "获取每日更新时间时失败。");
                intRegionCode = 0;
                return false;
            }
        }
    }
}
