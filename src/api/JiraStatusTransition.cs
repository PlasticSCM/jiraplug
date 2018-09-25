using System;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

using log4net;

using JiraPlug.Api.Serialization;

namespace JiraPlug.Api
{
    internal static class JiraStatusTransition
    {
        internal static async Task<JiraTransitionAction> GetTargetTransitionAsync(
            Uri transitionUri,
            string accessToken,
            string newStatusValue)
        {
            JiraTransitionsResult jiraTransitions =
                await GetAvailableTransitions(transitionUri, accessToken);

            foreach (JiraTransition transition in jiraTransitions.Transitions)
            {
                if (IsTargetStatus(transition.To, newStatusValue))
                {
                    JiraTransitionAction result = new JiraTransitionAction();
                    result.Transition = transition;
                    return result;
                }
            }
            return null;
        }

        static async Task<JiraTransitionsResult> GetAvailableTransitions(
            Uri transitionsUri,
            string accessToken)
        {
            mLog.DebugFormat(
                "Retrieving list of transitions: {0}", transitionsUri);

            HttpWebRequest request = JiraRequest.CreateAuthenticatedRequest(
                transitionsUri,
                accessToken);

            string response = await JiraRequest.GetResponseAsync(request);

            return JsonConvert.DeserializeObject<JiraTransitionsResult>(
                response,
                GetIgnoreNullJsonSettings());
        }

        static bool IsTargetStatus(
           JiraTransition.JiraStatus status, string newStatusValue)
        {
            if (status.Name.Equals(
                newStatusValue, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return status.StatusCategory.Name.Equals(
                newStatusValue, StringComparison.InvariantCultureIgnoreCase);
        }

        static JsonSerializerSettings GetIgnoreNullJsonSettings()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            return settings;
        }

        static readonly ILog mLog = LogManager.GetLogger("JiraStatusTransition");
    }
}
