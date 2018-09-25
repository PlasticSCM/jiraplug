using Newtonsoft.Json;

namespace JiraPlug.Api.Serialization
{
    public class JiraTransitionAction
    {
        [JsonProperty(PropertyName = "transition")]
        public JiraTransition Transition;
    }

    public class JiraTransition
    {
        [JsonProperty(PropertyName = "id")]
        public int Id;
        [JsonProperty(PropertyName = "name")]
        public string Name;
        [JsonProperty(PropertyName = "to")]
        public JiraStatus To;
        public class JiraStatus
        {
            [JsonProperty(PropertyName = "description")]
            public string Description;
            [JsonProperty(PropertyName = "name")]
            public string Name;
            [JsonProperty(PropertyName = "id")]
            public int Id;
            [JsonProperty(PropertyName = "statusCategory")]
            public JiraStatusCategory StatusCategory;

            public class JiraStatusCategory
            {
                [JsonProperty(PropertyName = "id")]
                public int Id;
                [JsonProperty(PropertyName = "key")]
                public string Key;
                [JsonProperty(PropertyName = "name")]
                public string Name;
            }
        }
    }

    public class JiraTransitionsResult
    {
        public string Expand;
        public JiraTransition[] Transitions;
    }
}
