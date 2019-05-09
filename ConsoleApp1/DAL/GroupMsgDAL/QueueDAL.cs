using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Marchen.DAL
{
    class QueueDAL
    {
        /// <summary>
        /// 查询本群是否激活bot功能与属于哪一个游戏的群
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="intStat">状态：0：未激活；1：激活；-2：无资料；-1：数据库问题；-100：非指定游戏，不执行</param>
        /// <param name="intType">游戏类型：0：公主连接</param>
        /// <returns></returns>
        public static bool GroupRegVerify(string strGrpID,out DataTable dtVfyResult)
        {
            string sqlGrpVfy = "select ORG_STAT,ORG_TYPE from TTL_ORGLIST where ORG_ID='" + strGrpID + "'";
            try
            {
                dtVfyResult = DBHelper.GetDataTable(sqlGrpVfy);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex1)
            {
                Console.WriteLine(orex1);
                Console.WriteLine("群：" + strGrpID + "进行群有效性查询时，数据库连接失败");
                dtVfyResult = null;
                return false;
            }
            //if (dtGrpStat.Rows.Count == 0)
            //{
            //    Console.WriteLine("群：" + strGrpID + "不在激活列表内");
            //    intStat = -2;
            //}
            //else if (dtGrpStat.Rows.Count > 1)
            //{
            //    Console.WriteLine("群：" + strGrpID + "进行群有效性查询时，查出非单一结果");
            //    intStat = -2;
            //}
            //else
            //{
            //    intStat = int.Parse(dtGrpStat.Rows[0]["ORG_STAT"].ToString());
            //    int intType = int.Parse(dtGrpStat.Rows[0]["ORG_TYPE"].ToString());
            //    if (intType == 0)//如果本群注册为公主连接群
            //    {
            //        if (intStat == 1)
            //        {
            //            Console.WriteLine("群：" + strGrpID + "，验证通过");
            //        }
            //        else if (intStat == 0)
            //        {
            //            Console.WriteLine("群：" + strGrpID + "状态为未激活");
            //        }
            //        else
            //        {
            //            Console.WriteLine("群：" + strGrpID + "进行群有效性查询时，发生了预料外的错误");
            //        }
            //    }
            //    else
            //    {
            //        intStat = -100;
            //        //其他游戏的群不动作，通过转发端口转发传到下一个程序
            //    }
            //}
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
            string sqlQrySeq = "select id,seq,name from TTL_Queue where grpid = '" + strGrpID + "' and seq > 0 order by seq asc";
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
            string sqlQryTopId = "delete from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq = (select MIN(seq) as seq from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq > 0 group by id)";
            try
            {
                intDelCount = DBHelper.ExecuteCommand(sqlQryTopId);
                return true;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orex)
            {
                Console.WriteLine("修改队列时跳出错误，SQL：" + sqlQryTopId + "。\r\n" + orex);
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
                DataRow[] drExistsID = dtNameList.Select("id='" + strUserID + "'");
                intMemberCount = dtNameList.Rows.Count;
                if (drExistsID.Length == 1)
                {
                    //存在，更新
                    string sqlUpdateName = "update TTL_Queue set name = '" + strUserGrpCard + "' where ID = '" + strUserID + "' and grpid = '" + strGrpID + "'";
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
                        string sqlInsertName = "insert into TTL_Queue(seq,id,name,grpid) values(" + 0 + ",'" + strUserID + "','" + strUserGrpCard + "','" + strGrpID + "')";
                        try
                        {
                            DBHelper.ExecCmdNoCount(sqlInsertName);
                            intMemberCount += 1;
                            return true;
                        }
                        catch (Oracle.ManagedDataAccess.Client.OracleException orex)
                        {
                            Console.WriteLine("新增名单时发生错误，SQL：" + sqlInsertName + "。\r\n" + orex);
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
            string sqlShowNameList = "select id,name from TTL_Queue where grpid = '" + strGrpID + "' and seq = 0 order by id asc";
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
        /// 删除名单的方法
        /// </summary>
        /// <param name="strGrpID">群号</param>
        /// <param name="strUserID">用户QQ号</param>
        /// <param name="deletedCount">被删除的行数</param>
        /// <returns>true：执行成功；false：执行失败。</returns>
        public static bool NameListDelete(string strGrpID, string strUserID, out int deletedCount)
        {
            string sqlDeleteNameList = "delete from TTL_Queue where grpid = '" + strGrpID + "' and id = '" + strUserID + "' and seq = 0";
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

        /// <summary>
        /// 获取成员报名状态及数量，返回值0:不存在；1:存在；-1：执行错误
        /// </summary>
        /// <param name="strGrpID"></param>
        /// <param name="strUserID"></param>
        /// <returns>0:不存在；1:存在；-1：执行错误</returns>
        public static int MemberCheck(string strGrpID, string strUserID)
        {
            if (QryNameList(strGrpID, out DataTable dtNameList))
            {
                DataRow[] drExistsID = dtNameList.Select("id='" + strUserID + "'");
                if (drExistsID.Length == 1)
                {
                    return 1;
                }
                if (drExistsID.Length == 0)
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
            else
            {
                Console.WriteLine("检查成员是否报名时发生错误。");
                return -1;
            }
        }
    }
}
