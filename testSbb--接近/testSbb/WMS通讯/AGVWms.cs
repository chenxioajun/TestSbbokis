using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testSbb
{
    public class AGVWms
    {
        public static string Url = "http://api-gw.hisense.com/cdwms/PULL/hisense_tv/hxmes/PULL?user_key=";
        /// <summary>
        ///  WMS 返空任务数据模型（与接口JSON格式完全对应）
        /// </summary>
        public class WmsRequestData
        {
            /// <summary>随机序列号（每次请求生成新的GUID）</summary>
            [JsonProperty("MESSAGE_ID")]
            public string MessageId { get; set; } = Guid.NewGuid().ToString().ToLower();

            /// <summary>数据格式（固定为JSON）</summary>
            [JsonProperty("MESSAGE_TYPE")]
            public string MessageType { get; set; } = "JSON";

            /// <summary>核心任务内容数组</summary>
            [JsonProperty("CONTENT")]
            public WmsTaskContent[] Content { get; set; }

            /// <summary>请求时间（格式：YYYY-MM-DD HH24:MI:SS）</summary>
            [JsonProperty("MESSAGE_TIME")]
            public string MessageTime { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            /// <summary>来源系统（固定为WCS）</summary>
            [JsonProperty("SOURCE_SYSTEM")]
            public string SourceSystem { get; set; } = "WCS";
        }

        /// <summary>
        /// 返空任务详情模型
        /// </summary>
        public class WmsTaskContent
        {
            [JsonProperty("FIELD2")]
            public string Field2 { get; set; } = "";

            [JsonProperty("FIELD3")]
            public string Field3 { get; set; } = "";

            /// <summary>任务类型（返空任务固定为EMPTY_BOX）</summary>
            [JsonProperty("PULL_TYPE")]
            public string PullType { get; set; } = "EMPTY_BOX";

            /// <summary>工厂编码（固定为2900）</summary>
            [JsonProperty("PLANT_CODE")]
            public string PlantCode { get; set; } = "2900";

            /// <summary>物料列表（返空任务为空数组）</summary>
            [JsonProperty("MATERIAL_LIST")]
            public object[] MaterialList { get; set; } = Array.Empty<object>();

            /// <summary>发送工位（需从上位机界面或配置中获取）</summary>
            [JsonProperty("SEND_STATION")]
            public string SendStation { get; set; }

            /// <summary>车间编码（固定为JMMZ，对应模组车间）</summary>
            [JsonProperty("AREA_CODE")]
            public string AreaCode { get; set; } = "JMMZ";

            [JsonProperty("PRODUCT_CODE")]
            public string ProductCode { get; set; } = "";

            [JsonProperty("FIELD1")]
            public string Field1 { get; set; } = "";

            /// <summary>发送点位（需从上位机界面或配置中获取）</summary>
            [JsonProperty("SEND_POINT")]
            public string SendPoint { get; set; }

            /// <summary>任务号（格式：R+YYMMDD+12位流水号，需按规则生成）</summary>
            [JsonProperty("DEMAND_CODE")]
            public string DemandCode { get; set; }

            [JsonProperty("WORK_ORDER_NO")]
            public string WorkOrderNo { get; set; } = "";

            /// <summary>产线编码（需从上位机界面或配置中获取）</summary>
            [JsonProperty("LINE_CODE")]
            public string LineCode { get; set; }
        }

        /// <summary>
        /// 海信 WMS 数据发送工具类
        /// </summary>
        public class HisenseWmsSender
        {
            // 接口基础地址（含固定user_key）
           // private const string WmsApiUrl = "http://api-gw.hisense.com/cdwms/PULL/hisense_tv/hxmes/PULL?user_key=2a6gxdhnaxndil7nide0bygz4ybytmzw";
            private static readonly string WmsApiUrl = AGVWms.Url;
            // HTTP客户端（全局复用，避免频繁创建销毁）
            private static readonly HttpClient _httpClient = new HttpClient();

            static HisenseWmsSender()
            {
                // 配置HTTP客户端超时时间（根据实际网络调整，建议5-10秒）
                _httpClient.Timeout = TimeSpan.FromSeconds(10);
                // 设置请求头：告知接口发送的是JSON格式数据
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            }

            /// <summary>
            /// 生成符合规则的任务号（R+YYMMDD+12位流水号）
            /// </summary>
            /// <param name="serialNumber">12位流水号（需从上位机数据库或配置中获取，确保唯一）</param>
            /// <returns>完整任务号</returns>
            public static string GenerateDemandCode(string serialNumber)
            {
                // 验证流水号格式（12位数字）
                if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber.Length != 12 || !long.TryParse(serialNumber, out _))
                {
                    throw new ArgumentException("流水号必须是12位数字", nameof(serialNumber));
                }
                // 拼接格式：R + 年月日（YYMMDD） + 12位流水号
                string datePart = DateTime.Now.ToString("yyMMdd");
                return $"R{datePart}{serialNumber}";
            }

            /// <summary>
            /// 发送返空任务到海信WMS
            /// </summary>
            /// <param name="sendStation">发送工位（如JM08BB01）</param>
            /// <param name="sendPoint">发送点位（如M08BB255501013）</param>
            /// <param name="lineCode">产线编码（如JM08）</param>
            /// <param name="serialNumber">12位流水号（用于生成任务号）</param>
            /// <returns>接口响应结果（成功/失败信息）</returns>/
            ///<returns>元组 (bool 成功状态, string 响应信息)</returns>
            public static async Task<(bool IsSuccess, string Message)> SendEmptyBoxTaskAsync(string sendStation, string sendPoint, string lineCode, string serialNumber)
            {
                try
                {
                    // 1. 构建请求数据（按接口格式封装）
                    var requestData = new WmsRequestData
                    {
                        Content = new[]
                        {
                        new WmsTaskContent
                        {
                            SendStation = sendStation,
                            SendPoint = sendPoint,
                            LineCode = lineCode,
                            DemandCode = GenerateDemandCode(serialNumber) // 生成任务号
                        }
                    }
                    };

                    // 2. 将对象序列化为JSON字符串（忽略null值，保持格式简洁）
                    string jsonBody = JsonConvert.SerializeObject(requestData,
                        Formatting.None,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    // 记录发送的JSON数据（描述+格式化后的JSON）
                    LogHelper.WriteJsonLog(jsonBody, $"发送到海信WMS的返空任务（请求ID：{requestData.MessageId}）");

                    // 3. 构建HTTP POST请求（接口通常用POST发送数据，若为GET需调整）
                    var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PostAsync(WmsApiUrl, httpContent);

                    // 4. 处理响应结果
                    response.EnsureSuccessStatusCode(); // 若HTTP状态码非200-299，抛出异常
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // 新增：完整打印响应内容到日志（含请求ID和响应原文）
                    LogHelper.WriteJsonLog(responseContent, $"WMS返空任务响应（请求ID：{requestData.MessageId}）");
                    if (response.IsSuccessStatusCode)
                    {
                        // ------------------- 新增：打印响应结果到日志 -------------------
                        LogHelper.WriteLog($"返空任务发送成功（请求ID：{requestData.MessageId}），WMS响应成功：{responseContent}");

                        // 5. 返回成功信息（含请求ID，便于问题排查）
                        return (true, $"任务发送成功！\n请求ID：{requestData.MessageId}\n接口响应成功：{responseContent}");
                    }
                    else
                    {  // ------------------- 新增：打印响应结果到日志 -------------------
                        LogHelper.WriteLog($"返空任务发送成功（请求ID：{requestData.MessageId}），WMS响应失败：{responseContent}");

                        // 5. 返回成功信息（含请求ID，便于问题排查）
                        return (false, $"任务发送成功！\n请求ID：{requestData.MessageId}\n接口响应失败：{responseContent}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogHelper.WriteLog($"返空任务发送失败（网络/接口错误）：{ex.Message}");
                    // 捕获HTTP相关错误（如网络异常、接口超时、状态码错误）
                    return (false, $"任务发送失败（网络/接口错误）：{ex.Message}");
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog($"返空任务发送失败（网络/接口错误）：{ex.Message}");
                    // 捕获其他错误（如数据格式错误、流水号无效）
                    return (false,$"任务发送失败（数据错误）：{ex.Message}");
                }
            }
        }
    }
}

