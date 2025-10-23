using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace TcpUserServer
{
    public static class ClientHelper
    {
        public static string ServerHost = "127.0.0.1";
        public static int ServerPort = 9000;

        public static async Task<Response> SendRequestAsync(Request req)
        {
            TcpClient client = new TcpClient();
            await client.ConnectAsync(ServerHost, ServerPort);
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string reqJson = JsonConvert.SerializeObject(req);
                    await writer.WriteLineAsync(reqJson);
                    await writer.FlushAsync();

                    string respLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(respLine))
                        return new Response { Success = false, Message = "No response" };

                    Response resp = JsonConvert.DeserializeObject<Response>(respLine);
                    return resp;
                }
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }
    }
}
