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
        public static string PostAddress { get; set; }
        /// <summary>
        /// API端口地址
        /// </summary>
        public static string ApiAddress { get; set; }
        /// <summary>
        /// API实例
        /// </summary>
        public static HttpApiClient HttpApi { get; set; }
    }
    public static class SelfProperties
    {
        /// <summary>
        /// 自己的QQ号
        /// </summary>
        public static string SelfID { get; set; }
    }
}
