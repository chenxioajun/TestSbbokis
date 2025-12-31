using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testSbb
{
    public static class loghelperPLC
    {
        // 日志文件存储路径（默认：程序运行目录下的 Log 文件夹）
        private static readonly string _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log_PLC");

        static loghelperPLC()
        {
            // 初始化：若日志目录不存在，自动创建
            if (!Directory.Exists(_logDir))
            {
                Directory.CreateDirectory(_logDir);
            }
        }

        /// <summary>
        /// 记录普通日志（如发送状态、响应结果）
        /// </summary>
        /// <param name="content">日志内容</param>
        public static void WriteLog(string content)
        {
            WriteLogToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] {content}");
        }

        /// <summary>
        /// 记录 JSON 日志（发送的 JSON 数据，自动格式化）
        /// </summary>
        /// <param name="jsonContent">原始 JSON 字符串</param>
        /// <param name="logDesc">日志描述（如“发送到WMS的返空任务JSON”）</param>
        public static void WriteJsonLog(string jsonContent, string logDesc)
        {
            try
            {
                // 格式化 JSON（带缩进，便于阅读）
                var formattedJson = JsonConvert.DeserializeObject(jsonContent);
                string prettyJson = JsonConvert.SerializeObject(formattedJson, Formatting.Indented);

                // 拼接日志内容（含描述+格式化后的JSON）
                string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [JSON] {logDesc}:\n{prettyJson}\n";
                WriteLogToFile(logContent);
            }
            catch (Exception ex)
            {
                // 若 JSON 格式化失败，直接记录原始内容
                WriteLogToFile($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [JSON_ERROR] {logDesc}（JSON格式化失败）: {jsonContent}\n错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 核心方法：将日志写入本地文件
        /// </summary>
        /// <param name="logContent">最终日志内容</param>
        private static void WriteLogToFile(string logContent)
        {
            try
            {
                // 日志文件名：按日期命名（如 2025-03-05.log）
                string logFileName = $"{DateTime.Now:yyyy-MM-dd}.log";
                string logFilePath = Path.Combine(_logDir, logFileName);

                // 追加写入日志（UTF-8编码，避免中文乱码）
                using (var streamWriter = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    streamWriter.WriteLine(logContent);
                    // 写入分隔线，便于区分不同日志条目
                    streamWriter.WriteLine("--------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                // 日志写入失败时，不影响主流程（可输出到控制台备用）
                Console.WriteLine($"日志写入失败：{ex.Message}");
            }
        }
    }
}
