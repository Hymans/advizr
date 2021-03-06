
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
            public static double userAge = 0;
            public static double userPotSize = 0;
            public static double userPotEquity = 0;
            public static double userPotCash = 0;
            public static double userWithdrawalAmount = 0;
            public static double userPotIncreaseRate = 0;
            public static string userHealth = "";
            public static string userGender = "";
            public static string userPostcode = "";
            public static string userPostcodeProxy = "";
            public const string subsricptionKeyLifeExpectancy = "ab05b31f579c4d92aa06bd61d4186b64";
            public const string subsricptionKeyDrawDown = "9a511111d99a41f5b298ed8f4f0e9ac3";

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
                            
                            //Find User Location
                            Globals.userPostcode = await GetUserLocations(deviceID, accessToken, apiEndPoint, log);

                            if (Globals.userPostcode == "NoPermission")
                            {
                                //Ask for location permission
                                IEnumerable<string> locationPermission = new string[] { "read::alexa:device:all:address:country_and_postal_code" };
                                return ResponseBuilder.TellWithAskForPermissionConsentCard("Please allow me to access the location to find life expectancy. Please check permission request on your mobile.", locationPermission);
                            }
                            log.Info(" postcode = " + Globals.userPostcode);
                                                        
                            //Call Postcode proxy API
                            log.Info("Calling postcodeproxy API");
                            Globals.userPostcodeProxy = await CallPostCodeProxyAPI(log);
                            log.Info("Postcode Proxy is " + Globals.userPostcodeProxy);
                            

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
                            log.Info("Extract slots value");
                            GetSlotValues(intentRequest, log, updatedIntent);

                            //Check valid/invalid value
                            log.Info("Check slots value");
                            var isSlotsValid = CheckSlotsValue();
                            log.Info("Check slots value:" + isSlotsValid);
                            if (!isSlotsValid)
                            {
                                return InvalidSlotsResponse();
                            }

                            //Call Life expectancy API
                            RootLifeExpectancy lifeExpectancyResult = await CallLifeExpectancyAPI(log);
                            var lifeExpectancyValue = lifeExpectancyResult.Data.LifeExpectancyPersonA;

                            //Return response
                            return ResponseBuilder.Tell("Your life expectancy is " + lifeExpectancyValue + " years old. That's great.");
                        }
                    case "DrawDown":

                        //Create updated intent
                        updatedIntent.Name = "DrawDown";
                        updatedIntent.ConfirmationStatus = "NONE";
                        updatedIntent.Slots = intentRequest.Intent.Slots;

                        if (intentRequest.DialogState == "STARTED")
                        {
                            //Find User Location
                            Globals.userPostcode = await GetUserLocations(deviceID, accessToken, apiEndPoint, log);

                           if (Globals.userPostcode == "NoPermission")
                           {
                               //Ask for location permission
                               IEnumerable<string> locationPermission = new string[] { "read::alexa:device:all:address:country_and_postal_code" };
                               return ResponseBuilder.TellWithAskForPermissionConsentCard("Please allow me to access the location to find life expectancy. Please check permission request on your mobile.", locationPermission);
                           }
                           log.Info(" postcode = " + Globals.userPostcode);
                           
                            //Call Postcode proxy API
                            log.Info("Calling postcodeproxy API");
                            Globals.userPostcodeProxy = await CallPostCodeProxyAPI(log);
                            log.Info("Postcode Proxy is " + Globals.userPostcodeProxy);
                            

                            log.Info("Current Dialogstate: " + intentRequest.DialogState);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else if (intentRequest.DialogState != "COMPLETED")
                        {
                            CallDrawDownAPIBackground(log);

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
                            var isSlotsValid = CheckSlotsValue();
                            log.Info("Check slots valid:" + isSlotsValid);
                            if (!isSlotsValid)
                            {
                                return InvalidSlotsResponse();
                            }

                            //Call Drawdown API
                            log.Info("Calling Drawdown API");
                            RootDrawDown drawDownResult = await CallDrawDownAPI(log);
                            var longestvityPercent = drawDownResult.Data.LongevityWeightedProbSuccess*100;
                            var lifeExpectancy = drawDownResult.Data.LifeExpectancyOutput.LifeExpectancyPersonA;

                            //Return response
                            if(longestvityPercent < 50)
                            {
                                return ResponseBuilder.Tell("You life expectancy is "+lifeExpectancy+" years old. You have a chance " + longestvityPercent + " percent to achieve your goal. You might need to adjust your target to increase the chance.");
                            }
                            else
                            {
                                return ResponseBuilder.Tell("Wow! We have a great news. You life expectancy is " + lifeExpectancy + " years old. You have a chance " + longestvityPercent + " percent to achieve your goal.");
                            }
                        }
                    case "WhatHymans":
                        var whatSpeech = "At Hymans Robertson, we provide independent pensions, investments, benefits and risk consulting services, as well as data and technology solutions, to employers, trustees and financial services institutions. For more information please visit www.hymans.co.uk";
                        var whatReprompt = "Let's try how long can I live.";
                        return CreateResponse(whatSpeech, whatReprompt);

                    case "WhyHymans":
                        var whySpeech = "At the forefront of our industry, we�re influencing the way it works. Proud pioneers for the past 95 years, we�re at the vanguard of innovation. Our solutions give companies, trustees and members everything they need for brighter pensions prospects.";
                        var whyReprompt = "Let's try how long can I live.";
                        return CreateResponse(whySpeech, whyReprompt);

                    case "WhereHymans":
                        var whereSpeech = "We have offices located in London, Birmingham, Edinburgh, and Glasglow. If you want to contact us, please visit www.hymans.co.uk";
                        var whereReprompt = "Let's try how long can I live.";
                        return CreateResponse(whereSpeech, whereReprompt);
                }
            }
            else if (requestType == typeof(LaunchRequest))
            {
                return CreateResponse("Welcome to Hymans Robertson. We can find life expectancy and success chance from your investment target. You can use command like How long can I live? or do I have enough money for retirement?", "Try how long can I live?");
            }
            return ResponseBuilder.Tell("Sorry, we don't know your command. Please try again.");
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
            var intentName = request.Intent.Name;

            //Check these values for both life expectancy and drawdown intent
            //Value in the slot is valid
            if (filledSlots["gender"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_MATCH")
            {
                Globals.userGender = filledSlots["gender"].Resolution.Authorities[0].Values[0].Value.Name;
                log.Info("gender = " + Globals.userGender);
            }
            if (filledSlots["health"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_MATCH")
            {
                Globals.userHealth = filledSlots["health"].Resolution.Authorities[0].Values[0].Value.Name;
                log.Info("health = " + Globals.userHealth);
            }
            if (filledSlots["age"].Value != "?")
            {
                Globals.userAge = Double.Parse(filledSlots["age"].Value);
                log.Info("age = " + Globals.userAge);
            }

            //Value in the slot is invalid
            if (filledSlots["gender"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_NO_MATCH")
            {
                Globals.userGender = null;
                log.Info("gender = " + Globals.userGender);
            }

            if (filledSlots["health"].Resolution.Authorities[0].Status.Code == "ER_SUCCESS_NO_MATCH")
            {
                Globals.userHealth = null;
                log.Info("health = " + Globals.userHealth);
            }

            if (filledSlots["age"].Value == "?")
            {
                Globals.userAge = -1;
                log.Info("age = " + Globals.userAge);
            }


            //Check addition values for drawdown api
            if(intentName == "DrawDown")
            {
                //Value in the slot is valid
                if (filledSlots["potSize"].Value != "?")
                {
                    Globals.userPotSize = Double.Parse(filledSlots["potSize"].Value);
                    log.Info("potSize = " + Globals.userPotSize);
                }
                if (filledSlots["potEquity"].Value != "?")
                {
                    Globals.userPotEquity = Double.Parse(filledSlots["potEquity"].Value)/100;
                    log.Info("potEquity = " + Globals.userPotEquity);
                }
                if (filledSlots["withdrawalAmount"].Value != "?")
                {
                    Globals.userWithdrawalAmount = Double.Parse(filledSlots["withdrawalAmount"].Value);
                    log.Info("withdrawalAmount = " + Globals.userWithdrawalAmount);
                }
                if (filledSlots["potIncreaseRate"].Value != "?")
                {
                    Globals.userPotIncreaseRate = Double.Parse(filledSlots["potIncreaseRate"].Value)/100;
                    log.Info("potIncreaseRate = " + Globals.userPotIncreaseRate);
                }

                //Caculate userPotCash by 1 - userPotEquity
                Globals.userPotCash = 1 - Globals.userPotEquity;
                log.Info("potCash = " + Globals.userPotCash);

                //Value in the slot is invalid
                if (filledSlots["potSize"].Value == "?")
                {
                    Globals.userPotSize = -1;
                    log.Info("potSize = " + Globals.userPotSize);
                }
                if (filledSlots["potEquity"].Value == "?")
                {
                    Globals.userPotEquity = -1;
                    log.Info("potEquity = " + Globals.userPotEquity);
                }
                if (filledSlots["withdrawalAmount"].Value == "?")
                {
                    Globals.userWithdrawalAmount = -1;
                    log.Info("withdrawalAmount = " + Globals.userWithdrawalAmount);
                }
                if (filledSlots["potIncreaseRate"].Value == "?")
                {
                    Globals.userPotIncreaseRate = -1;
                    log.Info("potIncreaseRate = " + Globals.userPotIncreaseRate);
                }
            }
        }

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

            try
            {
                var getResponse = await client.GetAsync(getUrl);
                var statusCode = getResponse.StatusCode;
                if (statusCode == HttpStatusCode.Forbidden)
                {
                    log.Info("403 Forbidden: No permission to access the location");
                    return "NoPermission";
                }
                else if (statusCode == HttpStatusCode.OK)
                {
                    log.Info("Got User Location.");
                }
                while (statusCode == HttpStatusCode.SeeOther)
                {
                    Thread.Sleep(100);
                    getResponse = await client.GetAsync(getUrl);
                }

                var getResponseBody = await getResponse.Content.ReadAsStringAsync();
                Postcode deserializedJson = JsonConvert.DeserializeObject<Postcode>(getResponseBody);
                var postcode = deserializedJson.postalCode;
                var trimPostcode = postcode.Replace(" ", "");

                watch.Stop();
                log.Info("Time used for get user location from : " + watch.ElapsedMilliseconds + " ms");

                return trimPostcode;
            }
            catch(NullReferenceException e)
            {
                log.Info("Null postcode detected.");
                return "EH112EE";
            }
        }

        //This function call LifeExpectancy API
        public static async Task<RootLifeExpectancy> CallLifeExpectancyAPI(TraceWriter log)
        {
            var watch = Stopwatch.StartNew();
            // Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricptionKeyLifeExpectancy);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/";

            // Post request body
            var body = new StringContent("{\"personA\": {\"age\": "+Globals.userAge+",\"gender\": \""+Globals.userGender+"\",\"healthRelativeToPeers\": \""+Globals.userHealth+"\",\"postcodeProxy\": \"171001411\"}}", Encoding.UTF8, "application/json");
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
        public static async Task<string> CallPostCodeProxyAPI(TraceWriter log)
        {
            var watch = new Stopwatch();
            watch.Start();
            //Post Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricptionKeyLifeExpectancy);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/Longevity/postcode";

            //Post request body
            var body = new StringContent("{\"vitaCurvesVersion\": \"CV16v1_1214\",\"vitaSegmentsEdition\": \"LTG2014\",\"postcodes\": [\""+Globals.userPostcode+"\"]}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.SendAsync(request);

            //Get request header and URL
            var getAcceptHeader = "application/hal+json;v=1";
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var abUrl = postResponse.Headers.Location.AbsoluteUri;
            var hostUrl = "https://postcodeproxy-dev.azurewebsites.net/longevitypostcodeproxy/postcodeproxy/";

            var id = abUrl.Substring(hostUrl.Length, abUrl.Length - hostUrl.Length);
            var getUrl = "https://hymans-labs.co.uk/Longevity/postcodeproxy/" + id;
            Thread.Sleep(200);
            //Get response
            var getResponse = await client.GetAsync(getUrl);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var replacePostcode = getResponseBody.Replace(Globals.userPostcode, "Postcode");

            try
            {
                RootPostcodeProxy deserializedJson = JsonConvert.DeserializeObject<RootPostcodeProxy>(replacePostcode);

                watch.Stop();
                log.Info("Time used for postcode proxy API: " + watch.ElapsedMilliseconds + " ms");

                return deserializedJson.MappedPostcodes[0].Postcode;
            }
            catch (NullReferenceException e)
            {
                log.Info("Null postcode proxy detected, it will use default value.");
                return "171001411";
            }

        }

        //This function will call drawdown API
        public static async Task<RootDrawDown> CallDrawDownAPI(TraceWriter log)
        {
            var watch = Stopwatch.StartNew();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricptionKeyDrawDown);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/decumulationincomeforlifedev/drawdown/assess/";

            // Post request body
            var body = new StringContent("{\"memberData\": {\"personA\": {\"age\": "+Globals.userAge+",\"gender\": \""+Globals.userGender+"\",\"healthRelativeToPeers\": \""+Globals.userHealth+"\",\"postcodeProxy\": \"171001411\"}},\"potData\": {\"potSizePounds\": "+Globals.userPotSize+",\"potStrategy\": {\"assetClassMapping\": {\"ukEquity\": ["+Globals.userPotEquity+"],\"cash\": ["+Globals.userPotCash+"]}}},\"drawdownIncome\": {\"regularWithdrawal\": {\"amount\": ["+Globals.userWithdrawalAmount+"],\"increaseData\": {\"increaseType\": \"rpi\",\"increaseRate\": "+Globals.userPotIncreaseRate+"}}}}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            watch.Stop();
            log.Info("Time before post response: " + watch.ElapsedMilliseconds);

            var watch2 = Stopwatch.StartNew();
            var postResponse = await client.SendAsync(request);

            watch2.Stop();
            log.Info("Time after post response: " + watch2.ElapsedMilliseconds);

            Thread.Sleep(100);
            var getAcceptHeader = "application/hal+json;v=1";
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(getAcceptHeader));
            var getUrl = postResponse.Headers.Location.AbsoluteUri;
            var getResponse = await client.GetAsync(getUrl);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();

            try
            {
                RootDrawDown deserializedJson = JsonConvert.DeserializeObject<RootDrawDown>(getResponseBody);

                while (deserializedJson.Status == "InProgress")
                {
                    Thread.Sleep(100);
                    getResponse = await client.GetAsync(getUrl);
                    getResponseBody = await getResponse.Content.ReadAsStringAsync();
                    deserializedJson = JsonConvert.DeserializeObject<RootDrawDown>(getResponseBody);
                }

                return deserializedJson;
            }
            catch
            {
                log.Info("Null error found.");
                throw new ArgumentNullException();
            }
        }

        //This function will call drawdown API in background (1st time call the API very slow)
        public static async void CallDrawDownAPIBackground(TraceWriter log)
        {
            var watch = Stopwatch.StartNew();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricptionKeyDrawDown);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/decumulationincomeforlifedev/drawdown/assess/";

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
            log.Info("Time used for Drawdown background API: " + watch2.ElapsedMilliseconds);

        }


        //This function will check valid/invalid slots
        public static bool CheckSlotsValue()
        {
            if (Globals.userGender == null || Globals.userHealth == null || Globals.userAge < 0 || Globals.userPotSize < 0 || Globals.userPotEquity < 0 || Globals.userPotIncreaseRate < 0 || Globals.userWithdrawalAmount < 0)
            {
                return false;
            }
            return true;
        }
        
        //This function will return message for invalid slots
        public static SkillResponse InvalidSlotsResponse()
        {
            var speech = "Sorry, your information: ";
            if(Globals.userGender == null)
            {
                speech = speech + "gender, ";
            }
            if(Globals.userHealth == null)
            {
                speech = speech + "health, ";
            }
            if (Globals.userAge < 0)
            {
                speech = speech + "age, ";
            }
            if (Globals.userPotSize < 0)
            {
                speech = speech + "investment pot size, ";
            }
            if (Globals.userPotEquity < 0)
            {
                speech = speech + "investment equity size, ";
            }
            if (Globals.userPotIncreaseRate < 0)
            {
                speech = speech + "portfolio increasing rate, ";
            }
            if (Globals.userWithdrawalAmount < 0)
            {
                speech = speech + "withdrawal amount, ";
            }

            speech = speech + "is not valid. Please try again.";
            var response = new SsmlOutputSpeech();
            response.Ssml = "<speak>" + speech + "</speak>";

            return ResponseBuilder.Tell(response);
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
            public DrawDownResult Data { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("_links")]
            public Links Links { get; set; }
        }

        public partial class DrawDownResult
        {
            [JsonProperty("lifeExpectancyOutput")]
            public LifeExpectancyOutput LifeExpectancyOutput { get; set; }

            [JsonProperty("timesteps")]
            public long[] Timesteps { get; set; }

            [JsonProperty("contributionsPaidReal")]
            public Dictionary<string, double[]> ContributionsPaidReal { get; set; }

            [JsonProperty("fundAmountReal")]
            public Dictionary<string, double[]> FundAmountReal { get; set; }

            [JsonProperty("probFundGreaterThanZero")]
            public double[] ProbFundGreaterThanZero { get; set; }

            [JsonProperty("potentialAnnuityIncomeFromFundReal")]
            public Dictionary<string, double[]> PotentialAnnuityIncomeFromFundReal { get; set; }

            [JsonProperty("fundAmountPostLegacyReal")]
            public Dictionary<string, double[]> FundAmountPostLegacyReal { get; set; }

            [JsonProperty("probFundGreaterThanLegacy")]
            public double[] ProbFundGreaterThanLegacy { get; set; }

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

            [JsonProperty("probAchieveMinimumIncome")]
            public long[] ProbAchieveMinimumIncome { get; set; }

            [JsonProperty("longevityWeightedProbSuccess")]
            public double LongevityWeightedProbSuccess { get; set; }
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

        public partial class RootPostcodeProxy
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }

            [JsonProperty("vitaCurvesVersion")]
            public string VitaCurvesVersion { get; set; }

            [JsonProperty("vitaSegmentsEdition")]
            public string VitaSegmentsEdition { get; set; }

            [JsonProperty("mappedPostcodes")]
            public MappedPostcode[] MappedPostcodes { get; set; }

            [JsonProperty("_links")]
            public Links Links { get; set; }
        }

        public partial class MappedPostcode
        {
            [JsonProperty("Postcode")]
            public string Postcode { get; set; }
        }
    }

}

