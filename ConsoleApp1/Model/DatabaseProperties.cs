using System;
using System.Collections.Generic;
using System.Text;

namespace Marchen.Model
{
    class DBProperties
    {
        /// <summary>
        /// 数据库用户名
        /// </summary>
        public static string DBUserID { get; set; }
        /// <summary>
        /// 数据库密码
        /// </summary>
        public static string DBPassWord { get; set; }
        /// <summary>
        /// 数据库地址
        /// </summary>
        public static string DBAddress { get; set; }
        /// <summary>
        /// 数据库监听端口
        /// </summary>
        public static string DBPort { get; set; }
        /// <summary>
        /// 数据库服务名
        /// </summary>
        public static string DBServiceName { get; set; }
        /// <summary>
        /// 创建伤害统计表格的存储过程名
        /// </summary>
        public static string DBCreaGDTProcName { get; set; }
    }
}
