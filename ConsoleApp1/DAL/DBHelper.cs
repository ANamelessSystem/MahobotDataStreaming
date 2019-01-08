using System.Data;
using Oracle.ManagedDataAccess.Client;
using Marchen.Model;
using System;

namespace Marchen.DAL
{
    /// <summary>
    /// DBHelper ONLY for oracle
    /// </summary>
    public class DBHelper
    {

        private static OracleConnection connection;
        public static OracleConnection Connection
        {
            get
            {
                string conStr = "User ID =" + DBProperties.DBUserID + "; Password =" + DBProperties.DBPassWord + "; Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = " + DBProperties.DBAddress + ")(PORT = " + DBProperties.DBPort + ")))(CONNECT_DATA = (SERVICE_NAME =" + DBProperties.DBServiceName + ")))";
                if (connection == null)
                {
                    connection = new OracleConnection(conStr);
                }
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                if (connection.State == ConnectionState.Broken)
                {
                    connection.Close();
                    connection.Open();
                }
                return connection;
            }
        }

        /// <summary>
        /// 采用DataTable方式查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns></returns>
        public static DataTable GetDataTable(string sql)
        {
            OracleDataAdapter oda = new OracleDataAdapter(sql, Connection);
            DataTable dt = new DataTable();
            oda.Fill(dt);
            return dt;
        }

        /// <summary>
        /// 返回式SQL执行语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>int</returns>
        public static int ExecuteCommand(string sql)
        {
            OracleCommand cmd = new OracleCommand(sql, Connection);
            int count = 0;
            ///OracleCommand command = connection.CreateCommand();
            OracleTransaction trans = connection.BeginTransaction();
            cmd.Transaction = trans;
            cmd.CommandText = sql;
            count = cmd.ExecuteNonQuery();
            trans.Commit();
            return count;
        }

        /// <summary>
        /// 创建伤害统计表用
        /// </summary>
        /// <param name="prod"></param>
        /// <param name="grpid"></param>
        public static void ExecCreaGDT(string prod,string grpid)
        {
            OracleCommand cmd = new OracleCommand(prod, Connection);
            cmd.CommandType = CommandType.StoredProcedure;
            OracleParameter opgid = cmd.Parameters.Add("grpID", OracleDbType.Varchar2, ParameterDirection.Input);
            opgid.Value = grpid;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 无返回式SQL执行语句
        /// </summary>
        /// <param name="sql">sql语句</param>
        public static void ExecCmdNoCount(string sql)
        {
            OracleCommand cmd = new OracleCommand(sql, Connection);
            cmd.CommandType = CommandType.Text;
            cmd.ExecuteNonQuery();
        }
    }
}
