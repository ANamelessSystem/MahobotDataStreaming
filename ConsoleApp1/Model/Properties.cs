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
        public static int HttpApiPort { get; set; }

        /// <summary>
        /// API端口地址
        /// </summary>
        public static string HttpApiIP { get; set; }

        ///// <summary>
        ///// API实例
        ///// </summary>
        //public static HttpApiClient HttpApi { get; set; }

        /// <summary>
        /// 内容转发地址，将收到的所有内容再次上报到此地址
        /// </summary>
        public static string HttpApiAuthKey { get; set; }
    }

    public static class SelfProperties
    {
        /// <summary>
        /// 自己的QQ号，保持自动获取
        /// </summary>
        public static string SelfID { get; set; }
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
    }
}
