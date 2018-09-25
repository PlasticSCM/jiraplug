using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JiraPlug
{
    static class Messages
    {
        internal static string BuildRegisterPlugMessage(string name, string type)
        {
            JObject obj = new JObject(
                new JProperty("action", "register"),
                new JProperty("type", type),
                new JProperty("name", name));

            return obj.ToString();
        }

        internal static string BuildLoginMessage(string token)
        {
            JObject obj = new JObject(
                new JProperty("action", "login"),
                new JProperty("key", token));

            return obj.ToString();
        }

        internal static string GetIssueTrackerActionType(string message)
        {
            return ReadProperty(message, "action").ToLower();
        }

        internal static GetIssueUrlMessage ReadGetIssueUrlMessage(string message)
        {
            return JsonConvert.DeserializeObject<GetIssueUrlMessage>(message);
        }

        internal static GetIssueFieldValueMessage ReadGetIssueFieldValueMessage(string message)
        {
            return JsonConvert.DeserializeObject<GetIssueFieldValueMessage>(message);
        }

        internal static SetIssueFieldValueMessage ReadSetIssueFieldValueMessage(string message)
        {
            return JsonConvert.DeserializeObject<SetIssueFieldValueMessage>(message);
        }

        internal static CreateReleaseMessage ReadCreateReleaseMessage(string message)
        {
            return JsonConvert.DeserializeObject<CreateReleaseMessage>(message);
        }

        internal static GetReleaseTasksMessage ReadGetReleaseTasksMessage(string message)
        {
            return JsonConvert.DeserializeObject<GetReleaseTasksMessage>(message);
        }

        internal static string BuildGetIssueUrlResponse(string requestId, string value)
        {
            return new JObject(
                new JProperty("requestId", requestId),
                new JProperty("value", value)).ToString();
        }

        internal static string BuildGetIssueFieldValueResponse(string requestId, string value)
        {
            return new JObject(
                new JProperty("requestId", requestId),
                new JProperty("value", value)).ToString();
        }

        internal static string BuildGetReleaseTasksResponse(string requestId, List<string> tasks)
        {
            return new JObject(
                new JProperty("requestId", requestId),
                new JProperty("tasks", tasks)).ToString();
        }

        internal static string BuildErrorResponse(string requestId, string message)
        {
            return new JObject(
                new JProperty("requestId", requestId),
                new JProperty("error", message)).ToString();
        }

        internal static string BuildSuccessfulResponse(string requestId)
        {
            return new JObject(new JProperty("requestId", requestId)).ToString();
        }

        internal static string GetRequestId(string message)
        {
            return ReadProperty(message, "requestId");
        }

        static string ReadProperty(string message, string name)
        {
            try
            {
                return JObject.Parse(message).Value<string>(name);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class GetIssueUrlMessage
    {
        public string ProjectKey;
        public string TaskNumber;
    }

    public class GetIssueFieldValueMessage
    {
        public string ProjectKey;
        public string TaskNumber;
        public string FieldName;
    }

    public class SetIssueFieldValueMessage
    {
        public string ProjectKey;
        public string TaskNumber;
        public string FieldName;
        public string NewValue;
    }

    public class CreateReleaseMessage
    {
        public string ReleaseName;
        public string ReleaseComment;
        public string ProjectKey;
        public List<string> TaskNumbers;
    }

    public class GetReleaseTasksMessage
    {
        public string ProjectKey;
        public string ReleaseName;
    }
}