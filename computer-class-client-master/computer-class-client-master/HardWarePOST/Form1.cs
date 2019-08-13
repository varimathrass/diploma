using Newtonsoft.Json.Linq;
using OpenHardwareMonitor.Hardware;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace HardWarePOST
{
    public partial class Form1 : Form
    {
        private string comp_id;
        UpdateVisitor updateVisitor = new UpdateVisitor();
        string server_uri = "";
        System.Threading.Timer timer;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ChangeStatus(AppStatus.Offline);
            textBox1.Text = "http://localhost:3000/";
            this.BeginInvoke((MethodInvoker) this.connectToServer);

        }


        private void connectToServer()
        {
            

            ChangeStatus(AppStatus.Connecting);

            Computer c = new Computer()
            {
                CPUEnabled = true,
                GPUEnabled = true,
                HDDEnabled = true,
                MainboardEnabled = true,
                RAMEnabled = true
            };
            c.Open();
            
            var client = new RestClient(server_uri);
            var request = new RestRequest("api/computers", Method.POST);

            var comp = new ComputerInfo();

            foreach (var hardware in c.Hardware)
            {

                switch (hardware.HardwareType)
                {
                    case (HardwareType.CPU):
                        {
                            hardware.Update();
                            comp.cpuName = hardware.Name;
                            comp.cpuCores = hardware.Sensors.Count(s => s.SensorType == SensorType.Load) - 1;
                            break;
                        }
                    case (HardwareType.GpuNvidia):
                    case (HardwareType.GpuAti):
                        {
                            comp.gpuName = hardware.Name;
                            break;
                        }
                    case (HardwareType.RAM):
                        {
                            hardware.Update();
                            comp.ram = (float)hardware.Sensors.Where(s => s.SensorType == SensorType.Data).Select(s => s.Value).Sum();
                            break;
                        }

                    case (HardwareType.Mainboard):
                        {
                            comp.motherboard = hardware.Name;
                            break;
                        }
                }
            }
            c.Close();

            comp.hostName = System.Environment.MachineName;
            comp.macAddress = GetMacAddress().ToString();
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(comp);

            var responseData = client.Execute(request);

            if (responseData.StatusCode == HttpStatusCode.OK)
            {
                ChangeStatus(AppStatus.Online);
                dynamic response = JObject.Parse(responseData.Content);
                comp_id = response.id;
                var startTimeSpan = TimeSpan.Zero;
                var periodTimeSpan = TimeSpan.FromSeconds(6);
                timer = new System.Threading.Timer((e) =>
                {
                    SendHardWareData();
                }, null, startTimeSpan, periodTimeSpan);

        }
            else
            {
                ChangeStatus(AppStatus.Offline);
                Console.WriteLine(responseData.Content);
            }
            

        }
        


        private void SendHardWareData()
        {
            Computer c = new Computer();

            
            c.Open();
            c.CPUEnabled = true;
            c.GPUEnabled = true;
            c.HDDEnabled = true;
            c.RAMEnabled = true;
            c.FanControllerEnabled = true;
            c.MainboardEnabled = true;
            c.Accept(updateVisitor);
            Indicators computerIndicators = new Indicators();

            foreach (var hardware in c.Hardware)
            {
                

                switch (hardware.HardwareType)
                {
                    case (HardwareType.CPU):
                        {

                            computerIndicators.CPU.Cores = hardware.Sensors.Where(s => s.SensorType == SensorType.Load).Count() - 1;
                            computerIndicators.CPU.Load = hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.StartsWith("CPU Core")).Select(s => s.Value.GetValueOrDefault()).ToArray();
                            computerIndicators.CPU.LoadTotal = hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Equals("CPU Total")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.CPU.TempetureTotal = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Equals("CPU Package")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.CPU.Tempeture = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.StartsWith("CPU Core")).Select(s => s.Value.GetValueOrDefault()).ToArray();
                            break;
                        }
                    case (HardwareType.GpuNvidia):
                    case (HardwareType.GpuAti):
                        {
                            
                            computerIndicators.GPU.Tempeture = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.GPU.FreeMemory = hardware.Sensors.Where(s => s.Name.Equals("GPU Memory Free")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.GPU.UsedMemory = hardware.Sensors.Where(s => s.Name.Equals("GPU Memory Used")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            break;
                        }
                    case (HardwareType.RAM):
                        {
                            computerIndicators.RAM.AvaliableMemory = hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Name.Equals("Available Memory")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.RAM.UsedMemory = hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Name.Equals("Used Memory")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            break;
                        }
                    case (HardwareType.HDD):
                        {
                            computerIndicators.HDD.Tempeture = hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            computerIndicators.HDD.UsedSpace = hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Equals("Used Space")).Select(s => s.Value.GetValueOrDefault()).FirstOrDefault();
                            break;
                        }
                    case (HardwareType.Mainboard):
                        {
                            break;
                        }
                }
            }
            Console.WriteLine(String.Format("[{0}:{1}:{2}] Sending data......", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));

            var client = new RestClient(server_uri);
            var request = new RestRequest("api/computers/" + comp_id, Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new { update = computerIndicators, date = DateTime.Now });
            client.ExecuteAsync(request, response => {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine(String.Format("[{0}:{1}:{2}] Data succesfully sent!", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));
                }
            });          

           

        }

        public static PhysicalAddress GetMacAddress()
        {
            var myInterfaceAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .Select(n => n.GetPhysicalAddress())
                .FirstOrDefault();

            return myInterfaceAddress;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            server_uri = textBox1.Text;
            //if (timer != null)
            //{
            //    timer.Dispose();
            //}
            //connectToServer();
        }

        private void ChangeStatus(AppStatus st)
        {
            switch (st)
            {
                case AppStatus.Offline:
                    {
                        label3.Text = "Offline :(";
                        label3.ForeColor = Color.Red;
                        break;
                    }
                case AppStatus.Connecting:
                    {
                        label3.Text = "Connecting...";
                        label3.ForeColor = Color.LightBlue;
                        break;
                    }
                case AppStatus.Online:
                    {
                        label3.Text = "Online :)";
                        label3.ForeColor = Color.Green;
                        break;
                    }
            }
        }


        private void sendOfflineStatus()
        {
            var client = new RestClient(server_uri);
            var request = new RestRequest("api/computers/" + comp_id +"/offline", Method.POST);
            request.RequestFormat = DataFormat.Json;
            client.ExecuteAsync(request, (response)=> { });
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.sendOfflineStatus();
        }
    }



    enum AppStatus
    {
        Offline,
        Connecting,
        Online
    }


}
