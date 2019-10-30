using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using log4net;

using JiraPlug.Api;
using JiraPlug.Api.Serialization;

namespace JiraPlug
{
    internal static class JiraRestClient
    {
        internal static string GetIssuePageUrl(
            string url,
            string projectKey,
            string issueNumber)
        {
            string issueKey = GetIssueKey(projectKey, issueNumber);
            string browseUriPath = string.Format(BROWSE_URI, issueKey);

            return string.Format("{0}/{1}", url, browseUriPath);
        }

        internal static async Task<string> GetIssueFieldValueAsync(
            string url,
            string accessToken,
            string projectKey,
            string issueNumber,
            string fieldName)
        {
            string issueKey = GetIssueKey(projectKey, issueNumber);

            Uri restUri = GetBaseRestUri(url);
            string queryUri = string.Format(ISSUE_READ_FIELD_URI, issueKey, fieldName);

            Uri uri = new Uri(restUri, queryUri);

            try
            {
                HttpWebRequest request = JiraRequest.CreateAuthenticatedRequest(
                    uri,
                    accessToken);

                string response = await JiraRequest.GetResponseAsync(request);

                if (string.IsNullOrEmpty(response))
                    return null;

                return ReadJiraFieldValue(response, fieldName, WELL_KNOWN_STRING_FIELDS);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(
                    "Could not get the issue field [{0}] from issue key [{1}]. " +
                    "Jira server:[{2}]. Error:{3}",
                    fieldName,
                    issueKey,
                    url,
                    ex.Message));
            }
        }

        internal static bool CheckConnection(string url, string user, string passwordOrToken)
        {
            Uri restUri = GetBaseRestUri(url);
            Uri uri = new Uri(restUri, MYSELF_INFO_FOR_CHECK_CONN_URI);
            string accessToken = WebSocketRequest.BuildAccessToken(user, passwordOrToken);

            try
            {
                HttpWebRequest request = JiraRequest.CreateAuthenticatedRequest(
                    uri,
                    accessToken);

                string response =  JiraRequest.GetResponseAsync(request).Result;

                if (string.IsNullOrEmpty(response))
                {
                    mLog.ErrorFormat(
                        "Check Connection: Could not get info about the configured user." +
                        "URL: [{0}]. User: [{1}]. Null response.",
                        uri.AbsoluteUri, 
                        user);

                    return false;
                }

                mLog.DebugFormat(
                    "Check Connection: OK. URL: [{0}]. User: [{1}]",
                    uri.AbsoluteUri,
                    user);

                return true;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;

                mLog.ErrorFormat(
                    "Check Connection: Could not get info about the configured user." +
                    "URL: [{0}]. User: [{1}]. Message:[{2}]",
                    uri.AbsoluteUri,
                    user,
                    ex.Message);

                return false;
            }
        }

        internal static async Task TryUpdateFieldAsync(
            string url,
            string accessToken,
            string projectKey,
            string issueNumber,
            string fieldName,
            string newValue)
        {
            string issueKey = GetIssueKey(projectKey, issueNumber);
            string issueUriPath = string.Format(ISSUE_URI, issueKey);

            Uri uri = new Uri(GetBaseRestUri(url), issueUriPath);

            JiraIssueFieldUpdate issueToUpdate = JiraIssueFieldUpdate.Create(
                fieldName,
                newValue);

            if (HasToAddNameFieldToJsonPath(fieldName, WELL_KNOWN_STRING_FIELDS))
                issueToUpdate = JiraIssueFieldUpdate.Create(fieldName, NAME_PROPERTY, newValue);

            try
            {
                HttpWebRequest request = JiraRequest.CreateJsonPutRequest(
                    uri, accessToken, issueToUpdate);

                await JiraRequest.GetResponseAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(
                    "Could not update issue field [{0}] from issue key [{1}]. " +
                    "Jira server:[{2}]. Error:{3}",
                    fieldName,
                    issueKey,
                    url,
                    ex.Message));
            }
        }

        internal static async Task PerformStatusTransitionAsync(
            string url,
            string accessToken,
            string projectKey,
            string issueNumber,
            string newStatusValue)
        {
            string issueKey = GetIssueKey(projectKey, issueNumber);
            string queryPath = string.Format(TRANSITION_URI, issueKey);

            Uri transitionUri = new Uri(GetBaseRestUri(url), queryPath);

            JiraTransitionAction transitionAction =
                await JiraStatusTransition.GetTargetTransitionAsync(
                    transitionUri,
                    accessToken,
                    newStatusValue);

            if (transitionAction == null)
                throw new Exception(string.Format(
                    "Unable to find a suitable transition to '{0}' for issue: [{1}]",
                    newStatusValue, issueKey));

            try
            {
                HttpWebRequest request = JiraRequest.CreateJsonPostRequest(
                    transitionUri, accessToken, transitionAction);

                await JiraRequest.GetResponseAsync(request);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(
                    "Unable to post an {0} transition to issue {1}: {2}",
                    transitionAction.Transition.Name, issueKey, e.Message));
            }
        }

        static string ReadJiraFieldValue(string response, string fieldName, string[] wellKnownFields)
        {
            List<string> fieldPath = new List<string>() { FIELDS_PROPERY, fieldName };

            try
            {
                JObject obj = JObject.Parse(response);
                if (HasToAddNameFieldToJsonPath(
                    fieldName, wellKnownFields))
                {
                    return obj[FIELDS_PROPERY][fieldName][NAME_PROPERTY].Value<string>();
                }

                return obj[FIELDS_PROPERY][fieldName].Value<string>();
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "Error reading Json property [{0}]:{1}{2}{2}" +
                    "Response contents:[{3}]",
                    fieldName,
                    ex.Message,
                    Environment.NewLine,
                    response);
                throw;
            }
        }

        static bool HasToAddNameFieldToJsonPath(
            string requestedFieldName, string[] stringTypeJiraFields)
        {
            foreach (string strField in stringTypeJiraFields)
                if (requestedFieldName.Equals(strField, StringComparison.InvariantCultureIgnoreCase))
                    return false;

            return true;
        }

        static Uri GetBaseRestUri(string jiraUrl)
        {
            string hostUrl = jiraUrl;
            string restUri = BASE_URI_REST;

            // If JIRA is not running at the root level of the HOST, appending the 
            // REST endpoint could overwrite the JIRA path.
            //
            // Example:
            //   HOST_KEY -> https://myinternalserver/private/jira
            //   REST_URL_KEY -> /rest/v2
            //   Result -> https//myinternalserver/rest/v2
            //
            // This is solved with correct slashes placement:
            // https://stackoverflow.com/questions/1795917/path-part-gets-overwritten-when-merging-two-uris
            if (!hostUrl.EndsWith("/"))
                hostUrl += "/";

            if (restUri.StartsWith("/"))
                restUri = restUri.Substring(1);

            return new Uri(new Uri(hostUrl), restUri);
        }

        static string GetIssueKey(string projectKey, string issueNumber)
        {
            return projectKey + "-" + issueNumber;
        }

        const string NAME_PROPERTY = "name";
        const string FIELDS_PROPERY = "fields";
        const string ISSUE_READ_FIELD_URI = "issue/{0}?" + FIELDS_PROPERY +"={1}";
        const string ISSUE_URI = "issue/{0}";
        const string MYSELF_INFO_FOR_CHECK_CONN_URI = "myself";
        const string BROWSE_URI = "browse/{0}";
        const string TRANSITION_URI = "issue/{0}/transitions";
        const string BASE_URI_REST = "/rest/api/2/";

        static string[] WELL_KNOWN_STRING_FIELDS = new string[] { "summary", "description" };

        static readonly ILog mLog = LogManager.GetLogger("JiraRestClient");
    }
}