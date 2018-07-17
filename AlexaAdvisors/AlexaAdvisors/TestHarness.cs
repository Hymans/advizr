
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
using System.Collections.Generic;
using System.Diagnostics;

namespace AlexaAdvisors
{
    public static class TestHarness
    {
        [FunctionName("TestHarness")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            /*var queryString = HttpUtility.ParseQueryString(string.Empty);

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
            double longestvity = deserializedJson.data.lifeExpectancyPersonA;*/

            // Set value for header and URL
            /*var getURL = "/v1/devices/" + "amzn1.ask.device.AFY6M3NYK2GSAM2Y3NS6XKDKKCOAEHWRUMWNI2PNSA3VSVHVUQWDYPXIDPQLFN2LI3OO6G23LUTKR7RKJAHZLVMUVJXPHPNPTUAEVTH2CFUVCCYHZMTQ2YGUXV6QGEFKQRKBEYEKKOKG4BHE7GBUGTAPB4ZZSG5EGJQR3HQILHY6AZQ2EBJ3S" + " / settings/address/countryAndPostalCode";
            var authorization = "Bearer " + accessToken;
            var apiEndpoint = "https://api.amazonalexa.co.uk";

            log.Info("get URL :" + getURL);
            log.Info("Authorization :" + authorization);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            client.DefaultRequestHeaders.Add("Authorization", authorization);
            var uri = apiEndpoint + getURL;
            var getResponse = await client.GetAsync(uri);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            Postcode deserializedJson = JsonConvert.DeserializeObject<Postcode>(getResponseBody);
            var postcode = deserializedJson.postalCode;*/


            //TEST LifeExpectancy
            

            var watch = Stopwatch.StartNew();
            // Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "ab05b31f579c4d92aa06bd61d4186b64");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/";

            // Post request body
            var body = new StringContent("{\"personA\": {\"age\": 60,\"gender\": \"male\",\"healthRelativeToPeers\": \"same\",\"postcodeProxy\": \"171001411\"}}", Encoding.UTF8, "application/json");
            log.Info(body.ToString());

            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.SendAsync(request);

            var getAcceptHeader = "application/hal+json;v=1";
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var getUrl = postResponse.Headers.Location.AbsoluteUri;
            Thread.Sleep(100);
            var getResponse = await client.GetAsync(getUrl);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            RootLifeExpectancy deserializedJson = JsonConvert.DeserializeObject<RootLifeExpectancy>(getResponseBody);

            while (deserializedJson.Status == "InProgress")
            {
                Thread.Sleep(100);
                getResponse = await client.GetAsync(getUrl);
                getResponseBody = await getResponse.Content.ReadAsStringAsync();
                deserializedJson = JsonConvert.DeserializeObject<RootLifeExpectancy>(getResponseBody);

            }

            watch.Stop();
            log.Info("Time used for life expectancy API: " + watch.ElapsedMilliseconds + " ms");



            //TEST Drawdown
            // Request headers
            /*
            var watch = Stopwatch.StartNew();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "9a511111d99a41f5b298ed8f4f0e9ac3");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/decumulationrundowndev/assess";

            // Post request body
            var body = new StringContent("{\"memberData\": {\"personA\": {\"age\": 60,\"gender\": \"male\",\"healthRelativeToPeers\": \"same\",\"postcodeProxy\": \"171001411\"}},\"potData\": {\"potSizePounds\": 100000,\"potStrategy\": {\"assetClassMapping\": {\"ukEquity\": [0.5],\"cash\": [0.5]}}},\"drawdownIncome\": {\"regularWithdrawal\": {\"amount\": [5000],\"increaseData\": {\"increaseType\": \"rpi\",\"increaseRate\": 0.01}}}}", Encoding.UTF8, "application/json");
            log.Info(body.ToString());

            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            watch.Stop();
            log.Info("Time before post response: " + watch.ElapsedMilliseconds);
            var watch2 = Stopwatch.StartNew();
            var postResponse = await client.SendAsync(request);

            watch2.Stop();
            log.Info("Time after post response: " + watch2.ElapsedMilliseconds);

            var getAcceptHeader = "application/hal+json;v=1";
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var getUrl = postResponse.Headers.Location.AbsoluteUri;
            var getResponse = await client.GetAsync(getUrl);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            

            RootDrawDown deserializedJson = JsonConvert.DeserializeObject<RootDrawDown>(getResponseBody);



            while (deserializedJson.Status == "InProgress")
            {
                Thread.Sleep(100);
                getResponse = await client.GetAsync(getUrl);
                getResponseBody = await getResponse.Content.ReadAsStringAsync();
                deserializedJson = JsonConvert.DeserializeObject<RootDrawDown>(getResponseBody);
            }

            var test = deserializedJson.Data.LifeExpectancyOutput.LifeExpectancyPersonA;

          */

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

        public class Postcode
        {
            public string countryCode { get; set; }
            public string postalCode { get; set; }
        }

        public class LifeExpectancySlots
        {
            public SlotName gender;
            public SlotName health;
            public SlotNameAge age;
        }

        public class SlotName
        {
            public string synonym { get; set; }
            public string resolved { get; set; }
            public bool isValidated { get; set; }
        }

        public class SlotNameAge
        {
            public double synonym { get; set; }
            public double resolved { get; set; }
            public bool isValidated { get; set; }
        }


        public partial class RootDrawDown
        {
            [JsonProperty("correlationId")]
            public string CorrelationId { get; set; }

            [JsonProperty("data")]
            public DrawdownResult Data { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("_links")]
            public Links Links { get; set; }
        }

        public partial class DrawdownResult
        {
            [JsonProperty("lifeExpectancyOutput")]
            public LifeExpectancyOutput LifeExpectancyOutput { get; set; }

            [JsonProperty("timesteps")]
            public long[] Timesteps { get; set; }

            [JsonProperty("contributionsPaidReal")]
            public Dictionary<string, double[]> ContributionsPaidReal { get; set; }

            [JsonProperty("fundAmountReal")]
            public Dictionary<string, double[]> FundAmountReal { get; set; }

            [JsonProperty("potentialAnnuityIncomeFromFundReal")]
            public Dictionary<string, double[]> PotentialAnnuityIncomeFromFundReal { get; set; }

            [JsonProperty("fundAmountPostLegacyReal")]
            public Dictionary<string, double[]> FundAmountPostLegacyReal { get; set; }

            [JsonProperty("potentialAnnuityIncomePostLegacyReal")]
            public Dictionary<string, double[]> PotentialAnnuityIncomePostLegacyReal { get; set; }

            [JsonProperty("ppnFundWithdrawalReal")]
            public Dictionary<string, double[]> PpnFundWithdrawalReal { get; set; }

            [JsonProperty("regularWithdrawalReal")]
            public Dictionary<string, double[]> RegularWithdrawalReal { get; set; }

            [JsonProperty("adhocWithdrawalReal")]
            public Dictionary<string, double[]> AdhocWithdrawalReal { get; set; }

            [JsonProperty("partialAnnuityIncomeReal")]
            public Dictionary<string, double[]> PartialAnnuityIncomeReal { get; set; }

            [JsonProperty("totalWithdrawalsAndIncomeReal")]
            public Dictionary<string, double[]> TotalWithdrawalsAndIncomeReal { get; set; }

            [JsonProperty("probFundGreaterThanZero")]
            public double[] ProbFundGreaterThanZero { get; set; }

            [JsonProperty("probFundGreaterThanLegacy")]
            public double[] ProbFundGreaterThanLegacy { get; set; }

            [JsonProperty("probAchieveMinimumIncome")]
            public long[] ProbAchieveMinimumIncome { get; set; }

            [JsonProperty("probAchieveFinalAnnuityIncomePostLegacy")]
            public double[] ProbAchieveFinalAnnuityIncomePostLegacy { get; set; }

            [JsonProperty("longevityWeightedProbSuccess")]
            public double[] LongevityWeightedProbSuccess { get; set; }
        }

        public partial class LifeExpectancyOutput
        {
            [JsonProperty("lifeExpectancyPersonA")]
            public double LifeExpectancyPersonA { get; set; }

            [JsonProperty("lifeExpectancyMedianPersonA")]
            public double LifeExpectancyMedianPersonA { get; set; }

            [JsonProperty("lifeProbabilities")]
            public LifeProbabilities LifeProbabilities { get; set; }
        }

        public partial class LifeProbabilities
        {
            [JsonProperty("timesteps")]
            public long[] Timesteps { get; set; }

            [JsonProperty("ageA")]
            public long[] AgeA { get; set; }

            [JsonProperty("survivalProbA")]
            public double[] SurvivalProbA { get; set; }

            [JsonProperty("deathAtAgeProbA")]
            public double[] DeathAtAgeProbA { get; set; }
        }

        public partial class Links
        {
            [JsonProperty("self")]
            public Self Self { get; set; }
        }

        public partial class Self
        {
            [JsonProperty("href")]
            public string Href { get; set; }
        }

        public partial class RootLifeExpectancy
        {
            [JsonProperty("correlationId")]
            public string CorrelationId { get; set; }

            [JsonProperty("data")]
            public LifeExpectancyResult Data { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("_links")]
            public Links Links { get; set; }
        }

        public partial class LifeExpectancyResult
        {
            [JsonProperty("lifeExpectancyPersonA")]
            public double LifeExpectancyPersonA { get; set; }

            [JsonProperty("lifeExpectancyMedianPersonA")]
            public double LifeExpectancyMedianPersonA { get; set; }

            [JsonProperty("lifeProbabilities")]
            public LifeProbabilities LifeProbabilities { get; set; }
        }
    }
}
