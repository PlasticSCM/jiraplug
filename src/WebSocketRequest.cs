using System;
using System.Threading.Tasks;

using log4net;

namespace JiraPlug
{
    internal class WebSocketRequest
    {
        internal WebSocketRequest(Config config)
        {
            mConfig = config;
            mAccessToken = BuildAccessToken(config.User, config.Password);
        }

        internal async Task<string> ProcessMessage(string message)
        {
            string requestId = Messages.GetRequestId(message);
            string type = string.Empty;
            try
            {
                type = Messages.GetIssueTrackerActionType(message);
                switch (type)
                {
                    case "getissueurl":
                         return ProcessGetIssueUrlMessage(
                            requestId,
                            Messages.ReadGetIssueUrlMessage(message),
                            mConfig.Url);

                    case "getfieldvalue":
                        return await ProcessGetIssueFieldValueMessage(
                            requestId,
                            Messages.ReadGetIssueFieldValueMessage(message),
                            mConfig.Url,
                            mAccessToken);

                    case "setfieldvalue":
                        return await ProcessSetIssueFieldValueMessage(
                            requestId,
                            Messages.ReadSetIssueFieldValueMessage(message),
                            mConfig.Url,
                            mAccessToken);

                    case "createrelease":
                        return ProcessCreateReleaseMessage(
                            requestId,
                            Messages.ReadCreateReleaseMessage(message));

                    case "getreleasetasks":
                        return ProcessGetReleaseTasksMessage(
                            requestId,
                            Messages.ReadGetReleaseTasksMessage(message));

                    default:
                        return Messages.BuildErrorResponse(requestId, string.Format(
                            "The issue tracker plug action '{0}' is not supported", type));
                }
            }
            catch(Exception ex)
            {
                mLog.ErrorFormat("Error processing message {0}: \nMessage:{1}. Error: {2}",
                    type, message, ex.Message);
                mLog.DebugFormat("StackTrace: {0}", ex.StackTrace);
                return Messages.BuildErrorResponse(requestId, ex.Message);
            }
        }

        static string ProcessGetIssueUrlMessage(
            string requestId, GetIssueUrlMessage message, string url)
        {
            string response = JiraRestClient.GetIssuePageUrl(
                url, message.ProjectKey, message.TaskNumber);

            return Messages.BuildGetIssueUrlResponse(requestId, response);
        }

        static async Task<string> ProcessGetIssueFieldValueMessage(
           string requestId,
           GetIssueFieldValueMessage message,
           string url,
           string accessToken)
        {
            string response = await JiraRestClient.GetIssueFieldValueAsync(
                url,
                accessToken,
                message.ProjectKey,
                message.TaskNumber,
                message.FieldName);

            return Messages.BuildGetIssueFieldValueResponse(
                requestId, response);
        }

        static async Task<string> ProcessSetIssueFieldValueMessage(
            string requestId,
            SetIssueFieldValueMessage message,
            string url,
            string accessToken)
        {
            if (string.Compare(message.FieldName, "status", true) != 0)
            {
                await JiraRestClient.TryUpdateFieldAsync(
                    url,
                    accessToken,
                    message.ProjectKey,
                    message.TaskNumber,
                    message.FieldName,
                    message.NewValue);
                return Messages.BuildSuccessfulResponse(requestId);
            }

            await JiraRestClient.PerformStatusTransitionAsync(
                url,
                accessToken,
                message.ProjectKey,
                message.TaskNumber,
                message.NewValue);
            return Messages.BuildSuccessfulResponse(requestId);
        }

        static string ProcessCreateReleaseMessage(string requestId, CreateReleaseMessage message)
        {
            return Messages.BuildErrorResponse(
                requestId, "The 'createRelease' action is not implemented yet");
        }

        static string ProcessGetReleaseTasksMessage(
            string requestId, GetReleaseTasksMessage message)
        {
            return Messages.BuildErrorResponse(
                requestId, "The 'getReleaseTasks' action is not implemented yet");
        }

        internal static string BuildAccessToken(string user, string passOrToken)
        {
            return Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    string.Format("{0}:{1}", user, passOrToken)));
        }

        Config mConfig;
        string mAccessToken;

        static readonly ILog mLog = LogManager.GetLogger("jiraplug");
    }
}
