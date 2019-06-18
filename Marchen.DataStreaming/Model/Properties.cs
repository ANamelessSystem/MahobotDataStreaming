using System;
using System.Collections.Generic;
using System.Text;
using Sisters.WudiLib;

namespace Marchen.Model
{
    public static class ApiProperties
    {
        /// <summary>
        /// 监听地址
        /// </summary>
        public static string ApiPostAddr { get; set; }

        /// <summary>
        /// API端口地址
        /// </summary>
        public static string ApiAddr { get; set; }

        /// <summary>
        /// API实例
        /// </summary>
        public static HttpApiClient HttpApi { get; set; }

        /// <summary>
        /// 内容转发地址，将收到的所有内容再次上报到此地址
        /// </summary>
        public static string ApiForwardToAddr { get; set; }
    }

    public static class SelfProperties
    {
        /// <summary>
        /// 自己的QQ号，保持自动获取
        /// </summary>
        public static string SelfID { get; set; }
    }

    public static class GroupProperties
    {
        /// <summary>
        /// 是否显示剩余HP
        /// </summary>
        public static bool IsHpShow { get; set; }
        
        /// <summary>
        /// GMT设定，正负数有效
        /// </summary>
        public static int GMTValue { get; set; }
    }

    class DBProperties
    {
        /// <summary>
        /// 数据库用户名
        /// </summary>
        public static string DBUserID { get; set; }

        /// <summary>
        /// 数据库密码
        /// </summary>
        public static string DBPassword { get; set; }

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
