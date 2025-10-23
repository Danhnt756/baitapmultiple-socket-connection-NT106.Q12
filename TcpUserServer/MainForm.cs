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
    public partial class MainForm : Form
    {
        private readonly string token;
        private readonly string username;
        public MainForm()
        {
            InitializeComponent();
        }
        public MainForm(string username, string token) : this()
        {
            this.username = username;
            this.token = token;

            // khởi tạo UI cơ bản ngay (nếu các control đã được tạo trong InitializeComponent)
            if (txtWelcome != null)
                txtWelcome.Text = "Xin chào, " + username;
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
            // đảm bảo event này được liên kết trong Designer (MainForm.Load += MainForm_Load)
            await LoadProfile();
        }
        private async System.Threading.Tasks.Task LoadProfile()
        {
            txtProfileStatus.Text = "Loading profile...";
            var req = new Request { Action = "GetProfile", Payload = new { Token = token } };
            try
            {
                var resp = await ClientHelper.SendRequestAsync(req);
                txtProfileStatus.Text = resp.Message;
                if (resp.Success && resp.Data != null)
                {
                    txtEmail.Text = resp.Data.Value<string>("Email") ?? "";
                    txtBirthday.Text = resp.Data.Value<string>("Birthday") ?? "";
                }
            }
            catch (Exception ex)
            {
                txtProfileStatus.Text = "Error: " + ex.Message;
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
