using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using log4net;
using log4net.Config;

namespace JiraPlug
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                PlugArguments plugArgs = new PlugArguments(args);

                bool bValidArgs = plugArgs.Parse();

                ConfigureLogging(plugArgs.BotName);

                mLog.InfoFormat("JiraPlug [{0}] started. Version [{1}]",
                    plugArgs.BotName,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                ConfigureServicePoint();

                string argsStr = args == null ? string.Empty : string.Join(" ", args);
                mLog.DebugFormat("Args: [{0}]. Are valid args?: [{1}]", argsStr, bValidArgs);

                if (!bValidArgs || plugArgs.ShowUsage)
                {
                    PrintUsage();
                    return 0;
                }

                CheckArguments(plugArgs);

                Config config = ReadConfigFromFile(plugArgs.ConfigFilePath);

                CheckConnection(config.Url, config.User, config.Password);

                LaunchJiraPlug(plugArgs.WebSocketUrl, config,
                    plugArgs.BotName, plugArgs.ApiKey);

                return 0;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mLog.ErrorFormat("Error: {0}", e.Message);
                mLog.DebugFormat("StackTrace: {0}", e.StackTrace);
                return 1;
            }
        }

        static void ConfigureServicePoint()
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 500;
            // .NET 4.0 only includes SecurityProtocolType for SSLv3 and TLSv1.0,
            //   so we need to use the raw flag value
            ServicePointManager.SecurityProtocol |=
                (SecurityProtocolType)768 | // TLSv1.1
                (SecurityProtocolType)3072; // TLSv1.2
        }

        static void CheckConnection(string url, string user, string password)
        {
            if (JiraRestClient.CheckConnection(url, user, password))
                return;

            throw new Exception(string.Format(
                "Unable to contact Jira Server [{0}] using the specified credentials for user [{1}]. " + 
                "Are you using Jira Cloud? If so, did you properly configure a Jira API token for user [{1}] " +
                "and set it in the JiraPlug password/API Token configuration field?",
                url, user));
        }

        static void LaunchJiraPlug(
            string serverUrl,
            Config config,
            string plugName,
            string apiKey)
        {
            WebSocketClient ws = new WebSocketClient(
                serverUrl,
                "issueTrackerPlug",
                plugName,
                apiKey,
                new WebSocketRequest(config).ProcessMessage);

            ws.ConnectWithRetries();

            Task.Delay(-1).Wait();
        }

        static void ConfigureLogging(string plugName)
        {
            if (string.IsNullOrEmpty(plugName))
                plugName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

            try
            {
                string log4netpath = LogConfig.GetLogConfigFile();
                log4net.GlobalContext.Properties["Name"] = plugName;
                XmlConfigurator.Configure(new FileInfo(log4netpath));
            }
            catch
            {
                //it failed configuring the logging info; nothing to do.
            }
        }

        static void CheckArguments(PlugArguments plugArgs)
        {
            CheckAgumentIsNotEmpty(
                "Plastic web socket url endpoint",
                plugArgs.WebSocketUrl,
                "web socket url",
                "--server wss://blackmore:7111/plug");

            CheckAgumentIsNotEmpty("name for this bot", plugArgs.BotName, "name", "--name jira");
            CheckAgumentIsNotEmpty("connection API key", plugArgs.ApiKey, "api key",
                "--apikey 014B6147A6391E9F4F9AE67501ED690DC2D814FECBA0C1687D016575D4673EE3");
            CheckAgumentIsNotEmpty("JSON config file", plugArgs.ConfigFilePath, "file path",
                "--config jira-config.conf");
        }

        static Config ReadConfigFromFile(string file)
        {
            try
            {
                string fileContent = System.IO.File.ReadAllText(file);
                Config result = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(fileContent);

                if (result == null)
                    throw new Exception(string.Format(
                        "Config file {0} is not valid", file));

                CheckFieldIsNotEmpty("serverUrl", result.Url);
                CheckFieldIsNotEmpty("user", result.User);
                CheckFieldIsNotEmpty("password", result.Password);

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("The config cannot be loaded. Error: " + e.Message);
            }
        }

        static void CheckAgumentIsNotEmpty(
            string fielName, string fieldValue, string type, string example)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;
            string message = string.Format("jira plug can't start without specifying a {0}.{1}" +
                "Please type a valid {2}. Example:  \"{3}\"",
                fielName, Environment.NewLine, type, example);
            throw new Exception(message);
        }

        static void CheckFieldIsNotEmpty(string fieldName, string fieldValue)
        {
            if (!string.IsNullOrEmpty(fieldValue))
                return;

            throw BuildFieldNotDefinedException(fieldName);
        }

        static Exception BuildFieldNotDefinedException(string fieldName)
        {
            throw new Exception(string.Format(
                "The field '{0}' must be defined in the config", fieldName));
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tjiraplug.exe --server <WEB_SOCKET_URL> --config <JSON_CONFIG_FILE_PATH>");
            Console.WriteLine("\t             --apikey <WEB_SOCKET_CONN_KEY> --name <PLUG_NAME>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\tjiraplug.exe --server wss://blackmore:7111/plug --config jira-config.conf ");
            Console.WriteLine("\t             --apikey x2fjk28fda --name jira");
            Console.WriteLine();

        }

        static readonly ILog mLog = LogManager.GetLogger("jiraplug");
    }
}
