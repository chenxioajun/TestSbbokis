using HslCommunication;
using HslCommunication.Profinet.Melsec;
using static testSbb.AGVWms;
using AutoScaleHelper;
using System.Net.NetworkInformation;
using System.Net;
using System.Diagnostics;
using static System.Timers.Timer;
using System.Windows.Forms;
using testSbb.UI;
using System.Timers;

namespace testSbb
{
    public partial class Form1 : Form
    {
        private string _ip;
        private int _port;
        private MelsecMcNet _melsecMcNet; //MelsecA1EAsciiNet--ascii MelsecA1ENet--二进制   MelsecMcNet--二进制

        public ushort _heartbeat, Station1Triggered, Station2Triggered,
                      Station1Feedback, Station2Feedback, Station1Status, Station2Status;//内容显示
        private bool _isConnected = false;//连接状态

        JosnConfigHelper josnConfigHelper = new JosnConfigHelper("config.json");//配置文件

        AGVWms _AGVWms = new AGVWms();
        private (string PlcAddress, TextBox TargetTextBox)[] addressTextBoxPairs; // 定义 PLC 地址与界面控件的对应关系

        private bool _YunxuAGV = false;

        private bool _StationM = false;//用于判断写入哪个texbox
                                       // private string Url1 = null;

        private bool Moni = true;

        private static SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1); // 等效于锁的异步版本

        AutoScale autoScale = new AutoScale();//s缩放窗体
        TextScaleEx scaleEx = new TextScaleEx();
        TextScale textScale = new TextScale();

        // 异步连接PLC（核心方法，后台执行）
        private readonly object _connectLock = new object();

        // 权限枚举
        public enum UserRole
        {
            Admin,    // 管理员（所有控件可改）
            Engineer, // 工程师（部分控件可改）
            Operator  // 操作员（仅查看，不可修改）
        }
        public static UserRole CurrentRole;//枚举

        /// <summary>
        /// 获取空间的text给到变量 定义 PLC 地址与界面控件的对应关系
        /// </summary>
        private void InitializeAddressTextBoxPairs()
        {
            addressTextBoxPairs = new[]
            {
        (textBox1.Text, textBox1),
        (textBox2.Text, textBox2),
        (textBox3.Text, textBox3),
        (textBox4.Text, textBox4),
        (textBox8.Text, textBox8),
        (textBox7.Text, textBox7),
        (textBox6.Text, textBox6)
             };
        }

        public Form1()
        {
            InitializeComponent();
            JsonJiazai();
            InitializeAddressTextBoxPairs();

            // 设置窗口在屏幕中心显示
            this.StartPosition = FormStartPosition.CenterScreen;


            richTextBox2.ReadOnly = true; // 禁止编辑内容
            richTextBox1.ReadOnly = true; // 禁止编辑内容

            this.SetAnchorNone();//缩放窗口
            autoScale.AutoFont = true;
            autoScale.SetContainer(this);


            ConnectPLCAsync();//第一次连接-----------------------
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            this.SuspendLayout();
            autoScale.UpdateControlsLayout();
            this.ResumeLayout();
        }

        /// <summary>
        /// 缩放
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            button8.Enabled = false;//日志
            button9.Enabled = false;//日志
            // 订阅 TabControl 的 Selecting 事件
            //  AutoSize = new AutoAdaptWindowsSize(this);
            // 初始化时默认显示第一张图（可选）
            try
            {
                Putesty(0, 0);
                LoadImage(pictureBox3, "pcn5.png");
            }
            catch (Exception ex) { }
            tabControl1.Selecting += TabControl1_Selecting;

            if (!AuthHelper.CheckDeviceAuthorized())
            {
                using (var authForm = new AuthForm())
                {
                    if (authForm.ShowDialog() != DialogResult.OK)
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
            // 授权通过，加载主功能

        }

        // 窗体关闭释放资源
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            if (heartbeatTimer != null)
            {
                heartbeatTimer.Stop();
                heartbeatTimer.Dispose();
            }
            _melsecMcNet?.ConnectClose(); // 确保断开连接
            tabControl1.Selecting -= TabControl1_Selecting;

        }
        private void button1_Click(object sender, EventArgs e)
        {
            ConnectPLCAsync();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (isHeartbeatRunning)
            {
                isHeartbeatRunning = false;
                if (heartbeatTimer != null)
                {
                    heartbeatTimer.Stop();
                    loghelperPLC.WriteLog("PLC已手动断开连接");
                }
            }

            // 断开连接时更新状态变量
            if (_melsecMcNet != null)
            {
                _melsecMcNet.ConnectClose();
            }

            // 关键：手动断开时，同步设为false
            _isConnected = false;
            _reconnectCount = 0; // 重置重连计数器
            UpdateHeartbeatStatus(false, "PLC已断开", label28);
            UpdateHeartbeatStatus(false, "PLC已断开", label15);
        }

        private void ConnectPLCAsync()
        {
            // 加锁：防止多线程同时发起连接
            lock (_connectLock)
            {
                // 在后台线程执行连接操作（不阻塞UI）
                Task.Run(() =>
                {
                    if (Moni)
                    {
                        Jindu(1);
                        label1.BringToFront();
                        UpdateHeartbeatStatus(true, "配置参数加载中.....", label30);
                        // 启动后台线程执行任务
                        bool isSuccess = false;
                        string ipAddress = "";
                        // 将方法包装为Task
                        Task task = Task.Run(() =>
                        {
                            // 在后台线程执行获取IP的操作
                            ipAddress = GetLocalIPv4(ref isSuccess);
                        });
                        // 等待Task执行完成（会阻塞当前线程，若在UI线程调用需谨慎）
                        task.Wait();
                        if (isSuccess)
                        {
                            UpdateHeartbeatStatus(true, $"接口获取成功{ipAddress}", label30);
                            UpdateHeartbeatStatus(true, $"本机IP：{ipAddress}", label31);
                        }
                        else
                        {
                            UpdateHeartbeatStatus(true, $"接口获取失败{ipAddress}", label30);
                        }

                        Task task1 = Task.Run(() =>
                        {// 获取文本框中的IP地址
                            string targetIP = textBox14.Text.Trim();
                            Jindu(25);
                            Jindu(45);
                            UpdateHeartbeatStatus(true, "网络接口测试中.....", label30);
                            // 调用封装的Ping方法
                            string result = PingIP(targetIP);
                            Jindu(63);
                            UpdateHeartbeatStatus(true, result, label30);
                        });
                        // 等待Task执行完成（会阻塞当前线程，若在UI线程调用需谨慎）
                        task1.Wait();


                        Jindu(78);
                        Thread.Sleep(50);
                        Jindu(88);
                        UpdateHeartbeatStatus(true, "加载成功正在主动连接PLC！！！！", label30);
                        Thread.Sleep(500);
                        Jindu(95);
                        Moni = false;
                    }

                    UpdateHeartbeatStatus(true, "正在尝试连接PLC中.....", label15);
                    // 获取用户输入的IP和端口
                    try
                    {
                        // MessageBox.Show("button1_Click 已执行");
                        if (_isConnected)
                        {
                            return;
                        }
                        _ip = textBox14.Text.Trim();
                        if (int.TryParse(textBox13.Text, out int port) && _ip != null && !isHeartbeatRunning)
                        {
                            UpdateHeartbeatStatus(true, "正在尝试连接PLC中.....", label28);
                            UpdateHeartbeatStatus(true, "正在尝试连接PLC中.....", label15);
                            _port = port;

                            // 初始化连接对象
                            _melsecMcNet = new MelsecMcNet(_ip, _port);
                            _melsecMcNet.ConnectTimeOut = 5000; // 连接超时5秒
                            _melsecMcNet.ReceiveTimeOut = 3000; // 读取超时3秒
                            // 连接PLC
                            OperateResult connectResult = _melsecMcNet.ConnectServer();
                            if (connectResult.IsSuccess)
                            {
                                UpdateHeartbeatStatus(true, "", label30);
                                _isConnected = true; // 更新连接状态变量
                                UpdateHeartbeatStatus(true, "PLC已连接", label15);
                                UpdateHeartbeatStatus(true, "PLC已连接", label28);
                                loghelperPLC.WriteLog(label15.Text);
                                Jindu(100);
                                //// 启动心跳定时器（关键：添加启动逻辑）
                                //if (!isHeartbeatRunning)
                                //{
                                //    isHeartbeatRunning = true;
                                //    if (heartbeatTimer == null)
                                //    {
                                //        InitHeartbeatTimer();
                                //    }
                                //    heartbeatTimer.Start(); // 启动定时器
                                //}
                                // 关键修改：无论之前状态如何，强制重建并启动定时器
                                if (heartbeatTimer != null)
                                {
                                    heartbeatTimer.Stop();
                                    heartbeatTimer.Dispose(); // 释放旧定时器
                                }
                                isHeartbeatRunning = true; // 重置状态
                                InitHeartbeatTimer(); // 重建定时器
                                heartbeatTimer.Start(); // 启动新定时器
                                loghelperPLC.WriteLog("心跳定时器已启动"); // 新增日志：确认定时器启动

                            }
                            else
                            {
                                _isConnected = false; UpdateHeartbeatStatus(true, "", label30);
                                UpdateHeartbeatStatus(false, "PLC连接失败", label15);
                                UpdateHeartbeatStatus(false, "PLC连接失败", label28);
                                loghelperPLC.WriteLog(("尝试连接PLC失败：" + connectResult.Message));
                                Jindu(100);
                            }
                        }

                        else
                        {
                            _isConnected = false;
                            UpdateHeartbeatStatus(false, "PLC连接失败", label15);
                            UpdateHeartbeatStatus(false, "PLC连接失败", label28);
                            UpdateHeartbeatStatus(true, "", label30);
                            loghelperPLC.WriteLog("请输入有效的IP或者端口号");
                            ShowMessageBox($"请输入有效的IP或者端口号", "连接失败", MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        Jindu(100);
                        // 输出详细异常信息
                        _isConnected = false;
                        UpdateHeartbeatStatus(false, "PLC连接失败", label15);
                        UpdateHeartbeatStatus(false, "PLC连接失败", label28);
                        UpdateHeartbeatStatus(true, "", label30);
                        ShowMessageBox($"异常：{ex.Message}\n堆栈：{ex.StackTrace}", "连接失败", MessageBoxIcon.Warning);
                        //ShowMessageBox("工位、点位、产线、流水号不能为空！", "输入错误", MessageBoxIcon.Warning);
                    }
                });
            }
        }

        // 定时器和心跳状态
        private System.Timers.Timer heartbeatTimer;
        private bool isHeartbeatRunning = false;
        private int heartbeatInterval = 3500; // 3.5秒一次



        private void button3_Click(object sender, EventArgs e)
        {
            InitializeAddressTextBoxPairs();
            JsonCing();
            MessageBox.Show("配置保存成功");
            loghelperPLC.WriteLog("配置保存成功");
            JsonJiazai();
        }

        // 初始化心跳定时器
        /// <summary>
        /// 关联定时器
        /// </summary>
        private void InitHeartbeatTimer()
        {
            heartbeatTimer = new System.Timers.Timer(heartbeatInterval);
            heartbeatTimer.AutoReset = true; // 循环触发
            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
        }

        // 定时器事件（读取PLC数据）
        /// <summary>
        /// 连接开启定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeartbeatTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            heartbeatTimer.Stop(); // 防止重入
            try
            {
                // 用状态变量判断连接
                //if (_melsecMcNet != null && _isConnected)
                //{
                //    ReadPlcAddressesAndDisplay();
                //}
                //else
                //{
                //    UpdateHeartbeatStatus(false, "PLC已断开", label25);
                //}

                // 1. 先检测PLC连接状态（而非仅依赖_isConnected变量）
                if (_melsecMcNet != null && _isConnected)
                {
                    // 2. 尝试读取PLC（若读取失败，进入catch触发重连）
                    ReadPlcAddressesAndDisplay();
                    UpdateHeartbeatStatus(true, "心跳正常", label28); // 正常时更新心跳状态
                }
                else
                {
                    UpdateHeartbeatStatus(false, "连接已断开，尝试重连", label28);
                    TriggerAutoReconnect(); // 触发自动重连
                }
            }
            catch (Exception ex)
            {
                // 关键：记录定时器异常，避免吞掉错误
                loghelperPLC.WriteLog($"心跳定时器异常：{ex.Message}（堆栈：{ex.StackTrace}）");
                UpdateHeartbeatStatus(false, $"定时器异常", label28);
                _isConnected = false; // 异常时标记连接失效
               // UpdateHeartbeatStatus(false, $"心跳异常：{ex.Message}", label25);
                UpdateHeartbeatStatus(false, $"心跳异常：{ex.Message}", label28);
                TriggerAutoReconnect(); // 读取抛异常时，也触发重连
            }
            finally
            {
                // 确保定时器重启（无论是否异常，只要未手动停止）

                if (isHeartbeatRunning && heartbeatTimer != null)
                {
                    heartbeatTimer.Start();
                    loghelperPLC.WriteLog("心跳定时器重启"); // 记录定时器重启
                }
            }
        }


        // 新增：自动重连方法（带重试次数限制，避免无限重试）
        private int _reconnectCount = 0; // 重连计数器
        private const int MaxReconnectCount = 5; // 最大重试次数（超过后提示）
        private void TriggerAutoReconnect()
        {
            if (!_isConnected && _reconnectCount < MaxReconnectCount)
            {
                _reconnectCount++;
                UpdateHeartbeatStatus(false, $"自动重连（{_reconnectCount}/{MaxReconnectCount}）", label15);

                // 调用现有连接逻辑（后台执行，不阻塞定时器）
                Task.Run(() =>
                {
                    OperateResult reconnectResult = _melsecMcNet?.ConnectServer() ?? new OperateResult("PLC客户端未初始化");
                    if (reconnectResult.IsSuccess)
                    {
                        _isConnected = true;
                        _reconnectCount = 0; // 重连成功，重置计数器
                        UpdateHeartbeatStatus(true, "PLC自动重连成功", label15);
                        UpdateHeartbeatStatus(true, "PLC自动重连成功", label28);
                        loghelperPLC.WriteLog("PLC自动重连成功");
                    }
                    else
                    {
                        UpdateHeartbeatStatus(false, $"重连失败：{reconnectResult.Message}", label15);
                        if (_reconnectCount >= MaxReconnectCount)
                        {
                            UpdateHeartbeatStatus(false, "达到最大重连次数，请手动检查", label15);
                            _reconnectCount = 0; // 重置计数器，避免后续无法重试
                        }
                    }
                });
            }
        }
        //    // 按钮点击：发送返空任务
        /// <summary>
        /// 手动测试发送POST 格式为json 请求agv1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSendTask_Click(object sender, EventArgs e)
        {
            // 先尝试获取锁，如果已被占用（有任务在执行），则直接返回（防止重复点击）
            if (!_sendLock.Wait(0)) // 非阻塞等待：0毫秒超时，立即返回是否获取成功
            {
                // MessageBox.Show("任务正在处理中，请稍后再试！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogHelper.WriteLog("任务正在处理中，请稍后再试！");
                return;
            }

            if (_YunxuAGV)
            {
                MessageBox.Show("当前为禁止写入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _sendLock.Release();
                return;
            }
            // 1. 从上位机界面获取输入
            string sendStation = textBox11.Text.Trim(); // 工位输入框
            string sendPoint = textBox9.Text.Trim();     // 点位输入框
            string lineCode = textBox10.Text.Trim();       // 产线输入框
            string suiji = Suijishu();
            string serialNumber = textBox15.Text.Trim() + suiji;   // 12位流水号输入框
                                                                   // 2. 验证输入（避免空值导致发送失败）
            if (string.IsNullOrWhiteSpace(sendStation) ||
                string.IsNullOrWhiteSpace(sendPoint) ||
                string.IsNullOrWhiteSpace(lineCode) ||
                string.IsNullOrWhiteSpace(serialNumber))
            {
                MessageBox.Show("工位、点位、产线、流水号不能为空！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogHelper.WriteLog("工位、点位、产线、流水号不能为空！");
                _sendLock.Release();
                return;
            }

            // 3. 禁用按钮，防止重复发送
            _StationM = true;
            textBox15.Enabled = false;
            button4.Enabled = false;
            button4.Text = "正在发送任务...";
            UpdateLogTextBox(_StationM ? richTextBox1 : richTextBox2, "手动触发了PORT1\n", 3);
            try
            {
                // 4. 调用发送方法（异步执行，避免阻塞界面）
                var (isSuccess, resultMsg) = await HisenseWmsSender.SendEmptyBoxTaskAsync(
                    sendStation: sendStation,
                    sendPoint: sendPoint,
                    lineCode: lineCode,
                    serialNumber: serialNumber);

                // 5. 显示结果
                button4.Text = "发送完成";
                LogHelper.WriteLog($"发送完成 {resultMsg.Contains("成功")}");
                MessageBox.Show(resultMsg, "发送结果", MessageBoxButtons.OK,
                    resultMsg.Contains("成功") ? MessageBoxIcon.Information : MessageBoxIcon.Error);//
                UpdateLogTextBox(_StationM ? richTextBox1 : richTextBox2, resultMsg, isSuccess ? 3 : 2);

            }
            finally
            {
                // 6. 恢复按钮状态
                textBox15.Enabled = true;
                button4.Enabled = true;
                //textBox15.Text = "123456789113";
                button4.Text = "TestPost1";
                // 释放锁，允许下次点击
                _sendLock.Release();
            }
        }
        /// <summary>
        /// 点位2手动触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button6_Click(object sender, EventArgs e)
        {
            // 先尝试获取锁，如果已被占用（有任务在执行），则直接返回（防止重复点击）
            if (!_sendLock.Wait(0)) // 非阻塞等待：0毫秒超时，立即返回是否获取成功
            {
                // MessageBox.Show("任务正在处理中，请稍后再试！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogHelper.WriteLog("任务正在处理中，请稍后再试！");
                return;
            }

            if (_YunxuAGV)
            {
                MessageBox.Show("当前为禁止写入！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _sendLock.Release();
                return;
            }
            // 1. 从上位机界面获取输入
            string sendStation1 = textBox11.Text.Trim(); // 工位输入框
            string sendPoint1 = textBox5.Text.Trim();     // 点位输入框
            string lineCode1 = textBox10.Text.Trim();       // 产线输入框
            string suiji = Suijishu();
            string serialNumber1 = textBox15.Text.Trim() + suiji;   // 12位流水号输入框
                                                                    // 2. 验证输入（避免空值导致发送失败）
            if (string.IsNullOrWhiteSpace(sendStation1) ||
                string.IsNullOrWhiteSpace(sendPoint1) ||
                string.IsNullOrWhiteSpace(lineCode1) ||
                string.IsNullOrWhiteSpace(serialNumber1))
            {
                MessageBox.Show("工位、点位、产线、流水号不能为空！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogHelper.WriteLog("工位、点位、产线、流水号不能为空！");
                _sendLock.Release();
                return;
            }

            // 3. 禁用按钮，防止重复发送
            _StationM = false;
            textBox15.Enabled = false;
            button6.Enabled = false;
            //textBox15.Text = "正在发送任务...";
            button6.Text = "正在发送任务...";
            UpdateLogTextBox(_StationM ? richTextBox1 : richTextBox2, "手动触发了PORT2", 3);
            try
            {
                // 4. 调用发送方法（异步执行，避免阻塞界面）
                var (isSuccess, resultMsg) = await HisenseWmsSender.SendEmptyBoxTaskAsync(
                    sendStation: sendStation1,
                    sendPoint: sendPoint1,
                    lineCode: lineCode1,
                    serialNumber: serialNumber1);

                // 5. 显示结果
                button6.Text = "发送完成";
                LogHelper.WriteLog($"发送完成 {resultMsg.Contains("成功")}");
                MessageBox.Show(resultMsg, "发送结果", MessageBoxButtons.OK,
                    resultMsg.Contains("成功") ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                UpdateLogTextBox(_StationM ? richTextBox1 : richTextBox2, resultMsg, isSuccess ? 3 : 2);

            }
            finally
            {
                // 6. 恢复按钮状态
                textBox15.Enabled = true;
                button6.Enabled = true;
                //textBox15.Text = "123456789113";
                button6.Text = "TestPost2";
                // 释放锁，允许下次点击
                _sendLock.Release();
            }
        }

        private async Task SendTaskAsync(string sendStation, string sendPoint, string lineCode, string serialNumber)
        {
            bool lockAcquired = false;
            try
            {
                // 尝试获取锁
                if (!_sendLock.Wait(0))
                {
                    LogHelper.WriteLog("任务正在处理中，请稍后再试！");
                    return;
                }
                lockAcquired = true;

                // 输入验证
                if (string.IsNullOrWhiteSpace(sendStation) ||
                    string.IsNullOrWhiteSpace(sendPoint) ||
                    string.IsNullOrWhiteSpace(lineCode) ||
                    string.IsNullOrWhiteSpace(serialNumber))
                {
                    ShowMessageBox("工位、点位、产线、流水号不能为空！", "输入错误", MessageBoxIcon.Warning);
                    LogHelper.WriteLog("工位、点位、产线、流水号不能为空！");
                    return;
                }

                // 调用发送方法
                var (isSuccess, resultMsg) = await HisenseWmsSender.SendEmptyBoxTaskAsync(
                    sendStation: sendStation,
                    sendPoint: sendPoint,
                    lineCode: lineCode,
                    serialNumber: serialNumber
                );

                // 发送成功时写入PLC
                if (isSuccess)
                {
                    WriteToPlc(_StationM ? textBox3.Text : textBox7.Text, 1);
                }
                else
                {
                    WriteToPlc(_StationM ? textBox3.Text : textBox7.Text, 2);
                }

                // 记录发送结果日志
                LogHelper.WriteLog($"WMS任务发送{(isSuccess ? "成功" : "失败")}：{resultMsg}");
                // 更新日志文本框
                UpdateLogTextBox(_StationM ? richTextBox1 : richTextBox2, resultMsg, isSuccess ? 3 : 2);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog($"发送任务过程中发生异常：{ex.Message}");
            }
            finally
            {
                if (lockAcquired)
                {
                    _sendLock.Release();
                }
            }
        }

        // PLC写入辅助方法
        private void WriteToPlc(string address, short Data1)
        {
            try
            {
                if (_melsecMcNet == null)
                {
                    LogHelper.WriteLog("PLC未连接，无法执行写入操作");
                    return;
                }
                byte[] data = _melsecMcNet.ByteTransform.TransByte(Data1);
                OperateResult writeResult = _melsecMcNet.Write(address, data);
                if (!writeResult.IsSuccess)
                {
                    LogHelper.WriteLog($"PLC写入失败（地址：{address}）：{writeResult.Message}");
                }
                else
                {
                    //  LogHelper.WriteLog($"PLC写入成功（地址：{address}）");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog($"PLC写入异常（地址：{address}）：{ex.Message}");
            }
        }

        /// <summary>
        /// 线程安全的弹窗方法（适配WinForm，避免跨线程异常）
        /// </summary>
        /// <param name="text">弹窗内容</param>
        /// <param name="caption">弹窗标题</param>
        /// <param name="icon">弹窗图标（如警告、错误）</param>
        private void ShowMessageBox(string text, string caption, MessageBoxIcon icon)
        {
            // 判断当前线程是否为UI线程
            if (this.InvokeRequired)
            {
                // 非UI线程：通过Invoke切换到UI线程执行
                this.Invoke(new Action<string, string, MessageBoxIcon>(ShowMessageBox), text, caption, icon);
                return;
            }

            // UI线程：直接显示弹窗
            MessageBox.Show(this, text, caption, MessageBoxButtons.OK, icon);
        }

        // 更新工位数据到UI（线程安全）
        /// <summary>
        /// 更新读取到的数据到ui层
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        private void UpdateStationValue(int index, ushort value)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, ushort>(UpdateStationValue), index, value);
                return;
            }
            // 注意：index=0对应D7400，index=1对应D7401，以此类推
            switch (index)
            {
                case 0:
                    _heartbeat = value;
                    label14.Text = _heartbeat.ToString();
                    break;
                case 1:
                    Station1Triggered = value;
                    label16.Text = Station1Triggered.ToString();
                    break;
                case 2:
                    Station1Feedback = value;
                    label18.Text = Station1Feedback.ToString();
                    break;
                case 3:
                    Station1Status = value; // 
                    label17.Text = Station1Status.ToString();
                    Putesty(Station1Status, Station2Status);//---------------------------------------显示图片
                    break;
                case 4:
                    Station2Triggered = value;
                    label20.Text = Station2Triggered.ToString();
                    break;
                case 5:
                    Station2Feedback = value;
                    label19.Text = Station2Feedback.ToString();
                    break;
                case 6:
                    Station2Status = value; // 
                    label22.Text = Station2Status.ToString();
                    Putesty(Station1Status, Station2Status);//---------------------------------------显示图片
                    break;
                    // 可添加其他寄存器的处理
            }
        }

        private void Jindu(int jindu)
        {
            this.Invoke(new Action(() =>
            {
                progressBar1.Value = jindu;
                // 其他需要操作 UI 控件的代码也放在这里
            }));
        }

        /// <summary>
        /// 触发wms呼叫agv
        /// </summary>
        private async void Triggered(int Value, int Chufa)
        {
            int va = Value, chufa = Chufa;
            string suiji = Suijishu();
            string sendStation = textBox11.Text.Trim(); // 工位输入框
            string sendPoint;
            string lineCode = textBox10.Text.Trim();       // 产线输入框
            string serialNumber = textBox15.Text.Trim() + suiji;   // 12位流水号输入框
            if (!_YunxuAGV)
            {
                if (va == 1 && chufa == 1)
                {
                    _StationM = true;
                    sendPoint = textBox9.Text.Trim();     // 点位输入框
                    UpdateLogTextBox(richTextBox1, $"{sendPoint} 请求了AGV\n", 1);
                    // UpdateLogTextBox(richTextBox1, "工位1请求agv下托板", 1);
                    // 调用 SendTaskAsync 异步执行任务
                    await SendTaskAsync(sendStation, sendPoint, lineCode, serialNumber);
                }
                if (va == 4 && chufa == 1)
                {
                    _StationM = false;
                    sendPoint = textBox5.Text.Trim();     // 点位输入框
                    UpdateLogTextBox(richTextBox2, $"{sendPoint} 请求了AGV\n", 1);
                    // UpdateLogTextBox(richTextBox2, "工位2请求agv下托板", 1);
                    // 调用 SendTaskAsync 异步执行任务
                    await SendTaskAsync(sendStation, sendPoint, lineCode, serialNumber);
                }
            }
        }
        // 新增：UI线程安全更新日志文本框
        private void UpdateLogTextBox(RichTextBox textBox, string result, int colorType)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action<RichTextBox, string, int>(UpdateLogTextBox), textBox, result, colorType);
                return;
            }
            // 1. 确定日志颜色
            Color textColor = colorType switch
            {
                3 => Color.Green,
                2 => Color.Red,
                _ => Color.Black
            };
            // 2. 构建带时间戳的日志内容（包含换行）
            string logContent = $"[{DateTime.Now:HH:mm:ss}] {result}{Environment.NewLine}";
            int newContentLength = logContent.Length;

            // 3. 在现有文本前插入新日志（保留原有样式）
            // 先选中开头位置（0长度选中，用于插入）
            textBox.Select(0, 0);
            // 替换选中区域（0长度即插入）为新日志
            textBox.SelectedText = logContent;

            // 4. 选中刚刚插入的新内容（从0到新内容长度）
            textBox.Select(0, newContentLength);
            textBox.SelectionColor = textColor;

            // 5. 取消选中
            textBox.DeselectAll();
        }
        // 更新心跳状态UI（线程安全）
        private void UpdateHeartbeatStatus(bool isNormal, string message, Label lab)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool, string, Label>(UpdateHeartbeatStatus), isNormal, message, lab);
                return;
            }
            lab.Text = message;
            lab.BackColor = isNormal ? Color.Green : Color.Red;
        }
        /// <summary>
        /// 主要代码 读取plc数据
        /// </summary>
        private void ReadPlcAddressesAndDisplay()
        {
            int index = 0; // 计数器，从0开始（或1开始）
            foreach (var pair in addressTextBoxPairs)
            { // 跳过空地址或空控件
                try
                {
                    if (string.IsNullOrEmpty(pair.PlcAddress) || pair.TargetTextBox == null)
                        continue;
                    // 读取 PLC 地址的值
                    OperateResult<byte[]> readResult = _melsecMcNet.Read(pair.PlcAddress, 1);

                    if (readResult.IsSuccess && readResult.Content.Length >= 2)
                    {
                        try
                        {   // 小端字节序解析（低位字节在前，高位字节在后）
                            byte lowByte = readResult.Content[0];  // 低位字节
                            byte highByte = readResult.Content[1]; // 高位字节
                            ushort value = (ushort)((highByte << 8) | lowByte);
                            // 线程安全更新UI（避免跨线程异常）
                            if (index == 0 && !string.IsNullOrEmpty(textBox1.Text))
                            {
                                //     int writeValue = Convert.ToInt32(0); // 要写入的值
                                if (value == 1)
                                {
                                    WriteToPlc(textBox1.Text, 0);
                                    UpdateStationValue(index, 0);
                                    UpdateHeartbeatStatus(false, "  ", label21);
                                    UpdateHeartbeatStatus(false, "  ", label33);
                                    Thread.Sleep(2000);
                                }
                            }
                            if (index != 0)
                            {
                                UpdateStationValue(index, value);
                                UpdateStationValue(0, 1);
                                UpdateHeartbeatStatus(true, "  ", label21);
                                UpdateHeartbeatStatus(true, " ", label33);
                                Triggered(index, value);//呼叫agv
                            }
                        }
                        finally
                        {
                            // 显式释放读取结果的资源（针对大数组，避免内存泄漏）
                            Array.Clear(readResult.Content, 0, readResult.Content.Length);
                        }
                    }
                    else
                    {
                        // 读取失败（地址错误、PLC无响应等）
                        string errorMsg = readResult.IsSuccess ? "返回数据长度不足" : readResult.Message;
                        loghelperPLC.WriteLog($"地址{pair.PlcAddress}读取失败：{errorMsg}");
                        UpdateStationValue(index, 0);
                        _isConnected = false; // 标记连接异常
                        UpdateStationValue(index, 0);
                    }
                }
                catch (Exception ex)
                {
                    loghelperPLC.WriteLog($"地址{pair.PlcAddress}读取抛异常：{ex.Message}（堆栈：{ex.StackTrace}）");
                    UpdateStationValue(index, 0);
                    _isConnected = false; // 标记连接异常
                    UpdateStationValue(index, 0);
                }
                index++; // 每次循环结束后计数器+1
            }
        }



        /// <summary>
        /// 主页图片显示
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        // 辅助方法：获取图片路径（避免重复写路径）
        private string GetImagePath(string fileName)
        {
            // 拼接：程序运行目录 → Images文件夹 → 图片名
            return Path.Combine(Application.StartupPath, "Images", fileName);
        }



        private void Putesty(int I, int m)
        {
            // 工位1图片
            switch (I)
            {
                case 0: LoadImage(pictureBox1, "pcn1.png"); break;
                case 1: LoadImage(pictureBox1, "pcn2.png"); break;
                case 2: LoadImage(pictureBox1, "pcn3.png"); break;
            }
            // 工位2图片
            switch (m)
            {
                case 0: LoadImage(pictureBox2, "pcn1.png"); break;
                case 1: LoadImage(pictureBox2, "pcn2.png"); break;
                case 2: LoadImage(pictureBox2, "pcn3.png"); break;
            }
        }
        void LoadImage(PictureBox pb, string fileName)
        {
            string imagePath = GetImagePath(fileName);
            if (!File.Exists(imagePath))
            {
                MessageBox.Show($"图片文件不存在：{imagePath}", "错误");
                return;
            }
            // 先释放旧图片资源，防止内存泄漏
            pb.Image?.Dispose();
            // 使用 FileStream 读取（不锁定文件，后续可删除/替换图片）
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                pb.Image = Image.FromStream(fs);
            }
        }
        /// <summary>
        /// 弹出窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl1_Selecting(object? sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tabPage2)  // 如果试图切换到第二个TabPage
                                        // 弹出密码输入框对话框
            {
                using (Form2 passwordDialog = new Form2())
                {
                    DialogResult result = passwordDialog.ShowDialog();
                    if (result == DialogResult.OK && passwordDialog.IsPasswordCorrect)
                    {  // 显示登录窗口，等待用户操作

                        // 登录成功，获取传递的权限数值（1、2、3）
                        int roleValue = passwordDialog.RoleValue;

                        // 根据数值执行不同逻辑（示例）
                        switch (roleValue)
                        {
                            case 1:
                                UpdateHeartbeatStatus(true, "admin", label32);
                                SetControlPermissions(UserRole.Admin);//更新画面权限
                                                                      // 执行 admin 权限的操作
                                break;
                            case 2:
                                UpdateHeartbeatStatus(true, "工程师", label32);
                                SetControlPermissions(UserRole.Engineer);
                                // 执行工程师权限的操作
                                break;
                            case 3:
                                UpdateHeartbeatStatus(true, "操作员", label32);
                                SetControlPermissions(UserRole.Operator);
                                // 执行操作员权限的操作
                                break;
                        }
                        // 如果密码正确，允许切换 TabPage
                        return;
                    }
                    else
                    {
                        // 如果密码不正确，取消切换
                        e.Cancel = true;
                    }
                }
            }
            if (e.TabPage == tabPage3)  // 如果试图切换到第3个TabPage
                                        // 弹出密码输入框对话框
            {
                using (Form3 passwordDialog1 = new Form3())
                {
                    DialogResult result1 = passwordDialog1.ShowDialog();
                    if (result1 == DialogResult.OK && passwordDialog1.IsPasswordCorrect)
                    {
                        // 如果密码正确，允许切换 TabPage
                        return;
                    }
                    else
                    {
                        // 如果密码不正确，取消切换
                        e.Cancel = true;
                    }
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Rizi("Log_PLC", "Documents", false);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Rizi("Log_PLC", "Log_PLC", true);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Rizi("Log_AGV", "Log_AGV", true);
        }
        private void Rizi(string Log, string Pef, bool ORpdf)
        {
            string logFilePath;
            string Ri = DateTime.Now.ToString("yyyy-MM-dd");
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (ORpdf)
            {
                logFilePath = Path.Combine(baseDirectory, $"{Log}", $"{Ri}.log");
            }
            else
            {
                logFilePath = Path.Combine(baseDirectory, $"{Pef}", "说明书.pdf");
            }
            try
            {
                // 调用系统默认程序打开PDF文件
                Process.Start(new ProcessStartInfo(logFilePath)
                {
                    UseShellExecute = true // 关键：使用系统外壳程序打开文件（适配默认关联程序）
                });
            }
            catch (Exception ex) { MessageBox.Show($"文件打开失败{ex.Message}", "提示", MessageBoxButtons.OK); }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            if (_YunxuAGV)
            {
                _YunxuAGV = false;
                button5.Text = "允许呼叫";
                button5.BackColor = Color.Green;
            }
            else
            {
                _YunxuAGV = true;
                button5.Text = "禁止呼叫";
                button5.BackColor = Color.Red;
            }
        }

        /// <summary>
        /// 生成4个随机数
        /// </summary>
        /// <returns></returns>
        private string Suijishu()
        {
            string Suijishu = null;
            Random random = new Random();
            int randomInt1 = random.Next(1, 10);  // 生成 1 到 9 之间的随机整数
            int randomInt2 = random.Next(1, 10);  // 生成 1 到 9 之间的随机整数
            int randomInt3 = random.Next(1, 10);  // 生成 1 到 9 之间的随机整数
            int randomInt4 = random.Next(1, 10);  // 生成 1 到 9 之间的随机整数
            Suijishu = randomInt1.ToString() + randomInt2.ToString() + randomInt3.ToString() + randomInt4.ToString();
            return Suijishu;
        }
        //配置文件有关
        /// <summary>
        /// 写入配置文件
        /// </summary>
        private void JsonCing()
        {
            josnConfigHelper.WriteConfig(textBox1.Name, textBox1.Text);
            josnConfigHelper.WriteConfig(textBox2.Name, textBox2.Text);
            josnConfigHelper.WriteConfig(textBox3.Name, textBox3.Text);
            josnConfigHelper.WriteConfig(textBox4.Name, textBox4.Text);
            josnConfigHelper.WriteConfig(textBox5.Name, textBox5.Text);
            josnConfigHelper.WriteConfig(textBox6.Name, textBox6.Text);
            josnConfigHelper.WriteConfig(textBox7.Name, textBox7.Text);
            josnConfigHelper.WriteConfig(textBox8.Name, textBox8.Text);
            josnConfigHelper.WriteConfig(textBox9.Name, textBox9.Text);
            josnConfigHelper.WriteConfig(textBox10.Name, textBox10.Text);
            josnConfigHelper.WriteConfig(textBox11.Name, textBox11.Text);
            josnConfigHelper.WriteConfig(textBox12.Name, textBox12.Text);
            josnConfigHelper.WriteConfig(textBox13.Name, textBox13.Text);
            josnConfigHelper.WriteConfig(textBox14.Name, textBox14.Text);
            josnConfigHelper.WriteConfig(textBox15.Name, textBox15.Text);
        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void JsonJiazai()
        {
            textBox1.Text = josnConfigHelper.ReadConfig(textBox1.Name);
            textBox2.Text = josnConfigHelper.ReadConfig(textBox2.Name);
            textBox3.Text = josnConfigHelper.ReadConfig(textBox3.Name);
            textBox4.Text = josnConfigHelper.ReadConfig(textBox4.Name);
            textBox5.Text = josnConfigHelper.ReadConfig(textBox5.Name);
            textBox6.Text = josnConfigHelper.ReadConfig(textBox6.Name);
            textBox7.Text = josnConfigHelper.ReadConfig(textBox7.Name);
            textBox8.Text = josnConfigHelper.ReadConfig(textBox8.Name);
            textBox9.Text = josnConfigHelper.ReadConfig(textBox9.Name);
            textBox10.Text = josnConfigHelper.ReadConfig(textBox10.Name);
            textBox11.Text = josnConfigHelper.ReadConfig(textBox11.Name);
            textBox12.Text = josnConfigHelper.ReadConfig(textBox12.Name);
            textBox13.Text = josnConfigHelper.ReadConfig(textBox13.Name);
            textBox14.Text = josnConfigHelper.ReadConfig(textBox14.Name);
            textBox15.Text = josnConfigHelper.ReadConfig(textBox15.Name);
            label27.Text = $"工位2 ({textBox5.Text})";
            label24.Text = $"工位1 ({textBox9.Text})";
            AGVWms.Url = josnConfigHelper.ReadConfig(textBox12.Name);
        }
        /// <summary>
        /// 获取电脑ip
        /// </summary>
        /// <param name="isSuccess">是否读取成功</param>
        /// <returns></returns>
        private string GetLocalIPv4(ref bool isSuccess)
        {
            isSuccess = false; // 默认为失败
            string localIP = "";
            try
            {
                // 获取所有网络接口
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    // 过滤：仅处理已启用且非环回的接口（排除虚拟网卡等无用接口）
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        // 获取接口的IP属性
                        IPInterfaceProperties properties = ni.GetIPProperties();
                        foreach (UnicastIPAddressInformation ipInfo in properties.UnicastAddresses)
                        {
                            // 过滤：仅IPv4，且非回环地址（127.0.0.1）
                            if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                && !IPAddress.IsLoopback(ipInfo.Address))
                            {
                                localIP = ipInfo.Address.ToString();
                                isSuccess = true; // 成功获取
                                return localIP; // 返回第一个找到的有效IP
                            }
                        }
                    }
                }

                // 若未找到有效IP，返回空
                return localIP;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        /// <summary>
        /// PINGIP
        /// </summary>
        /// <param name="targetIP"></param>
        /// <returns></returns>
        private string PingIP(string targetIP)
        {
            // 验证IP地址是否为空
            if (string.IsNullOrWhiteSpace(targetIP))
            {
                return "错误：请输入目标IP地址";
            }
            try
            {
                using (Ping pingSender = new Ping())
                {
                    // 发送Ping请求（可设置超时时间，如3000毫秒）
                    PingReply reply = pingSender.Send(targetIP, 3000);

                    if (reply.Status == IPStatus.Success)
                    {
                        // 成功：返回详细信息（IP、往返时间、TTL）
                        return $"IP地址 {targetIP} 可访问。\n" +
                               $"往返时间: {reply.RoundtripTime} 毫秒\n" +
                               $"TTL: {reply.Options.Ttl}";
                    }
                    else
                    {
                        // 失败：返回状态信息
                        return $"IP地址 {targetIP} 无法访问。\n状态: {reply.Status}";
                    }
                }
            }
            catch (Exception ex)
            {
                // 捕获异常（如IP格式错误、网络不可用等）
                return $"Ping操作出错: {ex.Message}";
            }
        }

        private void SetControlPermissions(Enum @enum)
        {
            switch (@enum)
            {
                case UserRole.Admin:
                    // 管理员：所有控件可编辑
                    textBox1.Enabled = true;//心跳
                    textBox2.Enabled = true;//d7401
                    textBox3.Enabled = true;//d7402
                    textBox4.Enabled = true;//d7403
                    textBox5.Enabled = true;//点位2
                    textBox6.Enabled = true;//2状态
                    textBox7.Enabled = true;//2反馈
                    textBox8.Enabled = true;//2触发
                    textBox9.Enabled = true;//点位1
                    textBox10.Enabled = true;//产线
                    textBox11.Enabled = true;//工位
                    textBox12.Enabled = true;//URL
                    textBox13.Enabled = true;//端口号
                    textBox14.Enabled = true;//IP
                    textBox15.Enabled = true;//流水号

                    button1.Enabled = true;//连接
                    button2.Enabled = true;//断开
                    button3.Enabled = true;//保存配置
                    button4.Enabled = true;//test1
                    button5.Enabled = true;//允许test
                    button6.Enabled = true;//test2
                    button7.Enabled = true;//说明书
                    button8.Enabled = true;//日志
                    button9.Enabled = true;//日志
                    break;

                case UserRole.Engineer:
                    // 管理员：所有控件可编辑
                    textBox1.Enabled = false;//心跳
                    textBox2.Enabled = false;//d7401
                    textBox3.Enabled = false;//d7402
                    textBox4.Enabled = false;//d7403
                    textBox5.Enabled = true;//点位2
                    textBox6.Enabled = false;//2状态
                    textBox7.Enabled = false;//2反馈
                    textBox8.Enabled = false;//2触发
                    textBox9.Enabled = false;//点位1
                    textBox10.Enabled = true;//产线
                    textBox11.Enabled = true;//工位
                    textBox12.Enabled = true;//URL
                    textBox13.Enabled = true;//端口号
                    textBox14.Enabled = true;//IP
                    textBox15.Enabled = true;//流水号

                    button1.Enabled = true;//连接
                    button2.Enabled = true;//断开
                    button3.Enabled = true;//保存配置
                    button4.Enabled = false;//test1
                    button5.Enabled = false;//允许test
                    button6.Enabled = false;//test2
                    button7.Enabled = true;//说明书
                    button8.Enabled = true;//日志
                    button9.Enabled = true;//日志
                    break;

                case UserRole.Operator:
                    // 操作员：所有控件只读（不可修改）
                    textBox1.Enabled = false;//心跳
                    textBox2.Enabled = false;//d7401
                    textBox3.Enabled = false;//d7402
                    textBox4.Enabled = false;//d7403
                    textBox5.Enabled = false;//点位2
                    textBox6.Enabled = false;//2状态
                    textBox6.Enabled = false;//2状态
                    textBox7.Enabled = false;//2反馈
                    textBox8.Enabled = false;//2触发
                    textBox9.Enabled = true;//点位1
                    textBox10.Enabled = false;//产线
                    textBox11.Enabled = false;//工位
                    textBox12.Enabled = false;//URL
                    textBox13.Enabled = false;//端口号
                    textBox14.Enabled = false;//IP
                    textBox15.Enabled = false;//流水号

                    button1.Enabled = false;//连接
                    button2.Enabled = false;//断开
                    button3.Enabled = false;//保存配置
                    button4.Enabled = false;//test1
                    button5.Enabled = false;//允许test
                    button6.Enabled = false;//test2
                    button7.Enabled = true;//说明书
                    break;
            }
        }
    }
    public class TextScaleEx : TextScale
    {
        protected override void SetControlFontSize(Control curCtrl, CtrlInfo ctrlInfo)
        {
            //效果：字体高度随容器宽度线性变化
            //根据设计器时期的字体大小与其容器宽度的比例计算得到当前字体大小
            float fontSize = ctrlInfo.Font.Size / ctrlInfo.Rect.Width * curCtrl.Width;
            curCtrl.Font = new Font(curCtrl.Font.Name, fontSize, curCtrl.Font.Style);
        }
    }
}