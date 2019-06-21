﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.Model;

namespace Marchen.DAL
{
    class RecordDAL
    {
        /// <summary>
        /// 伤害上报的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户ID(qq号)</param>
        /// <param name="intDMG">伤害值</param>
        /// <param name="intRound">周目值</param>
        /// <param name="intBossCode">BOSS代码</param>
        /// <param name="intEID">事件ID</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool DamageDebrief(string strGrpID, string strUserID, int intDMG, int intRound, int intBossCode, int intExTime, out int intEID)
        {
            DataTable dtMaxEID = new DataTable();
            int intEventID = 1;
            string sqlQryMaxEID = "select max(eventid) as maxeid from TTL_DMGRECORDS where GRPID = '" + strGrpID + "' and TIME >= trunc(sysdate,'mm')+1 and TIME < trunc(add_months(sysdate,1),'mm')+1";
            try
            {
                dtMaxEID = DBHelper.GetDataTable(sqlQryMaxEID);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询最大EID时返回错误，SQL：" + sqlQryMaxEID + "。\r\n" + oex);
                intEID = 0;
                return false;
            }
            if (dtMaxEID.Rows[0]["maxeid"].ToString() != null && dtMaxEID.Rows[0]["maxeid"].ToString() != "")
            {
                intEventID = int.Parse(dtMaxEID.Rows[0]["maxeid"].ToString()) + 1;
            }
            string sqlDmgDbrf = "insert into TTL_DMGRECORDS(grpid,userid,dmg,round,bc,extime,time,eventid) " +
                "values('" + strGrpID + "','" + strUserID + "'," + intDMG + "," + intRound + "," + intBossCode + "," + intExTime + ",sysdate," + intEventID + ")";
            try
            {
                DBHelper.ExecuteCommand(sqlDmgDbrf);
                intEID = intEventID;
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("上报伤害时返回错误，SQL：" + sqlDmgDbrf + "。\r\n" + oex);
                intEID = 0;
                return false;
            }
        }

        /// <summary>
        /// 记录无法被识别的语句的方法，可以通过你自己的人工学习来优化自然语言识别方向
        /// 因为没什么用而且占空间就没投用了，连表都没建
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <param name="strUnknownContext">没被识别的文本</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool RecordUnknownContext(string strGrpID, string strUserID, string strUnknownContext)
        {
            try
            {
                string sqlUploadUnknownContext = "insert into FAILEDCOMMAND(CMDCONTEXT,FROMGROUPID) values('" + strUnknownContext + "','" + strGrpID + "')";
                DBHelper.ExecuteCommand(sqlUploadUnknownContext);
                Console.WriteLine("失效命令上传数据库成功\r\n" + strUnknownContext);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("失效命令上传数据库失败\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 创建伤害统计表的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool CreateTablesForGuildDamage(string strGrpID)
        {
            Console.WriteLine("未查询到群：" + strGrpID + "存在伤害统计表，尝试创建\r\n");
            try
            {
                DBHelper.ExecCreaGDT(DBProperties.DBCreaGDTProcName, strGrpID);
                Console.WriteLine("群：" + strGrpID + "创建伤害统计表成功。\r\n");
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "创建伤害统计表失败。\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 查询EventID对应记录的方法
        /// </summary>
        /// <param name="intEID">EventID</param>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtDmgRec">返回dt</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDmgRecByEID(int intEID, string strGrpID, out DataTable dtDmgRec)
        {
            string sqlQryDmgRec = "select userid,dmg,round,bc,extime from TTL_DMGRECORDS " +
                "where grpid = '" + strGrpID + "' and eventid =" + intEID + " and " +
                "time >= trunc(sysdate,'mm')+1 and time < trunc(add_months(sysdate,1),'mm')+1";
            try
            {
                dtDmgRec = DBHelper.GetDataTable(sqlQryDmgRec);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询伤害记录时返回错误，SQL：" + sqlQryDmgRec + "。\r\n" + oex);
                dtDmgRec = null;
                return false;
            }
        }

        /// <summary>
        /// 根据eventID修改对应数据的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intDMG">伤害</param>
        /// <param name="intRound">周目</param>
        /// <param name="intBossCode">BOSS代号</param>
        /// <param name="intExTime">是否补时；1：是，2：否。</param>
        /// <param name="intEID">EventID</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool DamageUpdate(string strGrpID, string strUserID, int intDMG, int intRound, int intBossCode, int intExTime, int intEID)
        {
            string sqlDmgUpd = " update TTL_DMGRECORDS " +
                "set userid = '" + strUserID + "', dmg = " + intDMG + ", round = " + intRound + ", bc = " + intBossCode + ", extime = " + intExTime + " " +
                "where time >= trunc(sysdate,'mm')+1 and time < trunc(add_months(sysdate,1),'mm')+1 and grpid = '" + strGrpID + "' and eventid = " + intEID + "";
            try
            {
                DBHelper.ExecuteCommand(sqlDmgUpd);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("修改伤害时返回错误，SQL：" + sqlDmgUpd + "。\r\n" + oex);
                return false;
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
        /// <param name="strGrpID">群号</param>
        /// <param name="dtStart">开始时间（日期，时间）</param>
        /// <param name="dtEnd">结束时间（日期，时间）</param>
        /// <param name="dtInsuff">返回dt格式的出刀情况(userid,cmain=首刀次数，cex=补时刀数)</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryStrikeStatus(string strGrpID, DateTime dtStart, DateTime dtEnd, out DataTable dtInsuff)
        {
            string sqlQueryStrikeStatus = "select distinct(a.MBRID),a.MBRNAME,nvl(cm,0) as cmain,nvl(ce,0) as cex from " +
                "(select MBRID,MBRNAME from TTL_MBRLIST where grpid ='" + strGrpID + "' and MBRID is not null) a " +
                "left join (select userid,count(CASE WHEN EXTIME = 0 THEN 1 ELSE NULL END) as cm," +
                "count(CASE WHEN EXTIME = 1 THEN 1 ELSE NULL END) as ce from TTL_DMGRECORDS where grpid = '" + strGrpID + "' and " +
                "time between to_date('" + dtStart + "', 'yyyy/mm/dd hh24:mi:ss') and to_date('" + dtEnd + "','yyyy/mm/dd hh24:mi:ss') group by userid) b " +
                "on a.MBRID=b.userid";
            try
            {
                dtInsuff = DBHelper.GetDataTable(sqlQueryStrikeStatus);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询出刀状态时发生错误，SQL：" + sqlQueryStrikeStatus + "。\r\n" + oex);
                dtInsuff = null;
                return false;
            }
        }

        /// <summary>
        /// 查询BOSS进度的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtProgress">dt格式进度表</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GetBossProgress(string strGrpID, out DataTable dtProgress)
        {
            string sqlQueryProgress = "select c.maxbc,c.maxround,(d.HP-c.totaldmg) as hpremain from " +
                "(select max(a.MAXBC) as maxbc, max(a.MAXROUND) as maxround, nvl(sum(b.DMG), 0) as totaldmg from " +
                "(select nvl(max(bc), 1) as maxbc, nvl(max(round), 1) as maxround from TTL_DMGRECORDS where " +
                "grpid = '" + strGrpID + "' and TIME between trunc(sysdate, 'mm') + 1 and sysdate and round = (select max(round) from " +
                "TTL_DMGRECORDS where grpid = '" + strGrpID + "' and TIME between trunc(sysdate, 'mm') + 1 and sysdate)) a " +
                "left join (select dmg, bc, round from TTL_DMGRECORDS where grpid = '" + strGrpID + "' and TIME >= trunc(sysdate, 'mm') + 1 and TIME <= sysdate) b " +
                "on a.MAXBC = b.bc and a.maxround = b.round) c " +
                "left join (select roundmin, roundmax, bc, hp from ttl_hpset) d on c.MAXROUND between d.ROUNDMIN and d.ROUNDMAX and c.MAXBC = d.bc";
            try
            {
                dtProgress = DBHelper.GetDataTable(sqlQueryProgress);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询目前进度时发生错误，SQL：" + sqlQueryProgress + "。\r\n" + oex);
                dtProgress = null;
                return false;
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
        public static bool QueryDmgRecords(int intBossCode, int intRound, double douUserID, string strGrpID, out DataTable dtDmgRecords)
        {
            //string strUserID = "";
            Console.WriteLine("启动数据库查询语句");
            string sqlQryDmgRecByBCnRound = "";
            string sqlPaddingPattern = "";
            int elementCounter = 0;
            if (intBossCode > -1)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "bc = " + intBossCode;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and bc = " + intBossCode;
                    elementCounter += 1;
                }
            }
            if (intRound > -1)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "round = " + intRound;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and round = " + intRound;
                    elementCounter += 1;
                }
            }
            if (douUserID > -1)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "userid = " + douUserID;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and userid = " + douUserID;
                    elementCounter += 1;
                }
            }
            if (elementCounter < 1)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败：无查询条件。");
                dtDmgRecords = null;
                return false;
            }
            sqlQryDmgRecByBCnRound = "select userid,dmg,round,bc,extime,eventid from TTL_DMGRECORDS where grpid = '" + strGrpID + "' and " + sqlPaddingPattern + " order by eventid asc";
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
    }
}
