using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TcpUserServer
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            txtStatus.Text = "Đang đăng nhập...";
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                txtStatus.Text = "Nhập username và password";
                btnLogin.Enabled = true;
                return;
            }

            string hash = Utils.Sha256Hash(password);
            var req = new Request { Action = "Login", Payload = new { Username = username, PasswordHash = hash } };

            try
            {
                // gọi async và đợi kết quả
                var resp = await ClientHelper.SendRequestAsync(req);

                // kiểm tra null để tránh NullReferenceException
                if (resp == null)
                {
                    txtStatus.Text = "Không có phản hồi từ server";
                    return;
                }

                txtStatus.Text = resp.Message ?? "";

                if (resp.Success && resp.Data != null)
                {
                    // lấy token + user an toàn
                    string token = resp.Data.Value<string>("Token");
                    var user = resp.Data["User"] as Newtonsoft.Json.Linq.JObject;
                    string uname = user != null && user["Username"] != null ? (string)user["Username"] : username;

                    // gọi constructor có 2 tham số (MainForm phải có constructor này)
                    var main = new MainForm(uname, token);
                    this.Hide();
                    main.ShowDialog();
                    this.Show();
                }
            }
            catch (Exception ex)
            {
                // hiển thị lỗi thân thiện
                txtStatus.Text = "Lỗi: " + ex.Message;
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void btnOpenRegister_Click(object sender, EventArgs e)
        {
            using (var reg = new RegisterForm())
            {
                reg.ShowDialog();
            }
        }
    }
}
