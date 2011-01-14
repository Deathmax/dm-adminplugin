using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using aIW;

namespace DM_AdminPlugin2
{
    public class DM_AdminPluginHelper : AdminPluginBase
    {
        #region Variables
        static List<string> _activatorList;
        static List<char> _accesscharList;
        static List<string> _commanddescList;
        static List<string> _commandsyntaxList;
        static List<CommandDelegate> _delegateList;
        static Dictionary<string, string> _modCvars;
        static List<Command> _commandList;
        static List<Admin> _adminList;
        static List<Ban> _banList;
        static List<Level> _levelList;
        #endregion

        #region Get/sets
        public static List<string> activatorList { get { return _activatorList; } set { _activatorList = value; } }
        public static List<char> accesscharList { get { return _accesscharList; } set { _accesscharList = value; } }
        public static List<string> commanddescList { get { return _commanddescList; } set { _commanddescList = value; } }
        public static List<string> commandsyntaxList { get { return _commandsyntaxList; } set { _commandsyntaxList = value; } }
        public static List<CommandDelegate> delegateList { get { return _delegateList; } set { _delegateList = value; } }
        public static Dictionary<string, string> modCvars { get { return _modCvars; } set { _modCvars = value; } }
        public static List<Command> commandList { get { return _commandList; } set { _commandList = value; } }
        public static List<Admin> adminList { get { return _adminList; } set { _adminList = value; } }
        public static List<Ban> banList { get { return _banList; } set { _banList = value; } }
        public static List<Level> levelList { get { return _levelList; } set { _levelList = value; } }
        #endregion

        #region Reset
        static DM_AdminPluginHelper()
        {
            _accesscharList = new List<char>();
            _activatorList = new List<string>();
            _adminList = new List<Admin>();
            _banList = new List<Ban>();
            _commanddescList = new List<string>();
            _commandList = new List<Command>();
            _commandsyntaxList = new List<string>();
            _delegateList = new List<CommandDelegate>();
            _levelList = new List<Level>();
            _modCvars = new Dictionary<string, string>();
        }
        public static void clearAll()
        {
            _accesscharList.Clear();
            _activatorList.Clear();
            _adminList.Clear();
            _banList.Clear();
            _commanddescList.Clear();
            _commandList.Clear();
            _commandsyntaxList.Clear();
            _delegateList.Clear();
            _levelList.Clear();
            _modCvars.Clear();
        }
        #endregion

        #region Flag Checks
        public static bool checkFlag(long xuid, char flag)
        {
            foreach (var admin in _adminList)
            {
                if (admin.XUID == xuid)
                {
                    if (admin.Flags.Contains(flag) || levelFlagCheck(admin.Level, flag) || admin.Flags.Contains('*'))
                        return true;
                    else
                        return false;
                }
                else
                    continue;
            }
            return false;
        }

        public static char[] returnFlag(long xuid)
        {
            foreach (var admin in _adminList)
            {
                if (admin.XUID == xuid)
                    return (new string(admin.Flags) + new string(returnLevelFlag(admin.Level))).ToCharArray();
                else
                    continue;
            }
            return returnLevelFlag(0);
        }

        public static bool levelFlagCheck(int levelnum, char flag)
        {
            foreach (var level in _levelList)
            {
                if (level.LevelNum == levelnum)
                    if (level.Flags.Contains(flag) || level.Flags.Contains('*'))
                        return true;
                    else
                        return false;
                else
                    continue;
            }
            return false;
        }

        public static char[] returnLevelFlag(int levelnum)
        {
            foreach (var level in _levelList)
            {
                if (level.LevelNum == levelnum)
                    return level.Flags;
                else
                    continue;
            }
            return "".ToCharArray();
        }

        public static int returnLevel(long xuid)
        {
            if (string.IsNullOrEmpty(_adminList.Find(i => i.XUID == xuid).Name))
                return 0;
            else
                return _adminList.Find(i => i.XUID == xuid).Level;
        }

        /*public static char[] returnNumFlag(int clientnum)
        {
            foreach (var client in GetClients())
            {
                if (client.ClientNum == clientnum)
                    return (new string(returnFlag(client.GUID)) + new string(returnLevelFlag(returnLevel(client.GUID)))).ToCharArray();
                else
                    continue;
            }
            return "".ToCharArray();
        }*/
        #endregion

        #region Match Client Patterns
        public static List<AdminClient> matchclientpattern(string pattern, List<AdminClient> clients)
        {
            var totalclients = namematching(pattern, clients);
            totalclients.AddRange(xuidmatching(pattern, totalclients).ToArray());
            totalclients.AddRange(clientnummatching(pattern, totalclients).ToArray());
            return totalclients;
        }
        #region Match Client Pattern Function
        private static List<AdminClient> namematching(string pattern, List<AdminClient> clients)
        {
            var returnclients = new List<AdminClient>();
            foreach (var client in clients)
            {
                if (removeQuakeColorCodes(client.Name).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1 ||
                    client.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1)
                    returnclients.Add(client);
                else
                    continue;
            }
            return returnclients;
        }
        private static List<AdminClient> xuidmatching(string pattern, List<AdminClient> clients)
        {
            var returnclients = new List<AdminClient>();
            foreach (var client in clients)
            {
                if (client.GUID.ToString("X15").IndexOf(pattern) != -1 ||
                    client.GUID.ToString().IndexOf(pattern) != -1)
                    returnclients.Add(client);
                else
                    continue;
            }
            return returnclients;
        }
        private static List<AdminClient> clientnummatching(string pattern, List<AdminClient> clients)
        {
            var returnclients = new List<AdminClient>();
            int clientnum = 99;
            if (Int32.TryParse(pattern, out clientnum))
            {
                foreach (var client in clients)
                {
                    if (client.ClientNum == clientnum)
                        returnclients.Add(client);
                }
            }
            return returnclients;
        }
        #endregion
        #endregion

        #region Misc Helper Functions
        public static string removeQuakeColorCodes(string remove)
        {
            string filteredout = "";
            var array = remove.Split('^');
            foreach (string part in array)
            {
                if (part.StartsWith("0") || part.StartsWith("1") || part.StartsWith("2") || part.StartsWith("3") || part.StartsWith("4") || part.StartsWith("5") || part.StartsWith("6") || part.StartsWith("7") || part.StartsWith("8") || part.StartsWith("9"))
                    filteredout += part.Substring(1);
                else
                    filteredout += "^" + part;
            }
            return filteredout.Substring(1);
        }
        public static void Sleep(object sleep)
        {
            Thread.Sleep(int.Parse(sleep.ToString()));
        }
        #endregion

        #region Writing to admin.dat
        public static void addBan(AdminClient bannedclient, AdminClient client, string reason, double banlength, bool warning)
        {
            Ban ban = new Ban();
            ban.Name = bannedclient.Name;
            ban.XUID = bannedclient.GUID;
            ban.Reason = reason;
            ban.BanDate = DateTime.Now;
            ban.Banner = client.Name;
            ban.Expire = DateTime.Now.AddHours(banlength);
            ban.Warning = warning;
            _banList.Add(ban);
            writeBan(ban);
        }
        public static void writeBan(Ban ban)
        {
            string adminFilePath = Directory.GetCurrentDirectory() + @"\plugins\dm_admin\admin.dat";
            if (_modCvars.ContainsKey("pluginFolder"))
                adminFilePath = _modCvars.First(i => i.Key == "pluginFolder").Value;
            if (!File.Exists(adminFilePath))
            {
                Log.Warn("DM_AdminPlugin : admin.dat was not found at " + adminFilePath);
                return;
            }
            TextWriter writer = new StreamWriter(adminFilePath);
            writer.WriteLine(writer.NewLine);
            writer.WriteLine("[ban]");
            writer.WriteLine("name    = {0}", ban.Name);
            writer.WriteLine("guid    = {0}", ban.XUID);
            writer.WriteLine("reason  = {0}", ban.Reason);
            writer.WriteLine("made    = {0}", ban.BanDate);
            writer.WriteLine("expires = {0}", ban.Expire);
            writer.WriteLine("banner  = {0}", ban.Banner);
            writer.WriteLine("warning = {0}", ban.Warning);
            writer.Close();
        }
        #endregion
    }
}
