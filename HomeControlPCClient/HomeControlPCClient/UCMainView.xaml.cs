using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using NAudio.CoreAudioApi;
using AudioSwitcher.AudioApi.CoreAudio;

namespace HomeControlPCClient
{
    /// <summary>
    /// UCMainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UCMainView : UserControl
    {
        #region Variable
        /// <summary>
        /// HTTP
        /// </summary>
        private HttpListener listener = new HttpListener();
        private static readonly HttpClient client = new HttpClient();
        private const string IP = "http://localhost";
        private const string PortServer = "5000";
        private const string PortListener = "8080";
        private const string DeviceName = "HYTE60"; // 장치 이름 (예: HYTE60, HYTE70 등)
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Audio
        /// </summary>
        private readonly CoreAudioController audioController = new CoreAudioController();
        private CoreAudioDevice headsetDevice;
        private CoreAudioDevice speakerDevice;
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region Property

        #endregion

        #region Class
        public class AudioDevice
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        [ComImport]
        [Guid("294935CE-F637-4E7C-A41B-AB255460B862")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, Role role);
        }

        private class PolicyConfig : IPolicyConfig
        {
            private readonly IPolicyConfig _policyConfig;

            public PolicyConfig()
            {
                var clsid = new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
                var iid = typeof(IPolicyConfig).GUID;
                _policyConfig = (IPolicyConfig)Activator.CreateInstance(Type.GetTypeFromCLSID(clsid));
            }

            public void SetDefaultEndpoint(string deviceId, Role role)
            {
                _policyConfig.SetDefaultEndpoint(deviceId, role);
            }
        }
        #endregion


        #region Method

        /// <summary>
        /// 기본 시스템
        /// </summary>
        public UCMainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            Task.Run(() =>
            {
                LoadTargetDevices();
            }).ContinueWith(t =>
            {
                StartHttpServer();
                RegisterToServer();
            });

        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// HTTP Communication
        /// </summary>
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

            switch(command)
            {
                case "AudioChange":
                    {
                        string curAudio = audioController.DefaultPlaybackDevice.Equals(headsetDevice) ? "HeadSet" : "Speaker";
                        var result = await AudioChange();
                        // 서버에 완료 통지
                        var complete = new { result = $"{curAudio}", client = DeviceName };
                        var json = new StringContent(JsonConvert.SerializeObject(complete), Encoding.UTF8, "application/json");
                        await client.PostAsync($"{IP}:{PortServer}/client-complete", json);
                    }
                    break;
                case "Register":
                    // 서버에 등록
                    break;
                default:
                    break;
            }
            // 응답 전송
            string responseString = "{\"status\":\"ok\"}";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        private async void RegisterToServer()
        {
            string curAudio = audioController.DefaultPlaybackDevice.Equals(headsetDevice) ? "HeadSet" : "Speaker";
            var content = new
            {
                result = $"{curAudio}",
                url = $"{IP}:{PortListener}/"
            };
            var json = JsonConvert.SerializeObject(content);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            await client.PostAsync($"{IP}:{PortServer}/register", body);
        }

        // Message 별 동작 함수
        private async Task<bool> AudioChange()
        {
            // 오디오 장치 전환 넣기
            var currentDevice = audioController.DefaultPlaybackDevice;

            if(currentDevice == headsetDevice)
            {
                if (speakerDevice == null)
                    return false;
                await speakerDevice.SetAsDefaultAsync();
            }
            else if (currentDevice == speakerDevice)
            {
                if (headsetDevice == null)
                    return false;
                await headsetDevice?.SetAsDefaultAsync();
            }
            return true;
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Audio Switching
        /// </summary>
        private async void LoadTargetDevices()
        {
            var playbackDevices = await audioController.GetPlaybackDevicesAsync();
            var activeDevices = playbackDevices.Where(d => d.State == AudioSwitcher.AudioApi.DeviceState.Active);

            // 이름에 특정 문자열이 포함된 장치 찾기 (대소문자 무시)
            if(headsetDevice == null)
                headsetDevice = activeDevices.FirstOrDefault(d => d.FullName.ToLower().Contains("메인헤드셋"));
            if(speakerDevice == null)
                speakerDevice = activeDevices.FirstOrDefault(d => d.FullName.ToLower().Contains("메인스피커"));

            if (headsetDevice == null || speakerDevice == null)
            {
                MessageBox.Show($"s : {speakerDevice.Name} || h :{headsetDevice.Name}");
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

    }

}