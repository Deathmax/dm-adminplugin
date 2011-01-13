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