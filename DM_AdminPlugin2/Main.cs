using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using aIW;

namespace DM_AdminPlugin2
{
    public class DM_AdminPluginMain : AdminPluginBase
    {
        #region Variables
        private bool _init;
        private DM_AdminPluginMiscServ _Misc = new DM_AdminPluginMiscServ();
        private DM_AdminPluginModServ _Mod = new DM_AdminPluginModServ();
        #endregion

        #region Game events
        public override EventEat OnSay(AdminClient client, string message)
        {
            //only parse !commands atm
            if (message.StartsWith("!"))
                parseCommand(client, message);
            else if (message.StartsWith("/!") || message.StartsWith("/"))
                parseCommand(client, message.Substring(1));
            return EventEat.EatNone;
        }

        public override void OnFrame()
        {
            try
            {
                //initiate plugin options
                if (!_init)
                    initPlugin();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        #endregion

        #region Custom Functions
        private void initPlugin()
        {
            DM_AdminPluginHelper.clearAll();
            Thread load = new Thread(new ThreadStart(loadFiles));
            load.Start();
            Thread misc = new Thread(new ThreadStart(_Misc.loadServ));
            misc.Start();
            _init = true;
        }

        private void loadFiles()
        {
            Thread read = new Thread(new ThreadStart(readAdmin));
            read.Start();
            var runtime = DateTime.Now;
            while (read.ThreadState == ThreadState.Running)
            {
                var difference = DateTime.Now - runtime;
                if (difference.Seconds >= 45)
                    break;
                else
                    continue;
            }
            loadCommands();
        }

        private void parseCommand(AdminClient client, string message)
        {
            var args = message.Split(' ');
            foreach (var command in DM_AdminPluginHelper.commandList)
            {
                if (args[0] == command.Activator || args[0] == command.Activator.Substring(1) || (args[0] + args[1]) == command.Activator || (args[0] + args[1]) == command.Activator.Substring(1))
                {
                    command.delegatefunc(client, message);
                    break;
                }
            }
        }

        private void loadCommands()
        {
            DM_AdminPluginHelper.delegateList.Clear();
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rules));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rules));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Help));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Mod.Kick));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Mod.Ban));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));
            DM_AdminPluginHelper.delegateList.Add(new CommandDelegate(_Misc.Rotation));

            Command command = new Command();
            for (int i = 0; i < DM_AdminPluginHelper.activatorList.Count && i < DM_AdminPluginHelper.delegateList.Count; i++)
            {
                if (!string.IsNullOrEmpty(DM_AdminPluginHelper.activatorList[i]))
                {
                    command.Activator = DM_AdminPluginHelper.activatorList[i];
                    command.ACL = DM_AdminPluginHelper.accesscharList[i];
                    command.Help = DM_AdminPluginHelper.commanddescList[i];
                    command.Syntax = DM_AdminPluginHelper.commandsyntaxList[i];
                    command.delegatefunc = DM_AdminPluginHelper.delegateList[i];
                    DM_AdminPluginHelper.commandList.Add(command);
                }
            }
            Log.Info("DM_AdminPlugin : Loaded " + DM_AdminPluginHelper.commandList.Count + " commands");
        } 
        #endregion

        #region admin.dat Reading
        private void readAdmin()
        {
            try
            {
                string adminFilePath = Directory.GetCurrentDirectory() + @"\plugins\dm_admin\admin.dat";
                if (DM_AdminPluginHelper.modCvars.ContainsKey("pluginFolder"))
                    adminFilePath = DM_AdminPluginHelper.modCvars.First(i => i.Key == "pluginFolder").Value;
                if (!File.Exists(adminFilePath))
                {
                    Log.Warn("DM_AdminPlugin : admin.dat was not found at " + adminFilePath);
                    return;
                }
                var lines = File.ReadAllLines(adminFilePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("#") || lines[i].StartsWith(";") || lines[i].StartsWith("//") || lines[i] == "")
                        continue;

                    if (lines[i].StartsWith("[level]"))
                    {
                        var data = new string[10];
                        int h = 0;
                        while (true)
                        {
                            if ((i + 1) != lines.Length && lines[(i + 1)] != "" && h != data.Length)
                            {
                                data[h++] = lines[++i];
                            }
                            else
                                break;
                        }
                        parseLevel(data);
                    }
                    else if (lines[i].StartsWith("[admin]"))
                    {
                        var data = new string[10];
                        int h = 0;
                        while (true)
                        {
                            if ((i + 1) != lines.Length && lines[(i + 1)] != "" && h != data.Length)
                            {
                                data[h++] = lines[++i];
                            }
                            else
                                break;
                        }
                        parseAdmin(data);
                    }
                    else if (lines[i].StartsWith("[commands]"))
                    {
                        var data = new string[30];
                        int h = 0;
                        while (true)
                        {
                            if ((i + 1) != lines.Length && lines[(i + 1)] != "" && h != data.Length)
                            {
                                data[h++] = lines[++i];
                            }
                            else
                                break;
                        }
                        parseCommand(data);
                    }
                    else if (lines[i].StartsWith("[ban]"))
                    {
                        var data = new string[10];
                        int h = 0;
                        while (true)
                        {
                            if ((i + 1) != lines.Length && lines[(i + 1)] != "" && h != data.Length)
                            {
                                data[h++] = lines[++i];
                            }
                            else
                                break;
                        }
                        parseBan(data);
                    }
                    else if (lines[i].StartsWith("[cvars]"))
                    {
                        var data = new string[50];
                        int h = 0;
                        while (true)
                        {
                            if ((i + 1) != lines.Length && lines[(i + 1)] != "" && h != data.Length)
                            {
                                data[h++] = lines[++i];
                            }
                            else
                                break;
                        }
                        parseCvars(data);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        private void parseLevel(string[] data)
        {
            Level level = new Level();
            foreach (var line in data)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.StartsWith("level"))
                        Int32.TryParse(datValue(line), out level.LevelNum);
                    else if (line.StartsWith("name"))
                        level.Name = datValue(line);
                    else if (line.StartsWith("flags"))
                        level.Flags = datValue(line).ToCharArray();
                }
            }
            Log.Info(string.Format("DM_AdminPlugin : Loaded level {0}, with the name {1} and the flags {2}",
                level.LevelNum.ToString(),
                level.Name,
                new string(level.Flags)));
            DM_AdminPluginHelper.levelList.Add(level);
        }
        private void parseAdmin(string[] data)
        {
            Admin admin = new Admin();
            foreach (var line in data)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.StartsWith("name"))
                        admin.Name = datValue(line);
                    else if (line.StartsWith("guid"))
                        Int64.TryParse(datValue(line), out admin.XUID);
                    else if (line.StartsWith("level"))
                        Int32.TryParse(datValue(line), out admin.Level);
                    else if (line.StartsWith("flags"))
                        admin.Flags = datValue(line).ToCharArray();
                }
            }
            Log.Info(string.Format("DM_AdminPlugin : Loaded admin {0}, with the XUID {1}, level of {2}, and flags of {3}",
                admin.Name,
                admin.XUID.ToString("X15"),
                admin.Level.ToString(),
                new string(admin.Flags)));
            DM_AdminPluginHelper.adminList.Add(admin);
        }
        private void parseCommand(string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (!string.IsNullOrEmpty(data[i]))
                {
                    var strArray2 = data[i].Split(new char[] { '|' });
                    DM_AdminPluginHelper.activatorList.Add(strArray2[0]);
                    DM_AdminPluginHelper.accesscharList.Add(strArray2[1].ToCharArray()[0]);
                    DM_AdminPluginHelper.commanddescList.Add(strArray2[2]);
                    DM_AdminPluginHelper.commandsyntaxList.Add(strArray2[3]);
                }
            }

        }
        private void parseBan(string[] data)
        {
            Ban ban = new Ban();
            foreach (var line in data)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.StartsWith("name"))
                        ban.Name = datValue(line);
                    else if (line.StartsWith("guid"))
                        Int64.TryParse(datValue(line), out ban.XUID);
                    else if (line.StartsWith("reason"))
                        ban.Reason = datValue(line);
                    else if (line.StartsWith("made"))
                        DateTime.TryParse(datValue(line), out ban.BanDate);
                    else if (line.StartsWith("expires"))
                        DateTime.TryParse(datValue(line), out ban.Expire);
                    else if (line.StartsWith("banner"))
                        ban.Banner = datValue(line);
                    else if (line.StartsWith("warning"))
                        Boolean.TryParse(datValue(line), out ban.Warning);
                }
            }
            Log.Info(string.Format("DM_AdminPlugin : Loaded a ban on {0} with the XUID {1}, reason is {2}",
                ban.Name,
                ban.XUID.ToString("X15"),
                ban.Reason));
            DM_AdminPluginHelper.banList.Add(ban);
        }
        private void parseCvars(string[] data)
        {
            string key = "";
            string value = "";
            foreach (var line in data)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var temp = line.Split(' ');
                    key = temp[0];
                    value = datValue(line);
                    Log.Info(string.Format("DM_AdminPlugin : Loaded cvar {0} with the value {1}", key, value));
                    DM_AdminPluginHelper.modCvars.Add(key, value);
                }
            }
        }
        private string datValue(string line)
        {
            var temp = line.Split(' ');
            for (int i = 0; i < temp.Length; i++)
            {
                if (temp[i] == "=")
                {
                    if ((i + 1) != temp.Length)
                    {
                        i = i + 1;
                        string complete = "";
                        for (int h = i; h < temp.Length; h++)
                        {
                            complete += temp[h] + " ";
                        }
                        return complete.TrimEnd(new char[] { ' ' });
                    }
                }
            }
            return "";
        }
        #endregion
    }

    #region Plugin Structs
    public delegate void CommandDelegate(AdminClient client, string message);
    public struct Command
    {
        public string Activator;
        public char ACL;
        public string Help;
        public string Syntax;
        public CommandDelegate delegatefunc;

        public Command(string activator, char acl, string help, string syntax, CommandDelegate func)
        {
            Activator = activator;
            ACL = acl;
            Help = help;
            Syntax = syntax;
            delegatefunc = func;
        }
    }
    public struct Admin
    {
        public string Name;
        public long XUID;
        public int Level;
        public char[] Flags;

        public Admin(string name, long xuid, int level, char[] flags)
        {
            Name = name;
            XUID = xuid;
            Level = level;
            Flags = flags;
        }
    }
    public struct Ban
    {
        public string Name;
        public long XUID;
        public string Reason;
        public DateTime BanDate;
        public DateTime Expire;
        public string Banner;
        public bool Warning;

        public Ban(string name, long xuid, string reason, DateTime bandate, DateTime expire, string banner, bool warning)
        {
            Name = name; 
            XUID = xuid;
            Reason = reason;
            BanDate = bandate;
            Expire = expire;
            Banner = banner;
            Warning = warning;
        }
    }
    public struct Level
    {
        public int LevelNum;
        public string Name;
        public char[] Flags;

        public Level(int levelnum, string name, char[] flags)
        {
            LevelNum = levelnum;
            Name = name;
            Flags = flags;
        }
    }
    #endregion
}
