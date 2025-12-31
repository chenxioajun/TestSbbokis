using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32; // 新增：注册表操作需要
using System.Management; // 新增：读取硬件信息需要
using System.Security.Cryptography; // 新增：MD5加密需要
using System.Text; // 新增：字符串转字节需要

namespace testSbb.UI // 必须和AuthForm同一命名空间
{
    public partial class AuthForm : Form
    {
        // 你的AuthForm代码不变，这里省略...（和你之前的一致）
    }

    // 【替换后的完整AuthHelper类】包含所有缺失的方法
    public static class AuthHelper
    {
        #region 核心配置（可根据需求修改）
        private const string AuthSecretKey = "HmiAuth_2025_SecretKey_YourCompany123";// 授权密钥（和生成授权码的工具一致）
        private const string RegistryPath = @"Software\testSbb\DeviceAuth"; // 注册表存储路径
        private const string RegistryAuthKey = "IsAuthorized"; // 授权状态键名（1=已授权）
        private const int SerialNumberLength = 8; // 授权码长度（必须和生成工具一致）
        private const string CustomFixedStr = "SFCHENLUJUN"; // 你的自定义固定字符串
        #endregion

        // 1. 检查设备是否已授权（主程序启动时调用）
        public static bool CheckDeviceAuthorized()
        {
            try
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    // 注册表路径不存在，或键值不是1 → 未授权
                    if (regKey == null) return false;
                    object authValue = regKey.GetValue(RegistryAuthKey);
                    return authValue != null && authValue is int && (int)authValue == 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"授权状态读取失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // 2. 生成设备硬件码（用于获取授权码）
        public static string GetDeviceHardwareCode()
        {
            try
            {
                // 读取CPU序列号前6位（部分设备可能为空，用默认值兜底）
                string cpuSerial = GetHardwareInfo("Win32_Processor", "ProcessorId")?.Substring(0, 6) ?? "CPU0000";
                // 读取主板序列号前6位（部分设备可能为空，用默认值兜底）
                string boardSerial = GetHardwareInfo("Win32_BaseBoard", "SerialNumber")?.Substring(0, 6) ?? "BOARD00";
                return (cpuSerial + boardSerial).ToUpper(); // 组合成唯一硬件码
            }
            catch
            {
                // 硬件信息读取失败时，返回临时唯一码（仅测试用）
                return $"TEMP_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            }
        }

        // 3. 验证授权码（包含自定义字段+当日日期混淆，核心方法）
        public static bool VerifySerialNumber(string inputSerial)
        {
            // 第一步：基础格式校验（非空+长度正确）
            if (string.IsNullOrWhiteSpace(inputSerial) || inputSerial.Trim().Length != SerialNumberLength)
                return false;

            string deviceCode = GetDeviceHardwareCode();
            string todayDate = DateTime.Now.ToString("yyyyMMdd"); // 当日日期（格式：20251027）
            string checkSource = deviceCode + AuthSecretKey + CustomFixedStr + todayDate; // 拼接混淆字段
            string localCheckCode = GenerateMd5Hash(checkSource) // MD5加密
                .Substring(0, SerialNumberLength) // 截取指定长度
                .ToUpper(); // 转为大写
           
            return localCheckCode == inputSerial.Trim().ToUpper();
        }

        // 4. 保存授权状态到注册表（授权成功后调用，缺失的方法）
        public static void SaveAuthorizeStatus()
        {
            try
            {
                // 打开或创建注册表路径（当前用户权限，无需管理员）
                using (RegistryKey regKey = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (regKey == null)
                        throw new Exception("无法创建注册表路径，可能权限不足");

                    // 写入授权状态：1=已授权（DWord类型，持久化存储）
                    regKey.SetValue(RegistryAuthKey, 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"授权状态保存失败：{ex.Message}\n请以管理员身份运行程序", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region 内部辅助方法（私有，外部无需调用）
        // 读取硬件信息（基于WMI接口）
        private static string GetHardwareInfo(string wmiClassName, string propertyName)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClassName}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                // 忽略WMI查询异常（如设备禁用WMI、权限不足）
            }
            return "";
        }

        // 生成MD5哈希值（用于授权码加密）
        private static string GenerateMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5Hash.ComputeHash(inputBytes);

                // 将字节数组转为32位小写哈希字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
        #endregion
    }
}