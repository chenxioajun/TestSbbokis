using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace testSbb
{
    public class JosnConfigHelper // 注意：类名拼写建议修正为 JsonConfigHelper
    {
        private static readonly object fileLock = new object();
        private readonly string _filePath; // 用成员变量存储实例化时传入的文件名

        /// <summary>
        /// 构造函数：接收配置文件路径，支持动态指定
        /// </summary>
        /// <param name="filePath">配置文件路径（如 "passwords.json" 或 "configs/app.json"）</param>
        public JosnConfigHelper(string filePath)
        {
            // 校验路径有效性（可选，根据需求添加）
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "配置文件路径不能为空");

            _filePath = filePath; // 存储实例化时传入的路径
        }

        /// <summary>
        /// 确保文件存在（线程安全），使用实例化时传入的文件路径
        /// </summary>
        private void EnsureFileExists()
        {
            lock (fileLock)
            {
                if (File.Exists(_filePath)) return;

                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_filePath, "{}"); // 创建空JSON对象
            }
        }

        /// <summary>
        /// 读取配置值（使用实例化时的文件路径）
        /// </summary>
        public string ReadConfig(string key, string defaultValue = "111")
        {
            EnsureFileExists();
            var configDic = ReadAll();
            return configDic.TryGetValue(key, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// 写入或更新配置（使用实例化时的文件路径）
        /// </summary>
        public void WriteConfig(string key, string value)
        {
            lock (fileLock)
            {
                EnsureFileExists();
                var configDic = ReadAll();
                configDic[key] = value;
                SaveDictionary(configDic);
            }
        }

        /// <summary>
        /// 删除配置项（使用实例化时的文件路径）
        /// </summary>
        public void DeleteConfig(string key)
        {
            lock (fileLock)
            {
                EnsureFileExists();
                var configDic = ReadAll();

                if (configDic.Remove(key))
                {
                    SaveDictionary(configDic);
                }
            }
        }

        /// <summary>
        /// 清空所有配置（使用实例化时的文件路径）
        /// </summary>
        public void ClearConfig()
        {
            lock (fileLock)
            {
                SaveDictionary(new Dictionary<string, string>());
            }
        }

        /// <summary>
        /// 读取全部配置（使用实例化时的文件路径）
        /// </summary>
        public Dictionary<string, string> ReadAll()
        {
            EnsureFileExists();
            lock (fileLock)
            {
                string json = File.ReadAllText(_filePath);
                try
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                           ?? new Dictionary<string, string>();
                }
                catch (JsonException)
                {
                    return new Dictionary<string, string>();
                }
            }
        }

        /// <summary>
        /// 保存字典到文件（使用实例化时的文件路径）
        /// </summary>
        private void SaveDictionary(Dictionary<string, string> dict)
        {
            string json = JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
    }
}


//using Newtonsoft.Json;

//namespace testSbb
//{
//    public class JosnConfigHelper
//    {
//        private static readonly object fileLock = new object();

//        private const string DefaultFilePath = "config.json";

//        /// <summary>
//        /// 确保文件存在（线程安全）
//        /// </summary>
//        private void EnsureFileExists(string filePath = DefaultFilePath)
//        {
//            lock (fileLock)
//            {
//                if (File.Exists(filePath)) return;

//                string directory = Path.GetDirectoryName(filePath);
//                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                File.WriteAllText(filePath, "{}"); // 创建空JSON对象
//            }
//        }

//        /// <summary>
//        /// 读取配置值
//        /// </summary>
//        public string ReadConfig(string key, string defaultValue = "0", string filePath = DefaultFilePath)
//        {
//            EnsureFileExists(filePath);
//            var configDic = ReadAll(filePath);
//            string value;
//            return configDic.TryGetValue(key, out value) ? value : defaultValue;
//        }

//        /// <summary>
//        /// 写入或更新配置
//        /// </summary>
//        public void WriteConfig(string key, string value, string filePath = DefaultFilePath)
//        {
//            lock (fileLock)
//            {
//                EnsureFileExists(filePath);
//                var configDic = ReadAll(filePath);
//                configDic[key] = value;
//                SaveDictionary(configDic, filePath);
//            }
//        }

//        /// <summary>
//        /// 删除配置项
//        /// </summary>
//        public void DeleteConfig(string key, string filePath = DefaultFilePath)
//        {
//            lock (fileLock)
//            {
//                EnsureFileExists(filePath);
//                var configDic = ReadAll(filePath);

//                if (configDic.Remove(key))
//                {
//                    SaveDictionary(configDic, filePath);
//                }
//            }
//        }

//        /// <summary>
//        /// 清空所有配置
//        /// </summary>
//        public void ClearConfig(string filePath = DefaultFilePath)
//        {
//            lock (fileLock)
//            {
//                SaveDictionary(new Dictionary<string, string>(), filePath);
//            }
//        }

//        /// <summary>
//        /// 读取全部配置
//        /// </summary>
//        public Dictionary<string, string> ReadAll(string filePath = DefaultFilePath)
//        {
//            EnsureFileExists(filePath);
//            lock (fileLock)
//            {
//                string json = File.ReadAllText(filePath);
//                try
//                {
//                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
//                           ?? new Dictionary<string, string>();
//                }
//                catch (JsonException)
//                {
//                    return new Dictionary<string, string>();
//                }
//            }
//        }

//        /// <summary>
//        /// 保存字典到文件（私有方法）
//        /// </summary>
//        private void SaveDictionary(Dictionary<string, string> dict, string filePath)
//        {
//            string json = JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented);
//            File.WriteAllText(filePath, json);
//        }
//    }
//}
