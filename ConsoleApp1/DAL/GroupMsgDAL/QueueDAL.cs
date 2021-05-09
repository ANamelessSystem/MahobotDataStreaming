using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading;
using Oracle.ManagedDataAccess.Client;

namespace Marchen.DAL
{
    class QueueDAL
    {
        /// <summary>
        /// 查询本群是否激活对应类型的bot功能
        /// <param name="strGrpID">群号</param>
        /// <param name="dtVfyResult">返回dt格式的结果</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool GroupRegVerify(string strGrpID, out DataTable dtVfyResult)
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
        /// 加入队列或挂树的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="intBC">BOSS编号</param>
        /// <param name="strUserID">用户Q号</param>
        /// <param name="intType">类型，0通常，1补时，2挂树</param>
        /// <param name="intRound">周目</param>
        public static void JoinQueue(string strGrpID, int intBC, string strUserID, int intType, int intRound = 0)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,20),
                new OracleParameter(":i_numBossCode", OracleDbType.Int16,1),
                new OracleParameter(":i_varUserID", OracleDbType.Varchar2,20),
                new OracleParameter(":i_numType", OracleDbType.Int16,1),
                new OracleParameter(":i_numRound", OracleDbType.Int16,3)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = intBC;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = strUserID;
            param[2].Direction = ParameterDirection.Input;
            param[3].Value = intType;
            param[3].Direction = ParameterDirection.Input;
            param[4].Value = intRound;
            param[4].Direction = ParameterDirection.Input;
            try
            {
                DBHelper.ExecuteProdNonQuery("PROC_QUEUEADD_NEW", param);
            }
            catch (OracleException orex)
            {
                if (orex.Number == 20101)
                {
                    throw new Exception("无法同时加入多个队伍，请先使用C3退出现有队伍。");
                }
                else if (orex.Number == 20102)
                {
                    throw new Exception("尚未加入队伍，请先加入一个队伍。");
                }
                else if (orex.Number == 20104)
                {
                    throw new Exception("缺少周目值，请指定周目值。");
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString() + "执行PROC_QUEUEADD_NEW时跳出错误：" + orex);
                    throw new Exception("未知数据库错误代码" + orex.Number.ToString() + "，请联系bot管理员。");
                }
            }
        }

        /// <summary>
        /// 读取当前队列的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="intBC"></param>
        /// <param name="strUserID"></param>
        /// <param name="intType"></param>
        /// <param name="dtQueue"></param>
        public static void ShowQueue(string strGrpID, int intBC, string strUserID, int intType, out DataTable dtQueue)
        {
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,20),
                new OracleParameter(":i_numBossCode", OracleDbType.Int16,1),
                new OracleParameter(":i_varUserID", OracleDbType.Varchar2,20),
                new OracleParameter(":i_numQueryAll", OracleDbType.Int16,1),
                new OracleParameter(":o_refQueue",OracleDbType.RefCursor)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = intBC;
            param[1].Direction = ParameterDirection.Input;
            param[2].Value = strUserID;
            param[2].Direction = ParameterDirection.Input;
            param[3].Value = intType;
            param[3].Direction = ParameterDirection.Input;
            param[4].Direction = ParameterDirection.Output;
            try
            {
                dtQueue = DBHelper.ExecuteProdQuery("PROC_QUEUEQUERY_NEW", param);
            }
            catch (OracleException orex)
            {
                if (orex.Number == 20102)
                {
                    throw new Exception("尚未加入队列，请先加入一个队列，或使用BOSS编号指定查询的队列，或使用all字段查询所有队列。");
                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString() + "执行PROC_QUEUEQUERY_NEW时跳出错误：" + orex);
                }
                dtQueue = null;
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
            catch (OracleException orex)
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
        public static bool UpdateQueueToSos(string strGrpID, string strUserID, int intBossCode, int intRound, out int intUpdCount)
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
