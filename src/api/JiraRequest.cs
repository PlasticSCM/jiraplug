using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using log4net;

using Newtonsoft.Json;

namespace JiraPlug.Api
{
    internal static class JiraRequest
    {
        internal static HttpWebRequest CreateAuthenticatedRequest(Uri uri, string accessToken)
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            request.Headers.Add(string.Format("Authorization: Basic {0}", accessToken));
            request.Headers.Add("ContentType: application/json");
            return request;
        }

        internal static HttpWebRequest CreateJsonPostRequest(
            Uri endpoint,
            string accessToken,
            object jsonBodyObject)
        {
            return CreateWriteRequest(endpoint, accessToken, "POST", jsonBodyObject);
        }

        internal static HttpWebRequest CreateJsonPutRequest(
            Uri endpoint,
            string accessToken,
            object jsonBodyObject)
        {
            return CreateWriteRequest(endpoint, accessToken, "PUT", jsonBodyObject);
        }

        internal static async Task<string> GetResponseAsync(HttpWebRequest request)
        {
            using (HttpWebResponse response =
                await request.GetResponseAsync() as HttpWebResponse)
            {
                string responseMsg = string.Empty;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    responseMsg = reader.ReadToEnd();
                }

                if (response.StatusCode == HttpStatusCode.OK)
                    return responseMsg;

                mLog.ErrorFormat("Request error: {0}", responseMsg);
                return null;
            }
        }

        static HttpWebRequest CreateWriteRequest(
            Uri endpoint,
            string accessToken,
            string method,
            object jsonBodyObject)
        {
            HttpWebRequest request = CreateAuthenticatedRequest(endpoint, accessToken);
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Method = method;

            string body = JsonConvert.SerializeObject(jsonBodyObject);

            using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(body);
                streamWriter.Flush();
            }

            return request;
        }

        static readonly ILog mLog = LogManager.GetLogger("JiraRequest");
    }
}
