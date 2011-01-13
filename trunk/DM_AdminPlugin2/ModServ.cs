using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using aIW;

namespace DM_AdminPlugin2
{
    public class DM_AdminPluginModServ : AdminPluginBase
    {
        #region Variables
        public Dictionary<string, string> _accessList = new Dictionary<string, string>();
        #endregion

        #region Commands
        public void PList(AdminClient client, string message)
        {
            if (DM_AdminPluginHelper.checkFlag(client.GUID, 's'))
            {
                var clients = GetClients();
                var args = message.Split(' ');
                SayTo(client, "ModServ", "Clientnum - Name - XUID - Ping - Score");
                int i = 0;
                for (Int32.TryParse(args[1], out i); i < (i + 2) && i < clients.Count; i++)
                {
                    SayTo(client, "ModServ", string.Format("{1} - {2} ^7- {3} - {4} - {5}",
                        clients[i].ClientNum,
                        clients[i].Name,
                        clients[i].GUID.ToString("X15"),
                        clients[i].Ping,
                        clients[i].Score));
                }
            }
            else
                SayTo(client, "ModServ", "You don't have access");
        }
        public void Kick(AdminClient client, string message)
        {
            if (DM_AdminPluginHelper.checkFlag(client.GUID, 'K'))
            {
                var args = message.Split(' ');
                var foundclients = DM_AdminPluginHelper.matchclientpattern(args[1], GetClients());
                string reason = "";
                for (int i = 2; i < args.Length; i++)
                {
                    reason += args[i] + " ";
                }
                if (foundclients.Count == 0)
                    SayTo(client, "ModServ", "No clients found matching such a name/XUID/clientnum");
                else if (foundclients.Count == 1)
                {
                    if (DM_AdminPluginHelper.returnLevel(foundclients[0].GUID) >= DM_AdminPluginHelper.returnLevel(client.GUID) &&
                        !DM_AdminPluginHelper.checkFlag(foundclients[0].GUID, '%'))
                    {
                        ExecuteCommand("clientkick " + foundclients[0].ClientNum.ToString() + " " + reason);
                        Log.Info(string.Format("DM_AdminPlugin : ModServ : Kicked {0} with XUID {1} by {2} for {3}",
                            foundclients[0].Name, foundclients[0].GUID.ToString("X15"),
                            client.Name, reason));
                        SayTo(client, "ModServ", "Kicked " + foundclients[0].Name);
                    }
                    else
                    {
                        Log.Info(string.Format("DM_AdminPlugin : ModServ : {0} unsuccessfully attempted to kick {1}",
    client.Name, foundclients[0].Name));
                        SayTo(client, "ModServ", "No rights to kick the client (" + foundclients[0].Name + "^7)");
                    }
                }
                else if (foundclients.Count > 1)
                {
                    SayTo(client, "ModServ", "More than 1 client matches that pattern, please kick the clientnum from below");
                    foreach (var client2 in foundclients)
                    {
                        SayTo(client, "ModServ", client2.ClientNum.ToString() + " " + client2.Name);
                    }
                }
            }
            else
                SayTo(client, "ModServ", "You don't have access");
        }
        public void Ban(AdminClient client, string message)
        {
            if (DM_AdminPluginHelper.checkFlag(client.GUID, 'B'))
            {
                var args = message.Split(' ');
                var foundclients = DM_AdminPluginHelper.matchclientpattern(args[1], GetClients());
                double banlength = 1;
                string reason = "";
                for (int i = 2; i < (args.Length - 1); i++)
                {
                    reason += args[i] + " ";
                }
                if (!Double.TryParse(args[args.Length - 1], out banlength))
                    reason += args[args.Length - 1];
                if (foundclients.Count == 0)
                    SayTo(client, "ModServ", "No clients found matching such a name/XUID/clientnum");
                else if (foundclients.Count == 1)
                {
                    if (DM_AdminPluginHelper.returnLevel(foundclients[0].GUID) >= DM_AdminPluginHelper.returnLevel(client.GUID) &&
                        !DM_AdminPluginHelper.checkFlag(foundclients[0].GUID, '^'))
                    {
                        ExecuteCommand("clientkick " + foundclients[0].ClientNum.ToString() + " " + reason);
                        Log.Info(string.Format("DM_AdminPlugin : ModServ : Banned {0} with XUID {1} by {2} for {3} for {4} hours",
                            foundclients[0].Name, foundclients[0].GUID.ToString("X15"),
                            client.Name, reason, banlength));
                        SayTo(client, "ModServ", "Banned " + foundclients[0].Name + "^7 for ");
                    }
                    else
                    {
                        Log.Info(string.Format("DM_AdminPlugin : ModServ : {0} unsuccessfully attempted to ban {1}",
    client.Name, foundclients[0].Name));
                        SayTo(client, "ModServ", "No rights to ban the client (" + foundclients[0].Name + "^7)");
                    }
                }
                else if (foundclients.Count > 1)
                {
                    SayTo(client, "ModServ", "More than 1 client matches that pattern, please ban the clientnum from below");
                    foreach (var client2 in foundclients)
                    {
                        SayTo(client, "ModServ", client2.ClientNum.ToString() + " " + client2.Name);
                    }
                }
            }
            else
                SayTo(client, "ModServ", "You don't have access");
        }
        #endregion
    }
}
