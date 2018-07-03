
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Web;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using RestSharp.Deserializers;

namespace AlexaAdvisors
{
    public static class TestHarness
    {
        [FunctionName("TestHarness")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricption_key);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/";

            // Request body

            var body = new StringContent("{\"personA\": {\"age\": 60,\"gender\": \"male\",\"healthRelativeToPeers\": \"same\",\"postcodeProxy\": \"171001411\"}}", Encoding.UTF8, "application/json");
            log.Info(body.ToString());


            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var getAcceptHeader = "application/hal+json;v=1";
            var postResponse = await client.SendAsync(request);
            Thread.Sleep(200);
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var getUrl = postResponse.Headers.Location.AbsoluteUri;
            var getResponse = await client.GetAsync(getUrl);

            while (getResponse.StatusCode == HttpStatusCode.SeeOther)
            {
                Thread.Sleep(100);
                getResponse = await client.GetAsync(getUrl);
            }

            var getResponseBody = await getResponse.Content.ReadAsStringAsync();

            RootObject deserializedJson = JsonConvert.DeserializeObject<RootObject>(getResponseBody);
            double longestvity = deserializedJson.data.lifeExpectancyPersonA;

            return new OkObjectResult("passed");
        }

        public static class Globals
        {
            public static string user_ages = "";
            public static string user_health = "";
            public static string user_gender = "";
            public static string user_postcode = "";
            public static string user_postcode_proxy = "";
            
            public static HttpResponseMessage lifeExpectancyResponse;
            public static string lifeExpectancyResponseString;
            public const string subsricption_key = "82da5702bd954c458366c749fd8a7713";
        }

        public class RootObject
        {
            public string correlationId { get; set; }
            public DataObject data { get; set; }
        }

        public class DataObject
        {
            public double lifeExpectancyPersonA { get; set; }

            public double lifeExpectancyMedianPersonA { get; set; }

            public LifeProbabilities lifeProbabilities { get; set; }
        }

        public class LifeProbabilities
        {
            public int[] timesteps { get; set; }

            public int[] ageA { get; set; }

            public double[] survivalProbA { get; set; }

            public double[] deathAtAgeProbA { get; set; }
        }
    }
}
