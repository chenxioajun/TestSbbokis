using System;
using System.Drawing;
using System.Windows.Forms;

namespace testSbb.UI
{
    public partial class AuthForm : Form
    {
        private TextBox txtHardwareCode;
        private TextBox txtAuthCode;
        private Button btnShowHardwareCode;
        private Button btnAuthorize; // 新增：确认授权按钮
        private Button btnCancel;

        public AuthForm()
        {
            InitializeComponent();
            InitControls();
        }

        private void InitControls()
        {
            // 窗体基础设置（保持不变）
            this.Text = "设备授权验证";
            this.Size = new Size(450, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // 标题标签（保持不变）
            Label lblTitle = new Label
            {
                Text = "设备授权",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 51, 102),
                Size = new Size(400, 40),
                Location = new Point(20, 10),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // 硬件码区域（保持不变）
            GroupBox grpHardware = new GroupBox
            {
                Text = "设备硬件码（用于获取授权）",
                Size = new Size(400, 110),
                Location = new Point(20, 60),
                Font = new Font("微软雅黑", 9)
            };
            this.Controls.Add(grpHardware);

            txtHardwareCode = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(280, 30),
                Location = new Point(20, 30)
            };
            grpHardware.Controls.Add(txtHardwareCode);

            btnShowHardwareCode = new Button
            {
                Text = "获取",
                Size = new Size(90, 30),
                Location = new Point(310, 30),
                Font = new Font("微软雅黑", 9),
                BackColor = Color.FromArgb(72, 187, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnShowHardwareCode.Click += btnShowHardwareCode_Click;
            grpHardware.Controls.Add(btnShowHardwareCode);

            // 授权码区域（保持不变）
            GroupBox grpAuth = new GroupBox
            {
                Text = "输入授权码",
                Size = new Size(400, 110),
                Location = new Point(20, 180),
                Font = new Font("微软雅黑", 9)
            };
            this.Controls.Add(grpAuth);

            txtAuthCode = new TextBox
            {
                PlaceholderText = "请输入管理员提供的授权码",
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(350, 30),
                Location = new Point(20, 30)
            };
            grpAuth.Controls.Add(txtAuthCode);

            // 底部按钮区域（新增：确认授权按钮）
            Panel pnlButtons = new Panel
            {
                Size = new Size(400, 60), // 增大高度以容纳两个按钮
                Location = new Point(20, 300)
            };
            this.Controls.Add(pnlButtons);

            // 取消按钮（保持不变）
            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(90, 35),
                Location = new Point(290, 10),
                Font = new Font("微软雅黑", 9),
                BackColor = Color.FromArgb(230, 74, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            pnlButtons.Controls.Add(btnCancel);

            // 新增：确认授权按钮
            btnAuthorize = new Button
            {
                Text = "确认授权",
                Size = new Size(90, 35),
                Location = new Point(180, 10), // 与取消按钮并排
                Font = new Font("微软雅黑", 9),
                BackColor = Color.FromArgb(64, 158, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnAuthorize.Click += BtnAuthorize_Click; // 绑定点击事件
            pnlButtons.Controls.Add(btnAuthorize); // 添加到Panel容器
        }

        // 确认授权按钮点击事件（修正后）
        private void BtnAuthorize_Click(object sender, EventArgs e)
        {
            string authCode = txtAuthCode.Text.Trim();
            if (string.IsNullOrEmpty(authCode))
            {
                MessageBox.Show("请输入授权码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 关键修正：调用带混淆逻辑的VerifySerialNumber方法验证授权码0
            if (AuthHelper.VerifySerialNumber(authCode))
            {
                // 授权成功后，保存状态到注册表（下次启动无需重新授权）
                AuthHelper.SaveAuthorizeStatus();

                MessageBox.Show("授权成功！程序将正常启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK; // 告诉主程序授权通过
                this.Close();
            }
            else
            {
                MessageBox.Show("授权码无效，请重新输入）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtAuthCode.Focus(); // 聚焦输入框，方便重新输入
            }
        }
        // 获取硬件码按钮事件（保持不变）
        private void btnShowHardwareCode_Click(object sender, EventArgs e)
        {
            try
            {
                string hardwareCode = AuthHelper.GetDeviceHardwareCode();
                txtHardwareCode.Text = hardwareCode;
                Clipboard.SetText(hardwareCode);
                MessageBox.Show("已复制到剪贴板", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

   
}