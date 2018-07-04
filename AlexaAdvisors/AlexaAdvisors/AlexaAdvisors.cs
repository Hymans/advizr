
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

namespace AlexaAdvisors
{

    public static class AlexaResponse
    {
        //This function creates globals variables
        public static class Globals
        {
            public static int user_ages = 0;
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

            //Check intent request
            if (requestType == typeof(IntentRequest))
            {
                //Store intent request and intent's name
                var intentRequest = req.Request as IntentRequest;
                var intentName = intentRequest.Intent.Name;

                //Check intent's name
                switch (intentName)
                {
                    case "AMAZON.CancelIntent":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "AMAZON.HelpIntent":
                        return ResponseBuilder.Tell("You can try how long I can live ?");

                    case "AMAZON.StopIntent":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "SessionEndedRequest":
                        return ResponseBuilder.Tell("See you next time. Goodbye.");

                    case "LifeExpectancy":
                        //Create updated intent
                        var updatedIntent = new Intent();
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
                            //var slotValues = getSlotValues(intentRequest);

                            //Call Postcode proxy API here


                            //Call Life Expectancy API here
                            log.Info("Calling life expectancy API");

                            var lifeExpectancyValue = await CallLifeExpectancyAPI(log);

                            //Return response
                            return ResponseBuilder.Tell("Your average live around " + lifeExpectancyValue + " years old.");
                        }
                }
            }
            else if (requestType == typeof(LaunchRequest))
            {
                return CreateResponse("Welcome to Hymans Robertson! We can find life expectancy for you. Let's try. How long I can live?", "Try how long I can live?");
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
        public static Slot GetSlotValues (IntentRequest request)
        {
            var filledSlots = request.Intent.Slots;
            var slotValues = new Slot();
            
            foreach (var item in filledSlots.Keys)
            {
                var name = filledSlots[item].Name;
                if (filledSlots[item].Resolution.Authorities[0].Status.Code != null)
                {
                    switch (filledSlots[item].Resolution.Authorities[0].Status.Code)
                    {
                        case "ER_SUCCESS_MATCH":
                            slotValues.Name = name;
                            slotValues.Value = filledSlots[item].Value;
                            slotValues.ConfirmationStatus = "NONE";
                            break;

                        case "ER_SUCCESS_NO_MATCH":
                            slotValues.Name = name;
                            slotValues.Value = null;
                            slotValues.ConfirmationStatus = "NONE";
                            break;

                    }
                }
            }
            return slotValues;
        }

        //This function call LifeExpectancy API
        public static async Task<double> CallLifeExpectancyAPI(TraceWriter log)
        {
            // Request headers
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricption_key);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/";

            // Post request body
            var body = new StringContent("{\"personA\": {\"age\": 60,\"gender\": \"male\",\"healthRelativeToPeers\": \"same\",\"postcodeProxy\": \"171001411\"}}", Encoding.UTF8, "application/json");
            log.Info(body.ToString());

            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = body };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.SendAsync(request);

            Thread.Sleep(500);

            var getAcceptHeader = "application/hal+json;v=1";
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
            var lifeExpectancy = deserializedJson.data.lifeExpectancyPersonA;
            log.Info("longestvity = " + lifeExpectancy);
            //Globals.isCallAPIcomplete = true;
            return lifeExpectancy;
        
        }

        //This function call PostCodeProxy API by using postcode
        /*public static async void CallPostCodeProxyAPI()
        {

        }*/

        
        //Create class for json
        public class PersonA
        {
            public int age { get; set; }
            public string gender { get; set; }
            public string healthRelativeToPeers { get; set; }
            public string postcodeProxy { get; set; }
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

