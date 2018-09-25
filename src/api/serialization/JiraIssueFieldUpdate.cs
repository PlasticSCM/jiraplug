using System.Collections.Generic;

using Newtonsoft.Json;

namespace JiraPlug.Api.Serialization
{
    public class JiraIssueFieldUpdate
    {
        /* Example, from:
           https://developer.atlassian.com/server/jira/platform/jira-rest-api-example-edit-issues-6291632/
    {
      "fields": {
         "assignee":{
           "name":"harry"
         }
      }
    }
    */

        [JsonProperty(PropertyName = "fields")]
        public Dictionary<string, object> Fields;

        internal static JiraIssueFieldUpdate Create(
            string fieldName,
            string fieldPropertyName,
            string newValue)
        {
            Dictionary<string, object> newFieldValueProperty = new Dictionary<string, object>();
            newFieldValueProperty[fieldPropertyName] = newValue;

            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields[fieldName] = newFieldValueProperty;

            JiraIssueFieldUpdate issueFieldUpdate = new JiraIssueFieldUpdate();
            issueFieldUpdate.Fields = fields;

            return issueFieldUpdate;
        }

        internal static JiraIssueFieldUpdate Create(
            string fieldName,
            string newValue)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();
            fields[fieldName] = newValue;

            JiraIssueFieldUpdate issueFieldUpdate = new JiraIssueFieldUpdate();
            issueFieldUpdate.Fields = fields;

            return issueFieldUpdate;
        }
    }
}
