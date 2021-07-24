using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.Model;
using Oracle.ManagedDataAccess.Client;

namespace Marchen.DAL
{
    class RecordDAL
    {
        /// <summary>
        /// 伤害上报的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strRecID">用户ID(qq号)</param>
        /// <param name="intDMG">伤害值</param>
        /// <param name="intRound">周目值</param>
        /// <param name="intBossCode">BOSS代码</param>
        /// <param name="intEID">事件ID</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static void AddDamageRecord(string strGrpID, string strRecID, string strUpldrID, int intDMG, int intBossCode, int intRecType, int intDateAddjust, out int intEID)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_varUsrID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_varUpldrID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_numDamage", OracleDbType.Int32),
                new OracleParameter(":i_numBsCode", OracleDbType.Int16,1),
                new OracleParameter(":i_numRecType", OracleDbType.Int16,1),
                new OracleParameter(":i_numTimeOffset", OracleDbType.Int16,1),
                new OracleParameter(":o_numEvtID", OracleDbType.Int32)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = strRecID;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = strUpldrID;
            param[2].Direction = ParameterDirection.Input;
            param[3].Value = intDMG;
            param[3].Direction = ParameterDirection.Input;
            param[4].Value = intBossCode;
            param[4].Direction = ParameterDirection.Input;
            param[5].Value = intRecType;
            param[5].Direction = ParameterDirection.Input;
            param[6].Value = intDateAddjust;
            param[6].Direction = ParameterDirection.Input;
            param[7].Direction = ParameterDirection.Output;
            try
            {
                DBHelper.ExecuteProdNonQuery("PROC_DMGRECADD_NEW", param);
                intEID = int.Parse(param[7].Value.ToString());
            }
            catch (OracleException orex)
            {
                if (orex.Number == 20111)
                {
                    throw new Exception("因存在更低周目BOSS，无法录入本周目伤害。");
                }
                else if (orex.Number == 20112)
                {
                    throw new Exception("因存在更低阶段BOSS，无法录入本阶段伤害。");
                }
                else
                {
                    throw new Exception("未知数据库错误代码" + orex.Number.ToString() + "，请联系bot管理员。");
                }
            }
        }

        public static void GetDamageRecord(string strGrpID, string strRecID, int intEventID, int intBossCode, int intRound, int intAllFlag, int intQueryMode, out DataTable dtDamageRecord)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_varUsrID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_numEvtID", OracleDbType.Int32),
                new OracleParameter(":i_numBsCode", OracleDbType.Int16,1),
                new OracleParameter(":i_numRound", OracleDbType.Int32),
                new OracleParameter(":i_numIsAll", OracleDbType.Int16,1),
                new OracleParameter(":i_numQryMode", OracleDbType.Int16,1),
                new OracleParameter(":o_result_cur", OracleDbType.RefCursor)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = strRecID;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = intEventID;
            param[2].Direction = ParameterDirection.Input;
            param[3].Value = intBossCode;
            param[3].Direction = ParameterDirection.Input;
            param[4].Value = intRound;
            param[4].Direction = ParameterDirection.Input;
            param[5].Value = intAllFlag;
            param[5].Direction = ParameterDirection.Input;
            param[6].Value = intQueryMode;
            param[6].Direction = ParameterDirection.Input;
            param[7].Direction = ParameterDirection.Output;
            try
            {
                dtDamageRecord = DBHelper.ExecuteProd2DT("PROC_DMGRECQRY_NEW", param);
            }
            catch (OracleException orex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行PROC_DMGRECQRY_NEW时跳出错误：" + orex);
                if (orex.Number == 20103)
                {
                    throw new Exception("未指定查询模式，请联系bot管理员。");
                }
                else
                {
                    throw new Exception("未知数据库错误代码" + orex.Number.ToString() + "，请联系bot管理员。");
                }
            }
        }

        /// <summary>
        /// 根据EventID修改对应数据的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strRecID">QQ号</param>
        /// <param name="intDMG">伤害</param>
        /// <param name="intRound">周目</param>
        /// <param name="intBossCode">BOSS代号</param>
        /// <param name="intExTime">是否补时；1：是，2：否。</param>
        /// <param name="intEID">档案号</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static void ModifyDamageRecord(string strGrpID, string strRecID, int intDMG, int intRound, int intBossCode, int intExTime, int intEID)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_varUsrID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_numDamage", OracleDbType.Int32),
                new OracleParameter(":i_numRound", OracleDbType.Int32),
                new OracleParameter(":i_numBsCode", OracleDbType.Int16,1),
                new OracleParameter(":i_numRecType", OracleDbType.Int16,1),
                new OracleParameter(":i_numTimeOffset", OracleDbType.Int16),
                new OracleParameter(":i_numEvtID", OracleDbType.Int32)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = strRecID;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = intDMG;
            param[2].Direction = ParameterDirection.Input;
            param[3].Value = intRound;
            param[3].Direction = ParameterDirection.Input;
            param[4].Value = intBossCode;
            param[4].Direction = ParameterDirection.Input;
            param[5].Value = intExTime;
            param[5].Direction = ParameterDirection.Input;
            param[6].Value = 0;
            param[6].Direction = ParameterDirection.Input;
            param[7].Value = intEID;
            param[7].Direction = ParameterDirection.Input;
            try
            {
                DBHelper.ExecuteProdNonQuery("PROC_DMGRECMOD_NEW", param);
            }
            catch (OracleException orex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行PROC_DMGRECMOD_NEW时跳出错误：" + orex);
                throw new Exception("未知数据库错误代码" + orex.Number.ToString() + "，请联系bot管理员。");
            }
        }

        /// <summary>
        /// 查询数据库时间的方法
        /// </summary>
        /// <param name="dtTimeNow">返回dt格式时间</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryTimeNowOnDatabase(out DataTable dtResultTime)
        {
            string sqlGetDatabaseTime = "select sysdate from dual";
            try
            {
                dtResultTime = DBHelper.GetDataTable(sqlGetDatabaseTime);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询当前数据库时间时返回错误，SQL：" + sqlGetDatabaseTime + "。\r\n" + oex);
                dtResultTime = null;
                return false;
            }
        }

        /// <summary>
        /// 根据群号查询指定时间范围出刀情况
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="dtResult"></param>
        public static void QueryStrikeStatus(string strGrpID, out DataTable dtResult)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_numTimeOffset", OracleDbType.Int16),
                new OracleParameter(":i_numQueryType", OracleDbType.Int16),
                new OracleParameter(":o_refResult", OracleDbType.RefCursor)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = 0;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = 0;
            param[2].Direction = ParameterDirection.Input;
            param[3].Direction = ParameterDirection.Output;
            try
            {
                dtResult = DBHelper.ExecuteProd2DT("PROC_STRIKESTATUSQUERY", param);
            }
            catch (OracleException orex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行PROC_STRIKESTATUSQUERY时跳出错误：" + orex);
                throw new Exception("未知数据库错误代码" + orex.Number.ToString() + "，请联系bot管理员。");
            }
        }

        /// <summary>
        /// 查询BOSS进度的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtProgress">dt格式进度表</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static void GetProgress(string strGrpID, out DataTable dtProgress)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,20),
                new OracleParameter(":o_refProgress",OracleDbType.RefCursor)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Direction = ParameterDirection.Output;
            try
            {
                dtProgress = DBHelper.ExecuteProd2DT("PROC_PROGQUERY", param);
            }
            catch (OracleException orex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行PROC_PROGQUERY时跳出错误：" + orex);
                dtProgress = null;
            }
        }




        /// <summary>
        /// 根据BOSS或周目或两方查询出刀记录的方法
        /// </summary>
        /// <param name="intBossCode">BOSS代码</param>
        /// <param name="intRound">周目数</param>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtDmgRecords">dt格式的伤害数据</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDmgRecords(int intBossCode, int intRound, string strGrpID, out DataTable dtDmgRecords)
        {
            Console.WriteLine("启动数据库查询语句");
            string sqlPaddingPattern = "";
            int elementCounter = 0;
            if (intBossCode > -1)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "a.bc = " + intBossCode;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and a.bc = " + intBossCode;
                    elementCounter += 1;
                }
            }
            if (intRound > -1)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "a.round = " + intRound;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and a.round = " + intRound;
                    elementCounter += 1;
                }
            }
            if (elementCounter < 1)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败：无查询条件。");
                dtDmgRecords = null;
                return false;
            }
            string sqlQryDmgRecByBCnRound = "select userid,dmg,round,bc,extime,eventid,To_char(TIME, 'mm\"月\"dd\"日\"hh24\"点\"') as time,nvl(b.MBRNAME,'已不在名单') as name from TTL_DMGRECORDS a " +
                "left join (select MBRID,MBRNAME,GRPID from TTL_MBRLIST) b on a.USERID=b.MBRID and a.GRPID = b.GRPID " +
                "where a.grpid = '" + strGrpID + "' and " + sqlPaddingPattern + " and a.TIME >= trunc(sysdate,'mm') order by a.eventid asc";
            Console.WriteLine("将要查询的SQL语句为：" + sqlQryDmgRecByBCnRound);
            try
            {
                dtDmgRecords = DBHelper.GetDataTable(sqlQryDmgRecByBCnRound);
                Console.WriteLine("SQL语句成功执行");
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败，SQL：" + sqlQryDmgRecByBCnRound + "。\r\n" + oex);
                dtDmgRecords = null;
                return false;
            }
        }

        /// <summary>
        /// 根据UID查询的方法（当日）
        /// </summary>
        /// <param name="douUserID">QQ号</param>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtStart">查询条件：开始时间</param>
        /// <param name="dtEnd">查询条件：结束时间</param>
        /// <param name="dtDmgRecords">dt格式的伤害数据</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDmgRecords(double douUserID, string strGrpID, DateTime dtStart, DateTime dtEnd, out DataTable dtDmgRecords)
        {
            string sqlQryDmgRecByUID = "select userid,dmg,round,bc,extime,eventid,To_char(TIME, 'mm\"月\"dd\"日\"hh24\"点\"') as time,nvl(b.MBRNAME,'已不在名单') as name from TTL_DMGRECORDS a " +
                "left join (select MBRID,MBRNAME,GRPID from TTL_MBRLIST) b on a.USERID=b.MBRID and a.GRPID = b.GRPID " +
                "where a.grpid = '" + strGrpID + "' and a.userid = '" + douUserID + "' and time between to_date('" + dtStart + "', 'yyyy/mm/dd hh24:mi:ss') and to_date('" + dtEnd + "','yyyy/mm/dd hh24:mi:ss')" +
                " order by a.eventid asc";
            try
            {
                dtDmgRecords = DBHelper.GetDataTable(sqlQryDmgRecByUID);
                Console.WriteLine("SQL语句成功执行");
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败，SQL：" + sqlQryDmgRecByUID + "。\r\n" + oex);
                dtDmgRecords = null;
                return false;
            }
        }

        /// <summary>
        /// 根据UID查询的方法（整期）
        /// </summary>
        /// <param name="douUserID">QQ号</param>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtDmgRecords">dt格式的伤害数据</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDmgRecords_All(double douUserID, string strGrpID, out DataTable dtDmgRecords)
        {
            string sqlQryDmgRecByUID = "select userid,dmg,round,bc,extime,eventid,To_char(TIME, 'mm\"月\"dd\"日\"hh24\"点\"') as time,nvl(b.MBRNAME,'已不在名单') as name from TTL_DMGRECORDS a " +
                "left join (select MBRID,MBRNAME,GRPID from TTL_MBRLIST) b on a.USERID=b.MBRID and a.GRPID = b.GRPID " +
                "where a.grpid = '" + strGrpID + "' and a.userid = '" + douUserID + "'" + " order by a.eventid asc";
            try
            {
                dtDmgRecords = DBHelper.GetDataTable(sqlQryDmgRecByUID);
                Console.WriteLine("SQL语句成功执行");
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败，SQL：" + sqlQryDmgRecByUID + "。\r\n" + oex);
                dtDmgRecords = null;
                return false;
            }
        }

        /// <summary>
        /// 读取所有限值设置的方法
        /// </summary>
        /// <param name="dtLimits"></param>
        /// <returns></returns>
        public static bool QueryLimits(out DataTable dtLimits)
        {
            string sqlQryLimits = "select TYPE,VALUE from TTL_LIMITS";
            try
            {
                dtLimits = DBHelper.GetDataTable(sqlQryLimits);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("读取限值时失败，SQL：" + sqlQryLimits + "。\r\n" + oex);
                dtLimits = null;
                return false;
            }
        }

        /// <summary>
        /// 检查指定用户最后一刀是否为尾刀
        /// </summary>
        /// <param name="intIsLastAtk">0：否；1：是。</param>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <returns></returns>
        public static bool CheckLastAttack(string strGrpID, string strUserID, out int intIsLastAtk)
        {
            //Console.WriteLine("启动数据库查询语句");
            string sqlTimeFilter = "";
            if (QueryTimeNowOnDatabase(out DataTable dtResultTime))
            {
                DateTime dtNow = (DateTime)dtResultTime.Rows[0]["sysdate"];
                if (dtNow.Hour >= 0 && dtNow.Hour < 4)
                {
                    //当目前时间在0点到4点间时，筛选开始时间为前一天的凌晨4点
                    sqlTimeFilter = " and TIME >= trunc(sysdate)+(4/24)-1 ";
                }
                else
                {
                    //当目前时间在4点到24点间时，筛选开始时间为当天的凌晨4点
                    sqlTimeFilter = " and TIME >= trunc(sysdate)+(4/24) ";
                }
            }
            else
            {
                Console.WriteLine("数据库错误，未能取回数据库时间。");
            }
            string sqlCheckLA = "select EXTIME from TTL_DMGRECORDS where GRPID = '" + strGrpID + "' and USERID = '" + strUserID + "' " + sqlTimeFilter + " order by round desc, bc desc,time desc";
            try
            {
                DataTable dt = DBHelper.GetDataTable(sqlCheckLA);
                Console.WriteLine("SQL语句成功执行");
                if (dt.Rows.Count > 0)
                {
                    if (int.Parse(dt.Rows[0]["EXTIME"].ToString()) == 2)
                    {
                        intIsLastAtk = 1;
                    }
                    else
                    {
                        intIsLastAtk = 0;
                    }
                }
                else
                {
                    intIsLastAtk = 0;
                }
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "验证补时刀失败，SQL：" + sqlCheckLA + "。\r\n" + oex);
                intIsLastAtk = 0;
                return false;
            }
        }

    }
}
