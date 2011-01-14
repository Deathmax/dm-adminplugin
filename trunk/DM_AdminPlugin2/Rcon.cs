using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using aIW;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DM_AdminPlugin2
{
    public class DM_AdminPluginRcon : AdminPluginBase
    {
        #region Variables
        public bool continueServer = true;
        private string password = "";
        private DateTime lastRequest;
        Random random = new Random();
        private bool init = false;
        #endregion
        public override void OnFrame()
        {
            if (!init)
            {
                Thread start = new Thread(new ThreadStart(Start));
                start.Start();
                init = true;
            }
        }
        public void Start()
        {
            Log.Info("DM_AdminPlugin : Starting RconServer");
            try
            {
                int listenport = 28900;
                if (!Int32.TryParse(DM_AdminPluginHelper.modCvars.FirstOrDefault(i => i.Key == "p_rcon_port").Value, out listenport))
                    listenport = 28900;
                //password = DM_AdminPluginHelper.modCvars.First(i => i.Key == "p_rcon_password").Value;
                password = GetDvar("rcon_password");
                Log.Info(string.Format("DM_AdminPlugin : RconServer : Server is running at UDP port {0} and password is {1}",
                    listenport.ToString(),
                    password));
                UdpClient listener = new UdpClient(listenport);
                var listenEP = new IPEndPoint(IPAddress.Any, listenport);

                while (continueServer)
                {
                    listenEP = new IPEndPoint(IPAddress.Any, listenport);
                    lastRequest = DateTime.Now;
                    byte[] bytes = listener.Receive(ref listenEP);
                    Log.Info(string.Format("DM_AdminPlugin : Recieved packet from {0}", listenEP.Address.ToString()));
                    var packet = parsePacket(bytes);
                    listener.Send(packet, packet.Length, listenEP);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /*private void sendPacket(byte[] bytes, int length, IPEndPoint EP)
        {
            try
            {
                UdpClient client = new UdpClient();
                client.Send(bytes, length, EP);
                client.Close();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }*/

        private byte[] parsePacket(byte[] bytes)
        {
            string response = "";
            var packetstring = Encoding.UTF8.GetString(bytes).Substring(4);
            if ((DateTime.Now - lastRequest).Milliseconds >= 100)
            {
                if (packetstring.StartsWith("rcon"))
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        var args = packetstring.Split(' ');
                        if (args.Length >= 3)
                        {
                            if (args[1] == password)
                            {
                                string command = "";
                                for (int i = 2; i < args.Length; i++)
                                    command += args[i] + " ";
                                command = command.TrimEnd(' ');
                                response = executeCommand(command);
                            }
                            else
                            {
                                response = "Invalid password.";
                                Log.Info("DM_AdminPlugin : Bad rcon password");
                            }
                        }
                        else
                            response = "No command.";
                    }
                    else
                    {
                        response = "The server must set 'rcon_password' for clients to use 'rcon'."; Log.Info("DM_AdminPlugin : No rcon_password set");
                    }
                }
                else
                    Log.Info("DM_AdminPlugin : Not a valid RCON packet");
            }
            Log.Debug(response);
            return (constructPacket(response));
            //if (!string.IsNullOrEmpty(response)) { constructPacket(response, EP); }
        }

        private string executeCommand(string command)
        {
            var randomint = random.Next(1000, 10000).ToString();
            ExecuteCommand(randomint + " rcon\n");
            ExecuteCommand(command);
            Log.Info(string.Format("DM_AdminPlugin : Handled rcon {0}", command));
            return getResponse(randomint);
        }

        private byte[] constructPacket(string response)
        {
            var oob = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(oob);
            writer.Write(Encoding.UTF8.GetBytes(string.Format("print\n{0}", response)));
            var packet = new byte[(int)stream.Length];
            packet = stream.GetBuffer();
            return packet;
            //sendPacket(packet, (int)stream.Length, EP);
        }

        #region Reading Console Output
        private string getResponse(string check)
        {
            var content = GetLogContent().Split('\n');
            Log.Debug(GetLogContent());
            string output = "";
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i].IndexOf(string.Format("Unknown command \"{0}\"", check)) != -1)
                {
                    for (++i; i < content.Length; i++)
                        output += content[i] + "\n";
                }
            }
            return output;
        }
        #region NTAuthority's code
        private static readonly int _hwndConsoleInpEditLoc = 0x64feea8;
        private static readonly int _hwndConsoleLogEditLoc = 0x64fee9c;
        private static IntPtr _hwndInput;
        private static IntPtr _hwndLog;
        private static IntPtr _process;
        public const uint WM_CHAR = 0x102;
        public const uint WM_GETTEXT = 13;
        public const uint WM_GETTEXTLENGTH = 14;
        public const uint WM_SETTEXT = 12;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        private static IntPtr OpenIW4Process()
        {
            Process[] processesByName = Process.GetProcessesByName("iw4");
            /*Process[] processArray2 = Process.GetProcesses();
            var dediprocess = from process in processArray2
                              where process.MainModule.FileName.IndexOf(Directory.GetCurrentDirectory()) != -1
                              select process.Handle;
            if (dediprocess.Count() >= 1)
                return dediprocess.FirstOrDefault();*/
            foreach (var process in processesByName)
                return process.Handle;
            return IntPtr.Zero;
        }

        public static void PerformConnect(string ip)
        {
            IntPtr ptr = OpenIW4Process();
            if (ptr != IntPtr.Zero)
            {
                _process = ptr;
            }
            else
                Log.Warn("iw4.exe was not found.");
        }

        private static void ReadInputHwnd()
        {
            _hwndInput = ReadIntPtr(_hwndConsoleInpEditLoc);
        }

        private static int ReadInt32(int location)
        {
            int num;
            byte[] lpBuffer = new byte[4];
            ReadProcessMemory(_process, new IntPtr(location), lpBuffer, 4, out num);
            return BitConverter.ToInt32(lpBuffer, 0);
        }

        private static IntPtr ReadIntPtr(int location)
        {
            return new IntPtr(ReadInt32(location));
        }

        private static void ReadLogHwnd()
        {
            _hwndLog = ReadIntPtr(_hwndConsoleLogEditLoc);
        }

        private static void WaitForConsole()
        {
            while (true)
            {
                Thread.Sleep(1);
                ReadInputHwnd();
                ReadLogHwnd();
                if ((_hwndInput != IntPtr.Zero) && (_hwndLog != IntPtr.Zero))
                {
                    return;
                }
            }
        }

        private static string GetEditText(IntPtr hwnd)
        {
            int capacity = SendMessage(hwnd, 14, 0, (StringBuilder)null);
            StringBuilder lParam = new StringBuilder(capacity);
            SendMessage(hwnd, 13, (int)(capacity + 1), lParam);
            return lParam.ToString();
        }

        public static string GetLogContent()
        {
            IntPtr ptr = OpenIW4Process();
            if (ptr != IntPtr.Zero)
            {
                _process = ptr;
                WaitForConsole();
                return GetEditText(_hwndLog);
            }
            else
            {
                Log.Warn("MW2's console has not started");
                return "Dedi is not running";
            }
        }
        #endregion
        #endregion
    }
}
#region Legacy Code
// Udp code from IWNetServer by NTAuthority
/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using aIW;
using System.Threading;
using System.IO;

namespace DM_AdminPlugin2
{
    public class Rcon : AdminPluginBase
    {
        private UdpServer _server;
        private Helper Helper = new Helper();
        private UdpClient _client = new UdpClient();
        private string password;

        public void Start()
        {
            Log.Info("DM_AdminPlugin : Starting RconServer");

            int port = 28900;
            Int32.TryParse(Helper._modCvars.First(i => i.Key == "p_rcon_port").Value, out port);
            password = Helper._modCvars.First(i => i.Key == "p_rcon_password").Value;
            Log.Info(string.Format("DM_AdminPlugin : RconServer : Server is running at UDP port {0} and password is {1}",
                port.ToString(),
                password));

            _server = new UdpServer(UInt16.Parse(port.ToString()), "RconServer");
            _server.PacketReceived += new EventHandler<UdpPacketReceivedEventArgs>(server_ReceivedPacket);
            _server.Start();
        }

        public void server_ReceivedPacket(object sender, UdpPacketReceivedEventArgs e)
        {
            _client.Connect(e.Packet.GetSource());
            if (!string.IsNullOrEmpty(password))
            {
                var packet = e.Packet;
                var reader = packet.GetReader();

                // check is it a OOB packet
                if (reader.ReadBytes(4) == new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })
                {
                    if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == "rcon")
                    {
                        var command = new byte[reader.BaseStream.Length];
                        reader.Read(command, 0, Int32.Parse(reader.BaseStream.Length.ToString()));
                    }
                }
            }
            else
            {
            }
        }
    }

    #region UdpServer
    public class UdpServer
    {
        private ushort _port;
        private string _name;
        private Socket _socket;
        private Thread _thread;

        public event EventHandler<UdpPacketReceivedEventArgs> PacketReceived;

        public UdpServer(ushort port, string name)
        {
            _name = name;
            _port = port;
        }

        public void Start()
        {
            _thread = new Thread(new ThreadStart(Run));
            _thread.Start();
        }

        private void Run()
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, _port);
            //IPEndPoint localEP = new IPEndPoint(IPAddress.Parse("109.237.208.88"), _port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(localEP);

            // 4096 bytes should be enough
            byte[] buffer = new byte[4096];

            while (true)
            {
                try
                {
                    Thread.Sleep(1);

                    // might be unneeded, but just to clean up
                    Array.Clear(buffer, 0, buffer.Length);

                    // create a temp EP
                    EndPoint remoteEP = new IPEndPoint(localEP.Address, localEP.Port);

                    // and wait for reception
                    int bytes = _socket.ReceiveFrom(buffer, ref remoteEP);
                    IPEndPoint remoteIP = (IPEndPoint)remoteEP;

                    // decrypt a possible RSA packet
                    var encrypted = false;

                    var pbuffer = buffer;

                    if (buffer[0] == 0xFE)
                    {
                        var cryptBuffer = new byte[bytes - 1];
                        Array.Copy(buffer, 1, cryptBuffer, 0, cryptBuffer.Length);

                        encrypted = true;
                    }

                    encrypted = true;

                    // trigger packet handler
                    UdpPacket packet = new UdpPacket(pbuffer, bytes, remoteIP, _socket, _name, encrypted);

                    // trigger in remote thread. it could be the 'upacket' is unneeded, but better safe than sorry with delegates
#if NO
                    ThreadPool.QueueUserWorkItem(delegate (object upacket)
                    {
#else
                    object upacket = packet;
#endif

                    try
                    {
                        if (PacketReceived != null)
                        {
                            PacketReceived(this, new UdpPacketReceivedEventArgs((UdpPacket)upacket));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("Error occurred in a processing call in server {0}: {1}", _name, ex.ToString()));
                    }
#if NO
                    }, packet);
#endif

                    Log.Debug(string.Format("Received packet at {0} from {1}:{2}", _name, remoteIP.Address, remoteIP.Port));
                }
                catch (Exception e)
                {
                    Log.Error(string.Format("Error occurred in server {0}: {1}", _name, e.ToString()));
                }
            }
        }
    }
    public class UdpPacketReceivedEventArgs : EventArgs
    {
        public UdpPacketReceivedEventArgs(UdpPacket packet)
        {
            Packet = packet;
        }

        public UdpPacket Packet
        {
            get;
            private set;
        }
    }
    public class UdpPacket : IDisposable
    {
        private MemoryStream _inStream;
        private BinaryReader _inReader;

        private IPEndPoint _ipEndpoint;
        private Socket _socket;

        private string _server;

        private bool _secure;

        public UdpPacket(byte[] input, int length, IPEndPoint ipEndpoint, Socket socket, string server, bool encrypted)
        {
            _inStream = new MemoryStream();
            _inStream.Write(input, 0, length);
            _inStream.Position = 0;

            _inReader = new BinaryReader(_inStream);

            _ipEndpoint = ipEndpoint;
            _socket = socket;

            _server = server;

            _secure = encrypted;
        }

        ~UdpPacket()
        {
            Dispose();
        }

        public void Dispose()
        {
            _inReader.Close();
        }

        public BinaryReader GetReader()
        {
            return _inReader;
        }

        public IPEndPoint GetSource()
        {
            return _ipEndpoint;
        }

        public UdpResponse MakeResponse()
        {
            return new UdpResponse(_ipEndpoint, _socket, _server);
        }

        public bool Secure
        {
            get
            {
                return _secure;
            }
        }

    }
    public class UdpResponse : IDisposable
    {
        private MemoryStream _outStream;
        private BinaryWriter _outWriter;

        private IPEndPoint _ipEndpoint;
        private Socket _socket;

        private string _server;

        public UdpResponse(IPEndPoint ipEndpoint, Socket socket, string server)
        {
            _ipEndpoint = ipEndpoint;
            _socket = socket;
            _server = server;

            _outStream = new MemoryStream();
            _outWriter = new BinaryWriter(_outStream, Encoding.ASCII);
        }

        ~UdpResponse()
        {
            Dispose();
        }

        public void Dispose()
        {
            _outWriter.Close();
        }

        public BinaryWriter GetWriter()
        {
            return _outWriter;
        }

        public void Send()
        {
            byte[] reply = _outStream.ToArray();

            _socket.SendTo(reply, reply.Length, SocketFlags.None, _ipEndpoint);

            _outWriter.Close();

#if DEBUG
            int i = 0;

            foreach (byte data in reply)
            {
                if (i == 0)
                {
                    Console.Write(_server + ": ");
                }

                Console.Write(data.ToString("X2") + " ");

                i++;

                if (i == 16)
                {
                    Console.WriteLine();
                    i = 0;
                }
            }

            Console.WriteLine();
#endif
        }
    }
}
    #endregion
*/
#endregion