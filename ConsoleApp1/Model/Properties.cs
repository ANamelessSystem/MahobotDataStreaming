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

    public static class ConsoleProperties
    {
        /// <summary>
        /// 是否显示剩余HP
        /// </summary>
        public static bool IsHpShow { get; set; }
    }
}
