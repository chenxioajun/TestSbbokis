using HslCommunication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace testSbb
{
    public partial class Form2 : Form
    {
        // 控件声明
        private TextBox txtPassword;
        private Button btnOk;
        private Button btnCancel;
        private Button btnModifyPassword;
        private Label lblRole;
        private ComboBox cboRole;
        private Label lblPassword;

        // 密码存储
        public string Password { get; private set; }
        public bool IsPasswordCorrect { get; private set; } = false;
        public string SelectedRole { get; private set; }
        // 单独的密码变量（代替直接在字典中写死密码）
        private string _adminPassword = "1";       // admin密码变量
        private string _engineerPassword = "2"; // 工程师密码变量
        private string _operatorPassword = "3"; // 操作员密码变量

        private readonly Dictionary<string, string> _rolePasswords;

        JosnConfigHelper josnConfigHelper = new JosnConfigHelper("user.json");//配置文件

        // 新增：存储权限对应的数值（1→admin，2→工程师，3→操作员）
        public int RoleValue { get; private set; }
        public Form2()
        {
            this.DoubleBuffered = true;
            InitializeComponent();
            this.Shown += Form2_Shown;
            ReadCg();
            _rolePasswords = new Dictionary<string, string>
            {
                { "admin", _adminPassword },
                { "工程师", _engineerPassword },
                { "操作员", _operatorPassword }
            };
           
        }
        /// <summary>
        /// 写入配置文件
        /// </summary>
        private void Writercg()
        {
            josnConfigHelper.WriteConfig("adminPwd", _adminPassword); // 第一个参数是配置键，第二个参数是要写入的变量
            josnConfigHelper.WriteConfig("工程师", _engineerPassword); // 第一个参数是配置键，第二个参数是要写入的变量
            josnConfigHelper.WriteConfig("操作员", _operatorPassword); // 第一个参数是配置键，第二个参数是要写入的变量
        }
        /// <summary>
        /// 读取配置
        /// </summary>
        private void ReadCg()
        {
            _adminPassword = josnConfigHelper.ReadConfig("adminPwd");
            _engineerPassword = josnConfigHelper.ReadConfig("工程师");
            _operatorPassword = josnConfigHelper.ReadConfig("操作员");
            // 关键：同步更新字典（如果字典已初始化）
            if (_rolePasswords != null)
            {
                _rolePasswords["admin"] = _adminPassword;
                _rolePasswords["工程师"] = _engineerPassword;
                _rolePasswords["操作员"] = _operatorPassword;
            }

        }
        private void Form2_Shown(object? sender, EventArgs e)
        {
            InitializeSafeUI();
        }

        private void InitializeSafeUI()
        {
            // 窗口基础设置
            this.Text = "用户登录";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = SystemColors.Control;

            // 布局表格
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 3,
                ColumnCount = 2
            };
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            this.Controls.Add(table);

            // 1. 权限选择
            lblRole = new Label
            {
                Text = "选择权限：",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = SystemFonts.DefaultFont
            };
            table.Controls.Add(lblRole, 0, 0);

            cboRole = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                Font = SystemFonts.DefaultFont
            };
            cboRole.Items.AddRange(new[] { "admin", "工程师", "操作员" });
            cboRole.SelectedIndex = 2;
            cboRole.SelectedIndexChanged += CboRole_SelectedIndexChanged;
            table.Controls.Add(cboRole, 1, 0);

            // 2. 密码输入
            lblPassword = new Label
            {
                Text = "输入密码：",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = SystemFonts.DefaultFont
            };
            table.Controls.Add(lblPassword, 0, 1);

            txtPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                PasswordChar = '*',
                BorderStyle = BorderStyle.Fixed3D,
                Font = SystemFonts.DefaultFont
            };
            // 新增：密码输入变化时检查是否显示修改按钮
            txtPassword.TextChanged += TxtPassword_TextChanged;
            table.Controls.Add(txtPassword, 1, 1);

            // 3. 按钮区域
            var btnPanel = new Panel { Dock = DockStyle.Fill };
            table.Controls.Add(btnPanel, 0, 2);
            table.SetColumnSpan(btnPanel, 2);

            // 确定按钮
            btnOk = new Button
            {
                Text = "确定",
                Location = new Point(50, 10),
                Size = new Size(75, 23),
                FlatStyle = FlatStyle.Standard,
                Font = SystemFonts.DefaultFont
            };
            btnOk.Click += BtnOk_Click;
            btnPanel.Controls.Add(btnOk);

            // 取消按钮
            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(150, 10),
                Size = new Size(75, 23),
                FlatStyle = FlatStyle.Standard,
                Font = SystemFonts.DefaultFont
            };
            btnCancel.Click += BtnCancel_Click;
            btnPanel.Controls.Add(btnCancel);

            // 修改密码按钮（默认隐藏）
            btnModifyPassword = new Button
            {
                Text = "修改密码",
                Location = new Point(250, 10),
                Size = new Size(75, 23),
                FlatStyle = FlatStyle.Standard,
                Font = SystemFonts.DefaultFont,
                Visible = false
            };
            btnModifyPassword.Click += BtnModifyPassword_Click;
            btnPanel.Controls.Add(btnModifyPassword);
        }

        // 角色切换时检查修改按钮可见性
        private void CboRole_SelectedIndexChanged(object? sender, EventArgs e)
        {
            CheckModifyButtonVisibility();
        }

        // 密码输入变化时检查修改按钮可见性
        private void TxtPassword_TextChanged(object? sender, EventArgs e)
        {
            CheckModifyButtonVisibility();
        }

        // 核心逻辑：仅当选择admin且密码正确时显示修改按钮
        private void CheckModifyButtonVisibility()
        {
            bool isAdmin = cboRole.Text == "admin";
            if (isAdmin)
            {
                // 验证输入的密码是否与admin的正确密码一致
                bool isPasswordCorrect = txtPassword.Text == _adminPassword;
                btnModifyPassword.Visible = isPasswordCorrect;
            }
            else
            {
                btnModifyPassword.Visible = false;
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            ReadCg();
            string AD1,ad2,ad3;
            AD1 = _adminPassword;
            SelectedRole = cboRole.Text;
            string inputPwd = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(inputPwd))
            {
                MessageBox.Show("请输入密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPassword.Focus();
                return;
            }

            if (_rolePasswords.TryGetValue(SelectedRole, out string correctPwd) && inputPwd == correctPwd)
            {
                IsPasswordCorrect = true;
                Password = inputPwd;
                // 关键：根据选择的角色设置对应的数值（1、2、3）
                switch (SelectedRole)
                {
                    case "admin":
                        RoleValue = 1;
                        break;
                    case "工程师":
                        RoleValue = 2;
                        break;
                    case "操作员":
                        RoleValue = 3;
                        break;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("密码错误，请重新输入！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // 修改密码按钮点击事件
        private void BtnModifyPassword_Click(object? sender, EventArgs e)
        {
            using (var modifyForm = new FormModifyPassword(_rolePasswords))
            {
                if (modifyForm.ShowDialog() == DialogResult.OK)
                {
                    // 1. 获取选择的权限角色（如 "admin"、"工程师" 等）
                    string selectedRole = modifyForm.SelectedRole;
                    // 2. 获取用户输入的新密码
                    string newPassword = modifyForm.NewPassword;
                    // 3. 从字典中获取该角色的旧密码（用于验证或记录）
                    string oldPassword = _rolePasswords[selectedRole];

                    // 可以在这里添加额外的判断逻辑（例如验证旧密码是否正确，虽然修改窗口中已验证）
                    if (oldPassword != modifyForm.OldPassword)  // 需在FormModifyPassword中新增OldPassword属性
                    {
                        MessageBox.Show("旧密码验证失败，修改未生效！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 4. 根据选择的角色，更新对应的密码（如果用变量存储密码，这里同步更新变量）
                    switch (selectedRole)
                    {
                        case "admin":
                            _adminPassword = newPassword;  // 同步更新admin密码变量
                            break;
                        case "工程师":
                            _engineerPassword = newPassword;  // 同步更新工程师密码变量
                            break;
                        case "操作员":
                            _operatorPassword = newPassword;  // 同步更新操作员密码变量
                            break;
                        default:
                            MessageBox.Show("未知角色，修改失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                    }

                    // 5. 更新字典中的密码
                    _rolePasswords[selectedRole] = newPassword;
                    // 6. 写入配置（保存修改）
                    Writercg();
                    // 7. 提示修改成功（包含角色和密码信息）
                    MessageBox.Show(
                        $"已成功修改 {selectedRole} 的密码！\n旧密码：{oldPassword}\n新密码：{newPassword}",
                        "提示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }
        }

        // 修改后的密码修改窗口（增加权限选择）
        public class FormModifyPassword : Form
        {
            public string NewPassword { get; private set; }
            public string SelectedRole { get; private set; }
            public string OldPassword { get; private set; }  // 新增：存储用户输入的旧密码
            private readonly Dictionary<string, string> _rolePasswords;
            private TextBox txtOldPwd, txtNewPwd, txtConfirmPwd;
            private ComboBox cboModifyRole;

            public FormModifyPassword(Dictionary<string, string> rolePasswords)
            {
                _rolePasswords = rolePasswords;
                this.Text = "修改密码";
                this.Size = new Size(350, 260);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
                this.BackColor = SystemColors.Control;

                // 布局表格
                var table = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    RowCount = 5,
                    ColumnCount = 2
                };
                for (int i = 0; i < 5; i++)
                {
                    table.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
                }
                this.Controls.Add(table);

                // 1. 角色选择
                table.Controls.Add(new Label { Text = "选择角色：", Dock = DockStyle.Fill }, 0, 0);
                cboModifyRole = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Standard
                };
                cboModifyRole.Items.AddRange(_rolePasswords.Keys.ToArray());
                cboModifyRole.SelectedIndex = 0;
                cboModifyRole.SelectedIndexChanged += (s, e) => txtOldPwd.Clear();
                table.Controls.Add(cboModifyRole, 1, 0);

                // 2. 旧密码
                table.Controls.Add(new Label { Text = "旧密码：", Dock = DockStyle.Fill }, 0, 1);
                txtOldPwd = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill, BorderStyle = BorderStyle.Fixed3D };
                table.Controls.Add(txtOldPwd, 1, 1);

                // 3. 新密码
                table.Controls.Add(new Label { Text = "新密码：", Dock = DockStyle.Fill }, 0, 2);
                txtNewPwd = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill, BorderStyle = BorderStyle.Fixed3D };
                table.Controls.Add(txtNewPwd, 1, 2);

                // 4. 确认密码
                table.Controls.Add(new Label { Text = "确认密码：", Dock = DockStyle.Fill }, 0, 3);
                txtConfirmPwd = new TextBox { PasswordChar = '*', Dock = DockStyle.Fill, BorderStyle = BorderStyle.Fixed3D };
                table.Controls.Add(txtConfirmPwd, 1, 3);

                // 5. 确定按钮（修正核心：移除嵌套的事件绑定）
                var btnOk = new Button { Text = "确定", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Standard };
                btnOk.Click += (s, e) =>
                {
                    // 获取选择的角色和当前密码
                    string selectedRole = cboModifyRole.SelectedItem.ToString();
                    if (!_rolePasswords.TryGetValue(selectedRole, out string currentPwd))
                    {
                        MessageBox.Show("所选角色不存在");
                        return;
                    }

                    // 验证旧密码
                    if (txtOldPwd.Text != currentPwd)
                    {
                        MessageBox.Show($"输入的{selectedRole}旧密码错误");
                        return;
                    }
                    // 验证新密码不为空
                    if (string.IsNullOrEmpty(txtNewPwd.Text))
                    {
                        MessageBox.Show("新密码不能为空");
                        return;
                    }
                    // 验证两次输入一致
                    if (txtNewPwd.Text != txtConfirmPwd.Text)
                    {
                        MessageBox.Show("两次输入的新密码不一致");
                        return;
                    }

                    // 赋值属性（关键：在这里直接赋值，无需嵌套事件）
                    OldPassword = txtOldPwd.Text;  // 保存旧密码
                    SelectedRole = selectedRole;   // 保存选择的角色
                    NewPassword = txtNewPwd.Text;  // 保存新密码

                    // 关闭窗口并返回结果
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                table.Controls.Add(btnOk, 0, 4);

                // 取消按钮
                var btnCancel = new Button { Text = "取消", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Standard };
                btnCancel.Click += (s, e) => this.Close();
                table.Controls.Add(btnCancel, 1, 4);
            }
        }
    }
}
#region
//using System;
//using System.Windows.Forms;

//namespace testSbb
//{
//    public partial class Form2 : Form
//    {
//        private TextBox txtPassword;
//        private Button btnOk;
//        private Button btnCancel;
//        public string Password { get; private set; }
//        public bool IsPasswordCorrect { get; private set; } = false;
//        public Form2()
//        {
//            InitializeComponent();
//            // 设置窗口在屏幕中心显示
//            this.StartPosition = FormStartPosition.CenterScreen;
//            this.Text = "请输入密码";
//            this.Size = new System.Drawing.Size(320, 160);

//            txtPassword = new TextBox { PasswordChar = '*', Width = 200, Left = 50, Top = 30 };
//            btnOk = new Button { Text = "确定", Left = 50, Top = 70, Width = 75,Height=28 };
//            btnCancel = new Button { Text = "取消", Left = 150, Top = 70, Width = 75 ,Height = 28 };

//            btnOk.Click += button1_Click;
//            btnCancel.Click += button2_Click;

//            this.Controls.Add(txtPassword);
//            this.Controls.Add(btnOk);
//            this.Controls.Add(btnCancel);
//        }

//        private void button1_Click(object? sender, EventArgs e)
//        {
//            // 设置密码
//            string correctPassword = "12344321";  // 示例密码

//            if (txtPassword.Text == correctPassword)
//            {
//                IsPasswordCorrect = true;
//                Password = txtPassword.Text;
//                this.DialogResult = DialogResult.OK;
//                this.Close();
//            }
//            else
//            {
//                MessageBox.Show("密码错误，请重新输入！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void button2_Click(object? sender, EventArgs e)
//        {
//            this.DialogResult = DialogResult.Cancel;
//            this.Close();
//        }
//    }
//}
#endregion