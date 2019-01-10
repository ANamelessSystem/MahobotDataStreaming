﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using Marchen.Model;

namespace Marchen
{
    class FillConfig
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
        private static string pathPrefix = Directory.GetCurrentDirectory();
        private string strFilePath = pathPrefix + "\\MahobotConfig.ini";//ini path
        private string strCfgFileName = ""; 
        private string keyCode = "KururinPa";

        private void CreateConfigFile()
        {
            //Pending 能创建？
            try
            {
                strCfgFileName = Path.GetFileNameWithoutExtension(strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBAddress", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBServiceName", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBUserID", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DbPassword", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBPort", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "DBCreaGDTProcName", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiAddress", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiPostAddress", "", strFilePath);
                WritePrivateProfileString(strCfgFileName, "ApiForwardToAddress", "", strFilePath);
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
        private void LoadConfigFile()
        {
            if (File.Exists(strFilePath))
            {
                strCfgFileName = Path.GetFileNameWithoutExtension(strFilePath);
                string[] arrayEncryp = ContentValue(strCfgFileName, "DbPassword").Split(' ');
                StringBuilder sbDecryp = new StringBuilder();
                for (int i = 0; i < arrayEncryp.Length; i++)
                {
                    sbDecryp.Append((char)(keyCode[i] ^ int.Parse(arrayEncryp[i])));
                }
                DBProperties.DBPassword = sbDecryp.ToString();
                DBProperties.DBAddress = ContentValue(strCfgFileName, "DBAddress").ToString();
                DBProperties.DBServiceName = ContentValue(strCfgFileName, "DBServiceName").ToString();
                DBProperties.DBUserID = ContentValue(strCfgFileName, "DBUserID").ToString();
                DBProperties.DBPort = ContentValue(strCfgFileName, "DBPort").ToString();
                DBProperties.DBCreaGDTProcName = ContentValue(strCfgFileName, "DBCreaGDTProcName").ToString();
                ApiProperties.ApiAddr = ContentValue(strCfgFileName, "ApiAddress").ToString();
                ApiProperties.ApiPostAddr = ContentValue(strCfgFileName, "ApiPostAddress").ToString();
                ApiProperties.ApiForwardToAddr = ContentValue(strCfgFileName, "ApiForwardToAddress").ToString();
            }
            else
            {
                Console.WriteLine("未找到配置文件，正在创建模版……");
            }
        }

        /// <summary>
        /// 自定义读取INI文件中的内容方法
        /// </summary>
        /// <param name="Section">键</param>
        /// <param name="key">值</param>
        /// <returns></returns>
        private string ContentValue(string Section, string key)
        {
            StringBuilder temp = new StringBuilder(1024);
            GetPrivateProfileString(Section, key, "", temp, 1024, strFilePath);
            return temp.ToString();
        }
    }
}
