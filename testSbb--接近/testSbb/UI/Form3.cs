using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace testSbb
{
    public partial class Form3 : Form
    {
        
        public string Password { get; private set; }
        public bool IsPasswordCorrect { get; private set; } = false;
        public Form3()
        {
            InitializeComponent();

            // 基础窗口设置（禁止缩放+居中）
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "版本信息";
            this.Size = new Size(540, 375); // 扩大窗口
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 1. 软件名称标签（更宽，避免换行）
            var lblAppName = new Label
            {
                Text = "赛飞自动化控制软件（SFCLJ）",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                Left = 100,    // 右移，给Logo留空间
                Top = 30,
                Width = 400,   // 加宽，容纳长文字
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 2. 核心版本信息标签（增加高度，避免文字重叠）
            var lblVersion = new Label
            {
                Text = "当前版本：V1.1.0（内部版本：202510170001）\n发布日期：2025年10月17日",
                Font = new Font("微软雅黑", 9),
                Left = 100,
                Top = 70,      // 下移，避开名称标签
                Width = 400,
                Height = 50,   // 增加高度，容纳两行文字
                               //  LineHeight = 22
            };

            // 3. 版权信息标签（加宽+下移）
            var lblCopyright = new Label
            {
                Text = "© 2025 深圳市赛飞自动化设备有限公司 版权所有\n未经授权，禁止复制或传播",
                Font = new Font("微软雅黑", 8, FontStyle.Italic),
                ForeColor = Color.Gray,
                Left = 100,
                Top = 130,     // 下移，避开版本标签
                Width = 400,
                Height = 40,   // 增加高度
                               //
            };

            // 4. 系统要求标签（加宽+下移）
            var lblSystem = new Label
            {
                Text = "系统要求：Windows 10/11（64位） | .NET8.0+",
                Font = new Font("微软雅黑", 8),
                Left = 100,
                Top = 180,     // 下移
                Width = 400
            };

            // 5. 联系支持标签（加宽+下移）
            var lblSupport = new Label
            {
                Text = "技术支持：0755-23772116 | chenlujun@saifeizdh.com",
                Font = new Font("微软雅黑", 8),
                Left = 100,
                Top = 220,     // 下移
                Width = 400
            };

            // 6. Logo图片（位置微调）
            var pbLogo = new PictureBox
            {
                Left = 30,
                Top = 30,
                Width = 60,    // 适当放大Logo
                Height = 60,
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            // 7. 确定按钮（位置微调）
            var btnConfirm = new Button
            {
                Text = "确定",
                Font = new Font("微软雅黑", 9),
                Left = 380,    // 右移，适配新窗口宽度
                Top = 250,     // 下移，靠近窗口底部
                Width = 100,   // 加宽按钮
                Height = 30
            };
            // 7. 确定按钮（位置微调）
            var btnConfirm1 = new Button
            {
                Text = "联系我们",
                Font = new Font("微软雅黑", 9),
                Left = 260,    // 右移，适配新窗口宽度
                Top = 250,     // 下移，靠近窗口底部
                Width = 100,   // 加宽按钮
                Height = 30
            };
            btnConfirm.Click += (s, e) => this.Close();
            btnConfirm1.Click += BtnConfirm1_Click;
            // 添加所有控件到窗口
            this.Controls.Add(lblAppName);
            this.Controls.Add(lblVersion);
            this.Controls.Add(lblCopyright);
            this.Controls.Add(lblSystem);
            this.Controls.Add(lblSupport);
            this.Controls.Add(pbLogo);
            this.Controls.Add(btnConfirm);
            this.Controls.Add(btnConfirm1);

            // 加载图片
            LoadImageToPictureBox(pbLogo, "pcn6.png");
        }

        // 复用图片加载方法（参数改为PictureBox，避免重复代码）
        private void LoadImageToPictureBox(PictureBox pb,string fileName)
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

            //try
            //{
            //    string imagePath = GetImagePath("pcn6.png");
            //    if (File.Exists(imagePath))
            //    {
            //        using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            //        {
            //            pb.Image = Image.FromStream(fs);
            //        }
            //    }
            //}
            //catch
            //{
            //    // 图片加载失败时不弹窗，避免影响用户体验
            //    pb.Image = null;
            //}
        }
        private void BtnConfirm1_Click(object? sender, EventArgs e)
        {

            string companyWebsite = "http://www.szsaifei.com/about.html";
            try
            {
                // 启动系统默认浏览器并访问指定 URL
                Process.Start(new ProcessStartInfo(companyWebsite)
                {
                    // 设置为使用默认浏览器打开
                    UseShellExecute = true
                });
                this.Close();
            }
            catch (Exception ex)
            {
                // 捕获异常（如浏览器未找到、URL 无效等）
                this.Close();
                MessageBox.Show($"无法打开官网：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
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
    }
}
