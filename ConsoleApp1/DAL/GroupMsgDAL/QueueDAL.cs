using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;

namespace Marchen.DAL
{
    class QueueDAL
    {
        /// <summary>
        /// 查询本群是否激活对应类型的bot功能
        /// <param name="strGrpID">群号</param>
        /// <param name="dtVfyResult">返回dt格式的结果</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GroupRegVerify(string strGrpID,out DataTable dtVfyResult)
        {
            string sqlGrpVfy = "select ORG_STAT,ORG_TYPE from TTL_ORGLIST where ORG_ID='" + strGrpID + "'";
            bool bResult = false;
            dtVfyResult = null;
            for (int i = 0; i < 3;)
            {
                try
                {
                    dtVfyResult = DBHelper.GetDataTable(sqlGrpVfy);
                    bResult = true;
                    break;
                }
                catch (Oracle.ManagedDataAccess.Client.OracleException orex1)
                {
                    if (orex1.Number == 3135 || orex1.Number == 12570 || orex1.Number == 12571)
                    {
                        //两次连接数据库相隔时间太长时，有可能会出现“远程主机强迫关闭一个现有连接”这个错误，现在特定捕获这一错误进行一定量的重试
                        Console.WriteLine(DateTime.Now.ToString() + "\r\nGroupRegVerify(QueueDAL.cs)：" + orex1.Message + "重试次数" + i.ToString());
                        i += 1;
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "\r\nGroupRegVerify(QueueDAL.cs)：" + orex1.Message);
                        break;
                    }
                }
                Thread.Sleep(100);
            }
            if (bResult)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 加入队列的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool AddQueue(string strGrpID, string strUserID,int intSosFlag = 0)
        {
            //查询队列表中的最大序列值，如果查询结果是非空，则序号为查询的最大序号+1，如果查询结果为空则使用1
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
            string sqlAddSeq = "insert into TTL_Queue(seq,id,grpid,sosflag) values(" + intSequence + ",'" + strUserID + "','" + strGrpID + "'," + intSosFlag + ")";
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
            string sqlQrySeq = "select ID,SEQ,SOSFLAG,BC,ROUND,b.MBRNAME from TTL_QUEUE a " +
                "left join (select MBRID,MBRNAME,GRPID from TTL_MBRLIST) b " +
                "on a.GRPID = b.GRPID and a.ID = b.MBRID " +
                "where a.GRPID = '" + strGrpID + "' and a.SEQ > 0 order by a.SEQ asc";

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
        /// <param name="intDelCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool QuitQueue(string strGrpID, string strUserID, out int intDelCount)
        {
            string sqlDelTopQueue = "delete from TTL_Queue where GRPID = '" + strGrpID + "' and ID = '" + strUserID + "' and seq = (select MIN(seq) as seq from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq > 0 group by id)";
            try
            {
                intDelCount = DBHelper.ExecuteCommand(sqlDelTopQueue);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("修改队列时跳出错误，SQL：" + sqlDelTopQueue + "。\r\n" + orex);
                intDelCount = 0;
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
            string sqlClrQue = "delete from TTL_Queue where grpid = '" + strGrpID + "' and seq > 0";
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

        /// <summary>
        /// 挂树等救的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">QQ号</param>
        /// <param name="intUpdCount">受到影响的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool UpdateQueueToSos(string strGrpID, string strUserID,int intBossCode,int intRound, out int intUpdCount)
        {
            string sqlUpdateQueueToSos = "update TTL_Queue set sosflag = '1',bc = '" + intBossCode + "',round = '" + intRound + "' " +
                "where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq = (select MIN(seq) as seq from TTL_Queue " +
                "where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq > 0 and sosflag != 1 group by id)";
            try
            {
                intUpdCount = DBHelper.ExecuteCommand(sqlUpdateQueueToSos);
                return true;
            } 
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("修改队列时跳出错误，SQL：" + sqlUpdateQueueToSos + "。\r\n" + orex);
                intUpdCount = 0;
                return false;
            }
        }

        public static bool QuerySosList(string strGrpID, int intBCNow, int intRoundNow, out DataTable dtSosList)
        {
            string sqlQuerySosList = "select ID as userid from TTL_QUEUE " +
                "where GRPID = '" + strGrpID + "' and SOSFLAG = '1' and (ROUND < " + intRoundNow + " " +
                "or (ROUND = " + intRoundNow + " and BC < " + intBCNow + "))";
            try
            {
                dtSosList = DBHelper.GetDataTable(sqlQuerySosList);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("获取BOSS的初期HP时发生错误，SQL：" + sqlQuerySosList + "。\r\n" + orex);
                dtSosList = null;
                return false;
            }
        }
    }
}
