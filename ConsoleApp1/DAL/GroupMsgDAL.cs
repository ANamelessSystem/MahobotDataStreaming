using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Marchen.Model;

namespace Marchen.DAL
{
    class QueueDAL
    {
        /// <summary>
        /// 验证群组是否启用bot服务的方法
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <returns>
        /// 1：验证通过；0：未开启bot服务；10：数据库故障；11：信息表数据有误；12：预料外的错误。
        /// </returns>
        public static int GroupRegVerify(string groupId)
        {
            string sqlGrpVfy = "select ORG_STAT from TTL_ORGLIST where ORG_ID='" + groupId + "' and ORG_TYPE = 0";
            DataTable dtGrpStat = new DataTable();
            try
            {
                dtGrpStat = DBHelper.GetDataTable(sqlGrpVfy);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex1)
            {
                Console.WriteLine(orex1);
                Console.WriteLine("进行群有效性查询时，数据库连接失败");
                return 10;
            }
            if (dtGrpStat.Rows.Count == 0)
            {
                return 0;
            }
            else if (dtGrpStat.Rows.Count > 1)
            {
                Console.WriteLine("进行群有效性查询时，查出非单一结果");
                return 11;
            }
            else
            {
                int grpVfyFlag = int.Parse(dtGrpStat.Rows[0]["ORG_STAT"].ToString());
                if (grpVfyFlag == 1)
                {
                    Console.WriteLine("进行群有效性查询时，验证通过");
                    return 1;
                }
                else if (grpVfyFlag == 0)
                {
                    Console.WriteLine("进行群有效性查询时，验证未通过");
                    return 0;
                }
                else
                {
                    Console.WriteLine("进行群有效性查询时，发生了预料外的错误");
                    return 12;
                }
            }
        }

        /// <summary>
        /// 加入队列的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <param name="strUserGrpCard">用户群名片</param>
        /// <returns>
        /// true：执行成功；false：执行失败。
        /// </returns>
        public static bool AddQueue(string strGrpID, string strUserID, string strUserGrpCard)
        {
            DataTable dtMaxSeq = new DataTable();
            int intSequence = 1;
            string sqlQryMaxSeq = "select max(seq) as maxseq from TTL_Queue where grpid ='" + strGrpID + "'";
            try
            {
                dtMaxSeq = DBHelper.GetDataTable(sqlQryMaxSeq);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("查询最大序号时跳出错误，SQL：" + sqlQryMaxSeq + "。\r\n" + orex);
                return false;
            }
            if (dtMaxSeq.Rows[0]["maxseq"].ToString().Trim() != null && dtMaxSeq.Rows[0]["maxseq"].ToString().Trim() != "")
            {
                intSequence = int.Parse(dtMaxSeq.Rows[0]["maxseq"].ToString()) + 1;
            }
            string sqlAddSeq = "insert into TTL_Queue(seq,id,name,grpid) values(" + intSequence + ",'" + strUserID + "','" + strUserGrpCard + "','" + strGrpID + "')";
            try
            {
                DBHelper.ExecuteCommand(sqlAddSeq);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("写入队列时跳出错误，SQL：" + sqlAddSeq + "。\r\n" + orex);
                return false;
            }
        }

        /// <summary>
        /// 读取当前队列的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtQueue">包含id（用户qq号）,seq（序号）,name（用户群名片）的查询结果</param>
        /// <returns>
        /// true：执行成功；false：执行失败。
        /// </returns>
        public static bool ShowQueue(string strGrpID, out DataTable dtQueue)
        {
            string sqlQrySeq = "select id,seq,name from TTL_Queue where grpid = '" + strGrpID + "' order by seq asc";
            try
            {
                dtQueue = DBHelper.GetDataTable(sqlQrySeq);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("查询队列时跳出错误，SQL：" + sqlQrySeq + "。\r\n" + orex);
                dtQueue = null;
                return false;
            }
        }

        /// <summary>
        /// 退出队列的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <param name="deletedCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QuitQueue(string strGrpID, string strUserID, out int deletedCount)
        {
            string sqlQryTopId = "delete from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq = (select MIN(seq) as seq from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' group by id)";
            try
            {
                deletedCount = DBHelper.ExecuteCommand(sqlQryTopId);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("修改队列时跳出错误，SQL：" + sqlQryTopId + "。\r\n" + orex);
                deletedCount = 0;
                return false;
            }
        }

        /// <summary>
        /// 清空队列的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="deletedCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool ClearQueue(string strGrpID, out int deletedCount)
        {
            string sqlClrQue = "delete from TTL_Queue where grpid = '" + strGrpID + "'";
            try
            {
                deletedCount = DBHelper.ExecuteCommand(sqlClrQue);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("清空队列时跳出错误，SQL：" + sqlClrQue + "。\r\n" + orex);
                deletedCount = 0;
                return false;
            }
        }
    }

    class RecordDAL
    {
        /// <summary>
        /// 伤害上报的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID"></param>
        /// <param name="intDMG"></param>
        /// <param name="intRound"></param>
        /// <param name="intBossCode"></param>
        /// <param name="intEID"></param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool DamageDebrief(string strGrpID, string strUserID, int intDMG, int intRound, int intBossCode, int intExTime, out int intEID)
        {
            DataTable dtMaxEID = new DataTable();
            int intEventID = 1;
            string sqlQryMaxEID = "select max(eventid) as maxeid from GD_" + strGrpID;
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
            string sqlDmgDbrf = "insert into GD_" + strGrpID + "(userid,dmg,round,bc,extime,time,eventid) values('" + strUserID + "'," + intDMG + "," + intRound + "," + intBossCode + "," + intExTime + ",sysdate," + intEventID + ")";
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
            string sqlQryDmgRec = "select userid,dmg,round,bc,extime from GD_" + strGrpID + " where eventid =" + intEID;
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
            string sqlDmgDbrf = " update GD_" + strGrpID + " set userid = '" + strUserID + "', dmg = " + intDMG + ", round = " + intRound + ", bc = " + intBossCode + ", extime = " + intExTime + " where eventid = " + intEID;
            try
            {
                DBHelper.ExecuteCommand(sqlDmgDbrf);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("修改伤害时返回错误，SQL：" + sqlDmgDbrf + "。\r\n" + oex);
                return false;
            }
        }

        /// <summary>
        /// 检查伤害统计表是否存在的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtTableCount">返回dt</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool CheckClanDmgTable(string strGrpID, out DataTable dtTableCount)
        {
            string sqlCheckTableExist = "select count(*) as count from user_tables where table_name = 'GD_" + strGrpID + "'";
            try
            {
                dtTableCount = DBHelper.GetDataTable(sqlCheckTableExist);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询伤害表是否存在时返回错误，SQL：" + sqlCheckTableExist + "。\r\n" + oex);
                dtTableCount = null;
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
            string sqlQueryStrikeStatus = "select distinct(a.userid),nvl(cm,0) as cmain,nvl(ce,0) as cex from GD_" + strGrpID + " a left join (select userid,count(CASE WHEN EXTIME = 0 THEN 1 ELSE NULL END) as cm,count(CASE WHEN EXTIME = 1 THEN 1 ELSE NULL END) as ce from GD_" + strGrpID + " where time between to_date('" + dtStart + "', 'yyyy/mm/dd hh24:mi:ss') and to_date('" + dtEnd + "','yyyy/mm/dd hh24:mi:ss') group by userid) b on a.userid=b.userid";
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
        /// 根据群号查询伤害表的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtDmgReport">dt格式伤害表</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDamageTable(string strGrpID, out DataTable dtDmgReport)
        {
            string sqlQueryDmgTbl = "select * from GD_" + strGrpID;
            try
            {
                dtDmgReport = DBHelper.GetDataTable(sqlQueryDmgTbl);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("查询伤害表时发生错误：" + oex);
                dtDmgReport = null;
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
            string sqlQueryProgress = "select c.maxbc,c.maxround,(d.HP-c.totaldmg) as hpremain from (select max(a.MAXBC) as maxbc, max(a.MAXROUND) as maxround, sum(b.DMG) as totaldmg from (select max(bc) as maxbc, max(round) as maxround from GD_" + strGrpID + " where round = (select max(round) from GD_" + strGrpID + ")) a left join (select dmg, bc, round from GD_" + strGrpID + ") b on a.MAXBC = b.bc and a.maxround = b.round) c left join (select roundmin, roundmax, bc, hp from ttl_hpset) d on c.MAXROUND between d.ROUNDMIN and d.ROUNDMAX and c.MAXBC = d.bc";
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
        /// <param name="dtDmgRec">dt格式的伤害数据</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QueryDmgRecByBCnRound(int intBossCode, int intRound,string strUserID, string strGrpID, out DataTable dtDmgRec)
        {
            //string strUserID = "";
            string sqlQryDmgRecByBCnRound = "";
            string sqlPaddingPattern = "";
            int elementCounter = 0;
            if (intBossCode > 0)
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
            if (intRound > 0)
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
            if (strUserID != "" || strUserID != null)
            {
                if (elementCounter == 0)
                {
                    sqlPaddingPattern += "userid = " + strUserID;
                    elementCounter += 1;
                }
                else
                {
                    sqlPaddingPattern += "and userid = " + strUserID;
                    elementCounter += 1;
                }
            }
            //if (intBossCode > 0 && intRound > 0)
            //{
            //    sqlQryDmgRecByBCnRound = "select userid,dmg,round,bc,extime,eventid from GD_" + strGrpID + " where bc =" + intBossCode + " and round =" + intRound + " order by eventid asc";
            //}
            //else if (intBossCode > 0)
            //{
            //    sqlQryDmgRecByBCnRound = "select userid,dmg,round,bc,extime,eventid from GD_" + strGrpID + " where bc =" + intBossCode + " order by eventid asc";
            //}
            //else if (intRound > 0)
            //{
            //    sqlQryDmgRecByBCnRound = "select userid,dmg,round,bc,extime,eventid from GD_" + strGrpID + " where round =" + intRound + " order by eventid asc";
            //}
            //else
            //{
            //    Console.WriteLine("群：" + strGrpID + "查询伤害时失败，内容为：BC=" + intBossCode + "，ROUND=" + intRound + "。\r\n");
            //    dtDmgRec = null;
            //    return false;
            //}
            try
            {
                dtDmgRec = DBHelper.GetDataTable(sqlQryDmgRecByBCnRound);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oex)
            {
                Console.WriteLine("群：" + strGrpID + "查询伤害时失败，SQL：" + sqlQryDmgRecByBCnRound + "。\r\n" + oex);
                dtDmgRec = null;
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