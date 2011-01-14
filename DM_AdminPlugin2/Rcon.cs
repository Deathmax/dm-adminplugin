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
        private bool init = false;
        private string consoleData = "";
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
                    consoleData = "";
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
            ExecuteCommand(command, new CommandRedirectDelegate(getResponse));
            Log.Info(string.Format("DM_AdminPlugin : Handled rcon {0}", command));
            while (consoleData == string.Empty)
                continue;
            return consoleData;
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
        private void getResponse(string data)
        {
            consoleData = data;
        }
        #endregion
    }
}