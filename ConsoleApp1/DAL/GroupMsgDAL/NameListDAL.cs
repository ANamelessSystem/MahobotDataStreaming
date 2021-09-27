using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;

namespace Marchen.DAL
{
    class NameListDAL
    {
        /// <summary>
        /// 增加或更新名单的方法
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <param name="strUserGrpCard"></param>
        /// <param name="intMemberCount"></param>
        /// <returns></returns>
        public static bool UpdateNameList(string strGrpID, string strUserID, string strUserGrpCard, out int intMemberCount)
        {
            if (QryNameList(strGrpID, out DataTable dtNameList))
            {
                DataRow[] drExistsID = dtNameList.Select("MBRID='" + strUserID + "'");
                intMemberCount = dtNameList.Rows.Count;
                if (drExistsID.Length == 1)
                {
                    //存在，更新
                    string sqlUpdateName = "update TTL_MBRLIST set MBRNAME = '" + strUserGrpCard + "' where MBRID = '" + strUserID + "' and GRPID = '" + strGrpID + "'";
                    try
                    {
                        DBHelper.ExecCmdNoCount(sqlUpdateName);
                        return true;
                    }
                    catch (Oracle.ManagedDataAccess.Client.OracleException orex)
                    {
                        Console.WriteLine("更新名单时发生错误，SQL：" + sqlUpdateName + "。\r\n" + orex);
                        intMemberCount = -1;
                        return false;
                    }
                }
                if (drExistsID.Length == 0)
                {
                    //不存在，检查目前人数
                    if (intMemberCount < 30)
                    {
                        //人数低于30，允许新增
                        if (QryNameListWithNull(strGrpID, out DataTable dtNullList))
                        {
                            int intInsertSeq = int.Parse(dtNullList.Rows[0]["SEQNO"].ToString());
                            string sqlAddNameList = "update TTL_MBRLIST set MBRID = '" + strUserID + "',MBRNAME = '" + strUserGrpCard + "' where SEQNO = " + intInsertSeq + " and GRPID = '" + strGrpID + "'";
                            try
                            {
                                DBHelper.ExecCmdNoCount(sqlAddNameList);
                                intMemberCount += 1;
                                return true;
                            }
                            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
                            {
                                Console.WriteLine("新增名单时发生错误，SQL：" + sqlAddNameList + "。\r\n" + orex);
                                intMemberCount = -1;
                                return false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("新增名单时发生错误。");
                            intMemberCount = -1;
                            return false;
                        }
                    }
                    else
                    {
                        //达到或超过30人，不允许新增
                        Console.WriteLine("新增名单时发生错误，人数为" + intMemberCount + "达到30人。");
                        return false;
                    }
                }
                else
                {
                    //非预想值
                    Console.WriteLine("查询群员是否已报名时返回值非预想值：" + drExistsID.Length.ToString());
                    intMemberCount = -1;
                    return false;
                }
            }
            else
            {
                //sql执行错误
                Console.WriteLine("查询成员列表时返回错误。");
                intMemberCount = -1;
                return false;
            }
        }

        /// <summary>
        /// 读取当前名单的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtNameList"></param>
        /// <returns>
        /// true：执行成功；false：执行失败。
        /// </returns>
        public static bool QryNameList(string strGrpID, out DataTable dtNameList)
        {
            string sqlShowNameList = "select MBRID,MBRNAME,SEQNO from TTL_MBRLIST where GRPID = '" + strGrpID + "' and MBRID is not null order by SEQNO asc";
            try
            {
                dtNameList = DBHelper.GetDataTable(sqlShowNameList);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("查询名单列表时跳出错误，SQL：" + sqlShowNameList + "。\r\n" + orex);
                dtNameList = null;
                return false;
            }
        }

        /// <summary>
        /// 读取当前名单空余处的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="dtNameList"></param>
        /// <returns></returns>
        public static bool QryNameListWithNull(string strGrpID, out DataTable dtNameList)
        {
            string sqlShowNameListWithNull = "select MBRID,MBRNAME,SEQNO from TTL_MBRLIST where GRPID = '" + strGrpID + "' and MBRID is null order by SEQNO asc";
            try
            {
                dtNameList = DBHelper.GetDataTable(sqlShowNameListWithNull);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("查询名单列表时跳出错误，SQL：" + sqlShowNameListWithNull + "。\r\n" + orex);
                dtNameList = null;
                return false;
            }
        }

        /// <summary>
        /// 删除名单的方法（按UID）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <param name="deletedCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool NameListDelete(string strGrpID, string strUserID)
        {
            string sqlDeleteNameList = "update TTL_MBRLIST set MBRID = null,MBRNAME = null where GRPID = '" + strGrpID + "' and MBRID = '" + strUserID + "'";
            try
            {
                DBHelper.ExecuteCommand(sqlDeleteNameList);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("删除名单时跳出错误，SQL：" + sqlDeleteNameList + "。\r\n" + orex);
                return false;
            }
        }

        /// <summary>
        /// 初始化名单的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool NameListInit(string strGrpID)
        {
            try
            {
                DBHelper.ExecProd("PROC_INITMBRLIST", "varGrpID", strGrpID);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("初始化名单时跳出错误。\r\n" + orex);
                return false;
            }
        }

        /// <summary>
        /// 删除名单的方法（按序号）
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="intSeqNO">指定删除的序号</param>
        /// <param name="deletedCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool NameListDelete(string strGrpID, int intSeqNO, out int deletedCount)
        {
            string sqlDeleteNameList = "update TTL_MBRLIST set MBRID = null,MBRNAME = null where GRPID = '" + strGrpID + "' and SEQNO = '" + intSeqNO + "'";
            try
            {
                deletedCount = DBHelper.ExecuteCommand(sqlDeleteNameList);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("删除名单时跳出错误，SQL：" + sqlDeleteNameList + "。\r\n" + orex);
                deletedCount = 0;
                return false;
            }
        }


        public static bool GetMemberName(string strGrpID, string strUserID, out string strResultMbrName)
        {
            string sqlQryMbrNameByUID = "select MBRNAME from TTL_MBRLIST where MBRID = '" + strUserID + "' and GRPID = '" + strGrpID + "'";
            strResultMbrName = "";
            try
            {
                DataTable dtResult = DBHelper.GetDataTable(sqlQryMbrNameByUID);
                if (dtResult.Rows.Count != 0)
                {
                    if (dtResult.Rows[0]["MBRNAME"].ToString().Length > 0)
                    {
                        strResultMbrName = dtResult.Rows[0]["MBRNAME"].ToString();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("搜寻用户昵称时出现错误，SQL：" + sqlQryMbrNameByUID + "。\r\n" + orex);
                return false;
            }
        }

        /// <summary>
        /// 获取成员报名状态及数量，返回值0:不存在；1:存在；-1：执行错误
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <returns>0:不存在；1:存在；-1：执行错误</returns>
        public static int MemberCheck(string strGrpID, string strUserID)
        {
            int intResult = 0;
            OracleParameter[] param = new OracleParameter[]
            {
                new OracleParameter(":i_varGrpID", OracleDbType.Varchar2,40),
                new OracleParameter(":i_varUserID", OracleDbType.Varchar2,40),
                new OracleParameter(":o_bResult", OracleDbType.Int16)
            };
            param[0].Value = strGrpID;
            param[0].Direction = ParameterDirection.Input;
            param[1].Value = strUserID;
            param[1].Direction = ParameterDirection.Input;
            param[2].Direction = ParameterDirection.Output;
            try
            {
                DBHelper.ExecuteProdNonQuery("PROC_MBRCHECK", param);
            }
            catch (OracleException orex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "执行PROC_MBRCHECK时跳出错误：" + orex);
                return -1;
            }
            intResult = int.Parse(param[2].Value.ToString());
            if (intResult == 1)
            {
                return 1;
            }
            else if (intResult == 0)
            {
                Console.WriteLine("成员未报名。USERID:" + strUserID);
                return 0;
            }
            else
            {
                Console.WriteLine("检查成员是否报名时返回结果不唯一。USERID=" + strUserID);
                return -1;
            }
        }
    }
}