using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using aIW;

namespace DM_AdminPlugin2
{
    public class DM_AdminPluginMiscServ : AdminPluginBase
    {
        #region Variables
        private List<double> _timeBetweenMessages = new List<double>();
        private List<string> _messages = new List<string>();
        private List<string> _rules = new List<string>();
        private DateTime _lastRun;
        private int _currentMessage;
        private bool _init;
        Random random = new Random();
        #endregion

        #region Commands
        public void Rotation(AdminClient client, string message)
        {
            SayTo(client, "MiscServ", "Current server sv_mapRotation is : " + GetDvar("sv_maprotation"));
        }

        public void Rules(AdminClient client, string message)
        {
            if (message == "!rules all")
            {
                if (DM_AdminPluginHelper.checkFlag(client.GUID, 'G'))
                {
                    Thread thread = new Thread(new ParameterizedThreadStart(DM_AdminPluginHelper.Sleep));
                    foreach (var line in _rules)
                    {
                        var temp = line.Split('{');
                        foreach (var line2 in temp)
                        {
                            SayTo(client, "MiscServ", line2);
                            Log.Info("DM_AdminPlugin : MiscServ : Said : " + line2 + " : To : " + client.Name);
                        }
                        thread.Start();
                        while (thread.ThreadState == System.Threading.ThreadState.Running)
                            continue;
                    }
                }
                else
                    SayTo(client, "MiscServ", "You don't have access");
            }
            else
            {
                if (DM_AdminPluginHelper.checkFlag(client.GUID, 'g'))
                {
                    foreach (var line in _rules)
                    {
                        var temp = line.Split('{');
                        foreach (var line2 in temp)
                        {
                            SayAll("MiscServ", line2);
                            Log.Info("DM_AdminPlugin : MiscServ : Said : " + line2);
                        }
                    }
                }
                else
                    SayTo(client, "MiscServ", "You don't have access");
            }
        }

        public void Help(AdminClient client, string message)
        {
            var flags = DM_AdminPluginHelper.returnFlag(client.GUID);
            string helpstring = "";
            if (message == "!help")
            {
                for (int i = 0; i < DM_AdminPluginHelper.commandList.Count; i++)
                {
                    if (flags.Contains(DM_AdminPluginHelper.commandList[i].ACL) || flags.Contains('*'))
                        helpstring += DM_AdminPluginHelper.commandList[i].Activator + " ";
                    else
                        continue;
                }
                SayTo(client, "MiscServ", "Available commands are:");
                SayTo(client, "MiscServ", helpstring);
            }
            else
            {
                bool found = false;
                var args = message.Split(' ');
                foreach (var entry in DM_AdminPluginHelper.commandList)
                {
                    if (args[1] == entry.Activator || args[1] == entry.Activator.Substring(1))
                    {
                        if (flags.Contains(entry.ACL))
                        {
                            SayTo(client, "MiscServ", entry.Activator);
                            SayTo(client, "MiscServ", entry.Help);
                            SayTo(client, "MiscServ", entry.Syntax);
                            SayTo(client, "MiscServ", "Access Flag: " + entry.ACL);
                            found = true;
                        }
                    }
                }
                if (!found)
                    SayTo(client, "MiscServ", "No such command or not enough access");
            }
        }
        #endregion

        #region Game Events
        public override void OnFrame()
        {
            try
            {
                handleMessages();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        #endregion

        private void handleMessages()
        {
            if (_init)
            {
                if ((DateTime.Now - _lastRun).TotalSeconds >= _timeBetweenMessages[_currentMessage])
                {
                    var temp = _messages[_currentMessage].Split('{');
                    foreach (var line in temp)
                    {
                        SayAll("MiscServ", _messages[_currentMessage]);
                        Log.Info("DM_AdminPlugin : MiscServ : Said : " + _messages[_currentMessage]);
                    }
                    ++_currentMessage;
                    _lastRun = DateTime.Now;
                }
            }
        }

        public void loadServ()
        {
            _lastRun = DateTime.Now;
            var filePath = Directory.GetCurrentDirectory() +  @"\plugins\dm_admin\";
            if (DM_AdminPluginHelper.modCvars.ContainsKey("pluginFolder"))
            {
                filePath = DM_AdminPluginHelper.modCvars.First(i => i.Key == "pluginFolder").Value;
            }
            if (!File.Exists(filePath + "messages.txt") || !File.Exists(filePath + "rules.txt"))
            {
                Log.Warn("DM_AdminPlugin : MiscServ : messages.txt and/or rules.txt not found at " + filePath);
                return;
            }

            var messagestemp = File.ReadAllLines(filePath + "messages.txt");
            for (int i = 0; i < messagestemp.Length; i++)
            {
                _messages.Add(messagestemp[i]);
                _timeBetweenMessages.Add(Double.Parse(messagestemp[++i]));
            }
            Log.Info("DM_AdminPlugin : MiscServ : Loaded " + _messages.Count.ToString() + " messages.");
            var rulestemp = File.ReadAllLines(filePath + "rules.txt");
            for (int i = 0; i < rulestemp.Length; i++)
            {
                _rules.Add(rulestemp[i]);
            }
            Log.Info("DM_AdminPlugin : MiscServ : Loaded " + _rules.Count.ToString() + " rule lines.");
            _init = true;
        }
    }
}
