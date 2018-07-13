
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Alexa.NET.Request.Type;
using Alexa.NET;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using System.Net;
using System.Threading;
using System;
using System.Collections.Generic;

namespace AlexaAdvisors
{

    public static class AlexaResponse
    {
        //This function creates globals variables
        public static class Globals
        {
            public static double user_age = 0;
            public static string user_health = "";
            public static string user_gender = "";
            public static string user_postcode = "";
            public static string user_postcode_proxy = "";
            public const string subsricption_key = "ab05b31f579c4d92aa06bd61d4186b64";
        }


        //This is main function for Alexa response
        [FunctionName("AlexaResponse")]
        public static async Task<SkillResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)][FromBody]SkillRequest req, TraceWriter log)
        {
            //Find Request Type
            var requestType = req.GetRequestType();

            //Keep deviceId and AccessToken for access user's location
            var accessToken = req.Context.System.ApiAccessToken;
            var deviceID = req.Context.System.Device.DeviceID;
            var apiEndPoint = req.Context.System.ApiEndpoint;
            var requestID = req.Request.RequestId;


            //Check intent request
            if (requestType == typeof(IntentRequest))
            {
                //Store intent request and intent's name
                var intentRequest = req.Request as IntentRequest;
                var intentName = intentRequest.Intent.Name;
                var updatedIntent = new Intent();

                //Check intent's name
                switch (intentName)
                {
                    case "AMAZON.CancelIntent":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "AMAZON.HelpIntent":
                        return ResponseBuilder.Tell("You can try how long can I live ?");

                    case "AMAZON.StopIntent":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "SessionEndedRequest":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "LifeExpectancy":
                        //Create updated intent
                        updatedIntent.Name = "LifeExpectancy";
                        updatedIntent.ConfirmationStatus = "NONE";
                        updatedIntent.Slots = intentRequest.Intent.Slots;

                        if (intentRequest.DialogState == "STARTED")
                        {
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else if (intentRequest.DialogState != "COMPLETED")
                        {
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else
                        {
                            //Dialog already completed
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);

                            //Store slot values into globals variables
                            log.Info("Extract slot values");
                            GetSlotValues(intentRequest, log, updatedIntent);

                            //Check valid/invalid value
                            if (Globals.user_gender == null || Globals.user_health == null)
                            {
                                var speech = new SsmlOutputSpeech();
                                speech.Ssml = "<speak>Sorry, your gender or health value is not valid. Please try again.</speak>";

                                return ResponseBuilder.Tell(speech);
                            }

                            //Find User Location
                            /*
                            string userPostcode = await GetUserLocations(deviceID, accessToken, apiEndPoint, log);
                            
                            if(userPostcode == "")
                            {
                                //Ask for location permission here
                                IEnumerable<string> locationPermission = new string[] { "read::alexa:device:all:address:country_and_postal_code" };
                                return ResponseBuilder.TellWithAskForPermissionConsentCard("Please allow me to access the location to find life expectancy. Please check permission request on your mobile.", locationPermission);

                            }
                            else
                            {
                                log.Info(" postcode = " + userPostcode);
                            } 
                            */
                            
                            
                            //Call Postcode proxy API



                            //Call Life Expectancy API
                            /*
                            log.Info("Calling life expectancy API");
                            bool isSendProgress = await SendProgress("OK, I am finding your life expectancy. Please wait.", requestID, apiEndPoint, accessToken, log);
                            if (isSendProgress)
                            {
                                log.Info("Send progress succeed");
                            }
                            else
                            {
                                log.Info("Send progress failed");
                            }
                            */

                            RootLifeExpectancy lifeExpectancyResult = await CallLifeExpectancyAPI(log);
                            var lifeExpectancyValue = lifeExpectancyResult.Data.LifeExpectancyPersonA;

                            //Return response
                            return ResponseBuilder.Tell("Your life expectancy is " + lifeExpectancyValue + " years old.");
                        }
                    case "DrawDown":
                        
                        //Create updated intent
                        updatedIntent.Name = "DrawDown";
                        updatedIntent.ConfirmationStatus = "NONE";
                        updatedIntent.Slots = intentRequest.Intent.Slots;

                        if (intentRequest.DialogState == "STARTED")
                        {
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else if (intentRequest.DialogState != "COMPLETED")
                        {
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else
                        {
                            //Dialog already completed
                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            /*
                            bool isSendProgress = await SendProgress("OK, I am calculating the result. Please wait.", requestID, apiEndPoint, accessToken, log);
                            
                            if (isSendProgress)
                            {
                                log.Info("Send progress succeed");
                            }
                            else
                            {
                                log.Info("Send progress failed");
                            }
                            */

                            //Call Life Expectancy API
                            log.Info("Calling Drawdown API");
                            RootDrawDown drawDownResult = await CallDrawDownAPI(log);
                            var lifeExpectancyValue = drawDownResult.Data.LifeExpectancyOutput.LifeExpectancyPersonA;

                            //Return response
                            return ResponseBuilder.Tell("Your life expectancy is"+ lifeExpectancyValue + " years old.");
                        }
                }
            }
            else if (requestType == typeof(LaunchRequest))
            {
                return CreateResponse("Welcome to Hymans Robertson! We can find life expectancy for you. Let's try. How long can I live?", "Try how long can I live?");
            }
            return ResponseBuilder.Tell("Goodbye");
        }


        /* HELPER FUNCTIONS */
        //This function will help to create message response with response and repromt message
        public static SkillResponse CreateResponse(string responseMessage, string reprompt)
        {
            var speech = new SsmlOutputSpeech();
            speech.Ssml = "<speak>" + responseMessage + "</speak>";

            var repromptMessage = new SsmlOutputSpeech();
            repromptMessage.Ssml = "<speak>" + reprompt + "</speak>";

            var repromptBody = new Reprompt();
            repromptBody.OutputSpeech = repromptMessage;

            var responseBody = new ResponseBody();
            responseBody.OutputSpeech = speech;
            responseBody.ShouldEndSession = false;
            responseBody.Reprompt = repromptBody;

            var skillResponse = new SkillResponse();
            skillResponse.Response = responseBody;
            skillResponse.Version = "1.0";

            return skillResponse;
        }
        
        //This function will extract slot value when all slots were filled
        public static void GetSlotValues (IntentRequest request, TraceWriter log, Intent updatedIntent)
        {
            var filledSlots = request.Intent.Slots;
            
            //Value in the slot is valid
            if (filledSlots["gender"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_MATCH")
            {
                Globals.user_gender = filledSlots["gender"].Resolution.Authorities[0].Values[0].Value.Name;
                log.Info("gender = " + Globals.user_gender);
            }
            if (filledSlots["health"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_MATCH")
            {
                Globals.user_health = filledSlots["health"].Resolution.Authorities[0].Values[0].Value.Name;
                log.Info("health = " + Globals.user_health);
            } 
            if (filledSlots["age"].Value != "?")
            {
                Globals.user_age = Double.Parse(filledSlots["age"].Value);
                log.Info("age = " + Globals.user_age);
            }
            
            //Value in the slot is invalid, have to ask user again
            if (filledSlots["gender"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_NO_MATCH")
            {
                Globals.user_gender = null;
                log.Info("gender = " + Globals.user_gender);
            }

            if (filledSlots["health"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_NO_MATCH")
            {
                Globals.user_health = null;
                log.Info("gender = " + Globals.user_health);
            }

            if (filledSlots["age"].Value == "?")
            {
                Globals.user_age = 0;
                log.Info("age = " + Globals.user_age);
            }

        }

        //This one more flexible but didn't work
        /*public static void GetSlotValues (IntentRequest request, TraceWriter log)
        {
            var filledSlots = request.Intent.Slots;
            Globals.user_ages = double.Parse(filledSlots["age"].Value);
            log.Info("user age = " + Globals.user_ages);

            foreach (var item in filledSlots.Keys)
            {
                log.Info(item);

                var name = filledSlots[item].Name;
                if (filledSlots[item].Resolution.Authorities[0].Status.Code != null)
                {
                    log.Info(name);
                    switch (filledSlots[item].Resolution.Authorities[0].Status.Code)
                    {
                        case "ER_SUCCESS_MATCH":

                           if (name == "health")
                            {
                                Globals.user_health = filledSlots[item].Resolution.Authorities[0].Values[0].Value.Name;
                                log.Info("user health = " + Globals.user_health);
                            }
                            else if (name =="gender")
                            {
                                Globals.user_gender = filledSlots[item].Resolution.Authorities[0].Values[0].Value.Name;
                                log.Info("user gender = " + Globals.user_gender);
                            }
                            break;

                        case "ER_SUCCESS_NO_MATCH":

                           if (name.Equals("health"))
                            {
                                Globals.user_health = "";
                                log.Info("user health is not okay.");
                            }
                            else if (name.Equals("gender"))
                            {
                                Globals.user_gender = "";
                                log.Info("user gender is not okay.");
                            }
                            break;

                    }
                }
            }
        }*/

        //Get User Postcode
        private static async Task<string> GetUserLocations(String deviceID, String accessToken, String apiEndPoint, TraceWriter log)
        {
            var watch = Stopwatch.StartNew();
            // Set value for header and URL
            var getUrl = apiEndPoint+"/v1/devices/" + deviceID + "/settings/address/countryAndPostalCode";
            var authorization = "Bearer " + accessToken;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            client.DefaultRequestHeaders.Add("Authorization", authorization);

            var getResponse = await client.GetAsync(getUrl);
            var statusCode = getResponse.StatusCode;
            if(statusCode == HttpStatusCode.Forbidden){
                log.Info("403 Forbidden: No permission to access the location");
                return "";
             
            }
            else if(statusCode == HttpStatusCode.OK)
            {
                log.Info("Get User Location is OK.");
            }
            while (statusCode == HttpStatusCode.SeeOther)
            {
                Thread.Sleep(100);
                getResponse = await client.GetAsync(getUrl);
            }

            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            Postcode deserializedJson = JsonConvert.DeserializeObject<Postcode>(getResponseBody);
            var postcode = deserializedJson.postalCode;

            watch.Stop();
            log.Info("Time used for get user location from : " + watch.ElapsedMilliseconds + " ms");

            return postcode;

        }

        //This function call LifeExpectancy API
        public static async Task<RootLifeExpectancy> CallLifeExpectancyAPI(TraceWriter log)
        {
            var watch = Stopwatch.StartNew();
            // Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricption_key);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/";

            // Post request body
            var body = new StringContent("{\"personA\": {\"age\": "+Globals.user_age+",\"gender\": \""+Globals.user_gender+"\",\"healthRelativeToPeers\": \""+Globals.user_health+"\",\"postcodeProxy\": \"171001411\"}}", Encoding.UTF8, "application/json");
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
            return deserializedJson;
        
        }

        //This function call PostCodeProxy API by using postcode
        /*public static async void CallPostCodeProxyAPI()
        {

        }*/

        //This function will call drawdown API
        public static async Task<RootDrawDown> CallDrawDownAPI(TraceWriter log)
        {
            var watch = Stopwatch.StartNew();
            //Post Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "9a511111d99a41f5b298ed8f4f0e9ac3");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/decumulationrundowndev/assess";

            //Post request body
            var body = new StringContent("{\"memberData\": {\"personA\": {\"age\": 60,\"gender\": \"male\",\"healthRelativeToPeers\": \"same\",\"postcodeProxy\": \"171001411\"}},\"potData\": {\"potSizePounds\": 100000,\"potStrategy\": {\"assetClassMapping\": {\"ukEquity\": [0.5],\"cash\": [0.5]}}},\"drawdownIncome\": {\"regularWithdrawal\": {\"amount\": [5000],\"increaseData\": {\"increaseType\": \"rpi\",\"increaseRate\": 0.01}}}}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.SendAsync(request);
       
            //Get request header and URL
            var getAcceptHeader = "application/hal+json;v=1";
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var getUrl = postResponse.Headers.Location.AbsoluteUri;

            //Get response
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

            watch.Stop();
            log.Info("Time used for life drawdown API: " + watch.ElapsedMilliseconds + " ms");

            return deserializedJson;
        }


        //This function will send progress response to user
        public static async Task<bool> SendProgress(string progressMessage, string requestID, string apiEndPoint, string accessToken, TraceWriter log)
        {
            // Set value for header and URL
            var postURL = apiEndPoint + "/v1/directives/";
            var authorization = "Bearer " + accessToken;

            log.Info("post URL: " + postURL);
            log.Info("authorization: " + authorization);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            client.DefaultRequestHeaders.Add("Authorization", authorization);

            var body = new StringContent("{\"header\":{\"requestId\":\""+requestID+"\"},\"directive\":{\"type\":\"VoicePlayer.Speak\",\"speech\":\""+progressMessage+"\"}}", Encoding.UTF8, "application/json");
            log.Info(body.ToString());

            var request = new HttpRequestMessage(HttpMethod.Post, postURL) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.SendAsync(request);

            log.Info("Send Progress response code: "+postResponse.StatusCode.ToString());

            if (postResponse.StatusCode == HttpStatusCode.NoContent)
            {
                return true;

            } else
            {
                return false;
            }

        }


        /*CONSTRUCTOR FOR JSON*/
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

