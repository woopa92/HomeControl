using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace HomeControlPCClient
{
    /// <summary>
    /// UCMainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UCMainView : UserControl
    {
        private HttpListener listener = new HttpListener();
        private static readonly HttpClient client = new HttpClient();
        private const string IP = "http://localhost";
        private const string PortServer = "5000";
        private const string PortListener = "8080";

        public UCMainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            StartHttpServer();
            RegisterToServer();
        }

        private async void StartHttpServer()
        {
            listener.Prefixes.Add($"{IP}:{PortListener}/");
            listener.Start();
            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = HandleRequest(context); // fire & forget
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            string body = await reader.ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(body);
            string command = data?.command;

            if (command == "AudioChange")
            {
                Console.WriteLine("서버에서 Start 명령 수신!");
                var result = await PerformWork();
                // 서버에 완료 통지
                var complete = new { result = $"{result}", client = "pc1" };
                var json = new StringContent(JsonConvert.SerializeObject(complete), Encoding.UTF8, "application/json");
                await client.PostAsync($"{IP}:{PortServer}/client-complete", json);
            }

            // 응답 전송
            string responseString = "{\"status\":\"ok\"}";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private async Task<bool> PerformWork()
        {
            // 오디오 장치 전환 넣기

            Console.WriteLine("작업 완료");
            return false;
        }

        private async void RegisterToServer()
        {
            var content = new
            {
                url = $"{IP}:{PortListener}/"
            };
            var json = JsonConvert.SerializeObject(content);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync($"{IP}:{PortServer}/register", body);
        }

    }

}