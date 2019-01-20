using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Marchen.Model;

namespace Marchen
{
    class CfgLoader
    {
        /// <summary>
        /// 写入INI文件
        /// </summary>
        /// <param name="section">节点名称[如[TypeName]]</param>
        /// <param name="key">键</param>
        /// <param name="val">值</param>
        /// <param name="filepath">文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filepath);
        /// <summary>
        /// 读取INI文件
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <param name="key">键</param>
        /// <param name="def">值</param>
        /// <param name="retval">stringbulider对象</param>
        /// <param name="size">字节大小</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retval, int size, string filePath);
        private static string strFilePath = Directory.GetCurrentDirectory() + "\\MahobotConfig.ini";//ini path
        private static string strCfgFileName = ""; 
        //private static string keyCode = "KururinPa";

        private static void CreateConfigFile()
        {
            try
            {
                strCfgFileName = Path.GetFileNameWithoutExtension(strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBAddress", "(数据库地址)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBServiceName", "(数据库服务名)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBUserID", "(数据库用户名)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DbPassword", "(数据库用户密码)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBPort", "(数据库监听端口)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBCreaGDTProcName", "(创建表格所用存储过程的名字)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiAddress", "(酷Q HTTP API的监听地址)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiPostAddress", "(酷Q HTTP API的端口地址)", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiForwardToAddress", "(本程序接收酷Q HTTP API传来的信息后转发的地址)", strFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("创建配置文件失败");
                Console.WriteLine("请下载配置文件手动放入运行目录");
            }
        }

        /// <summary>
        /// 将INI文件的内容写到对应textbox的方法
        /// </summary>
        public static bool LoadConfigFile()
        {
            if (File.Exists(strFilePath))
            {
                try
                {
                    strCfgFileName = Path.GetFileNameWithoutExtension(strFilePath);
                    //string[] arrayEncryp = ContentValue(strCfgFileName, "DbPassword").Split(' ');
                    //StringBuilder sbDecryp = new StringBuilder();
                    //for (int i = 0; i < arrayEncryp.Length; i++)
                    //{
                    //    sbDecryp.Append((char)(keyCode[i] ^ int.Parse(arrayEncryp[i])));
                    //}
                    //DBProperties.DBPassword = sbDecryp.ToString();
                    DBProperties.DBPassword = ContentValue(strCfgFileName, "DbPassword").ToString();
                    DBProperties.DBAddress = ContentValue(strCfgFileName, "DBAddress").ToString();
                    DBProperties.DBServiceName = ContentValue(strCfgFileName, "DBServiceName").ToString();
                    DBProperties.DBUserID = ContentValue(strCfgFileName, "DBUserID").ToString();
                    DBProperties.DBPort = ContentValue(strCfgFileName, "DBPort").ToString();
                    DBProperties.DBCreaGDTProcName = ContentValue(strCfgFileName, "DBCreaGDTProcName").ToString();
                    ApiProperties.ApiAddr = ContentValue(strCfgFileName, "ApiAddress").ToString();
                    ApiProperties.ApiPostAddr = ContentValue(strCfgFileName, "ApiPostAddress").ToString();
                    ApiProperties.ApiForwardToAddr = ContentValue(strCfgFileName, "ApiForwardToAddress").ToString();
                    return true;
                }
                catch
                {
                    Console.WriteLine("配置文件格式有误，读取失败！");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("未找到配置文件，正在创建模版……");
                CreateConfigFile();
                Console.WriteLine("创建完毕");
                return false;
            }
        }

        /// <summary>
        /// 自定义读取INI文件中的内容方法
        /// </summary>
        /// <param name="Section">键</param>
        /// <param name="key">值</param>
        /// <returns></returns>
        private static string ContentValue(string Section, string key)
        {
            StringBuilder temp = new StringBuilder(1024);
            GetPrivateProfileString(Section, key, "", temp, 1024, strFilePath);
            return temp.ToString();
        }
    }
}
