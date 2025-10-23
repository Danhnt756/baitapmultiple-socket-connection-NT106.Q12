using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TcpUserServer
{
    public partial class ServerForm : Form
    {
        private TcpListener listener;
        private Thread listenThread;
        private volatile bool isRunning = false;
        private readonly ConcurrentDictionary<TcpClient, string> clients = new ConcurrentDictionary<TcpClient, string>();
        private readonly string connectionString = @"Server=.\SQLEXPRESS;Database=UserDB;Trusted_Connection=True;";

        public ServerForm()
        {
            InitializeComponent();
            txtPort.Text = "9000";
            btnStop.Enabled = false;
            txtLog.ReadOnly = true;
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (isRunning) return;
            int port;
            if (!int.TryParse(txtPort.Text.Trim(), out port))
            {
                AppendLog("Port không hợp lệ.");
                return;
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;
                listenThread = new Thread(ListenLoop);
                listenThread.IsBackground = true;
                listenThread.Start();
                AppendLog("Server started on port " + port);
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception ex)
            {
                AppendLog("Lỗi start server: " + ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            if (!isRunning) return;
            isRunning = false;
            try { listener.Stop(); } catch { }
            foreach (var kv in clients.Keys)
            {
                try { kv.Close(); } catch { }
            }
            clients.Clear();
            this.BeginInvoke((Action)(() => lstClients.Items.Clear()));
            AppendLog("Server stopped.");
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void ListenLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient tcp = listener.AcceptTcpClient(); // blocking
                    string ep = tcp.Client.RemoteEndPoint != null ? tcp.Client.RemoteEndPoint.ToString() : "unknown";
                    clients[tcp] = ep;
                    this.BeginInvoke((Action)(() => lstClients.Items.Add(ep)));
                    AppendLog("Client connected: " + ep);
                    Thread th = new Thread(() => HandleClient(tcp));
                    th.IsBackground = true;
                    th.Start();
                }
                catch (SocketException)
                {
                    // likely listener stopped
                    break;
                }
                catch (Exception ex)
                {
                    AppendLog("ListenLoop error: " + ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient tcp)
        {
            string remote = tcp.Client.RemoteEndPoint != null ? tcp.Client.RemoteEndPoint.ToString() : "unknown";
            try
            {
                NetworkStream stream = tcp.GetStream();
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    while (isRunning && tcp.Connected)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        AppendLog("Recv from " + remote + ": " + line);

                        JObject reqObj = null;
                        try
                        {
                            reqObj = JObject.Parse(line);
                        }
                        catch (Exception je)
                        {
                            AppendLog("Invalid JSON from client: " + je.Message);
                            var bad = new { Success = false, Message = "Invalid JSON" };
                            writer.WriteLine(JsonConvert.SerializeObject(bad));
                            continue;
                        }

                        string action = reqObj.Value<string>("Action");
                        JObject payload = reqObj["Payload"] as JObject;

                        object respObj = ProcessRequest(action, payload);
                        string respJson = JsonConvert.SerializeObject(respObj);
                        writer.WriteLine(respJson);
                        AppendLog("Sent to " + remote + ": " + respJson);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("Client " + remote + " error: " + ex.Message);
            }
            finally
            {
                string removed;
                clients.TryRemove(tcp, out removed);
                this.BeginInvoke((Action)(() => lstClients.Items.Remove(remote)));
                try { tcp.Close(); } catch { }
                AppendLog("Client disconnected: " + remote);
            }
        }

        private object ProcessRequest(string action, JObject payload)
        {
            if (string.IsNullOrEmpty(action))
                return new { Success = false, Message = "Empty action" };

            try
            {
                if (action == "Register") return HandleRegister(payload);
                if (action == "Login") return HandleLogin(payload);
                if (action == "GetProfile") return HandleGetProfile(payload);
                return new { Success = false, Message = "Unknown action" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Message = "Server exception: " + ex.Message };
            }
        }

        private object HandleRegister(JObject payload)
        {
            if (payload == null) return new { Success = false, Message = "Missing payload" };
            string username = payload.Value<string>("Username");
            string passHash = payload.Value<string>("PasswordHash");
            string email = payload.Value<string>("Email");
            string fullname = payload.Value<string>("FullName");
            DateTime? birthday = null;
            if (payload["Birthday"] != null)
            {
                DateTime dt;
                if (DateTime.TryParse(payload.Value<string>("Birthday"), out dt)) birthday = dt;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmdCheck = new SqlCommand("SELECT COUNT(1) FROM Users WHERE Username = @u", conn))
                {
                    cmdCheck.Parameters.AddWithValue("@u", username);
                    int cnt = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (cnt > 0) return new { Success = false, Message = "Username already exists" };
                }

                using (var cmd = new SqlCommand("INSERT INTO Users (Username, PasswordHash, Email, FullName, Birthday) VALUES (@u,@p,@e,@f,@b); SELECT SCOPE_IDENTITY()", conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", passHash);
                    cmd.Parameters.AddWithValue("@e", (object)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@f", (object)fullname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@b", (object)birthday ?? DBNull.Value);
                    object idObj = cmd.ExecuteScalar();
                    int newId = Convert.ToInt32(idObj);
                    return new { Success = true, Message = "Register success", Data = new { UserId = newId, Username = username } };
                }
            }
        }

        private object HandleLogin(JObject payload)
        {
            if (payload == null)
                return new { Success = false, Message = "Missing payload" };

            string username = payload.Value<string>("Username");
            string passHash = payload.Value<string>("PasswordHash");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passHash))
                return new { Success = false, Message = "Missing username or password" };

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Debug: ghi tên database hiện tại
                try
                {
                    using (var cmdDb = new SqlCommand("SELECT DB_NAME()", conn))
                    {
                        var curDb = cmdDb.ExecuteScalar();
                        AppendLog("Connected DB: " + (curDb ?? "<null>"));
                    }
                }
                catch (Exception exDb)
                {
                    AppendLog("DB name check failed: " + exDb.Message);
                }

                // Kiểm tra user hợp lệ
                using (var cmd = new SqlCommand(
                    "SELECT UserId, Username, Email, FullName, Birthday FROM Users WHERE Username=@u AND PasswordHash=@p",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", passHash);

                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read())
                        {
                            AppendLog($"Login failed for user '{username}': invalid credentials");
                            return new { Success = false, Message = "Invalid username or password" };
                        }

                        int userId = rdr.GetInt32(0);
                        string dbUsername = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        string email = rdr.IsDBNull(2) ? null : rdr.GetString(2);
                        string fullName = rdr.IsDBNull(3) ? null : rdr.GetString(3);
                        string birthday = rdr.IsDBNull(4) ? null : rdr.GetDateTime(4).ToString("yyyy-MM-dd");

                        JObject user = new JObject
                        {
                            ["UserId"] = userId,
                            ["Username"] = dbUsername,
                            ["Email"] = email,
                            ["FullName"] = fullName,
                            ["Birthday"] = birthday
                        };

                        rdr.Close();

                        // ==== Chọn cách tạo thời gian ở đây ====
                        // Nếu bạn muốn "hết hạn sau 2 giờ theo giờ máy chủ (local)",
                        DateTimeOffset expire = DateTimeOffset.Now.AddHours(2);

                        // Nếu bạn muốn "hết hạn sau 2 giờ theo UTC", dùng:
                        // DateTimeOffset expire = DateTimeOffset.UtcNow.AddHours(2);

                        Guid tokenGuid = Guid.NewGuid();

                        // Thực hiện INSERT token vào bảng UserTokens — kiểu cột nên là datetimeoffset để lưu offset đúng
                        try
                        {
                            using (var cmdToken = new SqlCommand(
                                "INSERT INTO dbo.UserTokens (TokenId, UserId, ExpireAt) VALUES (@t, @uid, @exp)",
                                conn))
                            {
                                cmdToken.Parameters.Add("@t", System.Data.SqlDbType.UniqueIdentifier).Value = tokenGuid;
                                cmdToken.Parameters.Add("@uid", System.Data.SqlDbType.Int).Value = userId;
                                cmdToken.Parameters.Add("@exp", System.Data.SqlDbType.DateTimeOffset).Value = expire;

                                int affected = cmdToken.ExecuteNonQuery();
                                AppendLog($"Token insert affected {affected} rows for user {userId}");
                            }
                        }
                        catch (Exception exToken)
                        {
                            AppendLog("Token insert failed: " + exToken.Message);
                        }

                        JObject data = new JObject
                        {
                            ["User"] = user,
                            ["Token"] = tokenGuid.ToString(),
                            // Trả về ISO 8601 có offset, ví dụ: "2025-10-18T18:00:00+07:00"
                            ["ExpireAt"] = expire.ToString("o")
                        };

                        return new { Success = true, Message = "Login success", Data = data };
                    }
                }
            }
        }

        private object HandleGetProfile(JObject payload)
        {
            if (payload == null) return new { Success = false, Message = "Missing payload" };
            string tokenStr = payload.Value<string>("Token");
            if (string.IsNullOrEmpty(tokenStr)) return new { Success = false, Message = "Missing token" };

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"SELECT u.UserId,u.Username,u.Email,u.FullName,u.Birthday,t.ExpireAt
                                                 FROM UserTokens t JOIN Users u ON t.UserId = u.UserId
                                                 WHERE t.TokenId = @t", conn))
                {
                    cmd.Parameters.AddWithValue("@t", tokenStr);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read()) return new { Success = false, Message = "Token not found" };
                        DateTime expire = rdr.GetDateTime(5);
                        if (DateTime.UtcNow > expire) return new { Success = false, Message = "Token expired" };

                        JObject user = new JObject();
                        user["UserId"] = rdr.GetInt32(0);
                        user["Username"] = rdr.GetString(1);
                        user["Email"] = rdr.IsDBNull(2) ? null : (JToken)rdr.GetString(2);
                        user["FullName"] = rdr.IsDBNull(3) ? null : (JToken)rdr.GetString(3);
                        user["Birthday"] = rdr.IsDBNull(4) ? null : (JToken)rdr.GetDateTime(4).ToString("yyyy-MM-dd");
                        return new { Success = true, Message = "OK", Data = user };
                    }
                }
            }
        }

        private void AppendLog(string text)
        {
            string line = string.Format("[{0}] {1}{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), text, Environment.NewLine);
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke((Action)(() => txtLog.AppendText(line)));
            }
            else
            {
                txtLog.AppendText(line);
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
