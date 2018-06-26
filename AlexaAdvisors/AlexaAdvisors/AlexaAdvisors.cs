
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

namespace AlexaAdvisors
{

    public static class AlexaResponse
    {

        [FunctionName("AlexaResponse")]
        public static async Task<SkillResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)][FromBody]SkillRequest req, TraceWriter log)
        {
            //Find Request Type
            var requestType = req.GetRequestType();

            var response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;


            //Check intent request
            if (requestType == typeof(IntentRequest))
            {
                //Store intent, intent's name and user session
                var intentRequest = req.Request as IntentRequest;
                var intentName = intentRequest.Intent.Name;
                var currentSessionAttributes = req.Session;

                //Check intent name
                switch (intentName)
                {
                    case "AMAZON.CancelIntent":
                        response = CreateResponse("Cancel Intent detected! Goodbye", "");
                        response.Response.ShouldEndSession = true;
                        return response;

                    case "AMAZON.HelpIntent":
                        response = CreateResponse("Let's try how long I can live.", "");
                        response.Response.ShouldEndSession = true;
                        return response;

                    case "AMAZON.StopIntent":
                        response = CreateResponse("Stop Intent detected! Goodbye", "");
                        response.Response.ShouldEndSession = true;
                        return response;

                    case "SessionEndedRequest":
                        response = CreateResponse("Session end request detected! Goodbye", "");
                        response.Response.ShouldEndSession = true;
                        return response;


                    case "LifeExpectancy":
                        var updatedIntent = new Intent();
                        updatedIntent.Name = "LifeExpectancy";
                        updatedIntent.ConfirmationStatus = "NONE";
                        updatedIntent.Slots = intentRequest.Intent.Slots;

                        if (intentRequest.DialogState == "STARTED")
                        {
                            log.Warning("Current Dialogstate: " + intentRequest.DialogState);
                            log.Info("UpdatedIntent value: " + updatedIntent.Slots["gender"].Value + updatedIntent.Slots["health"].Value + updatedIntent.Slots["postcode"].Value);
                            return ResponseBuilder.DialogDelegate(updatedIntent);
                        }
                        else if (intentRequest.DialogState != "COMPLETED")
                        {
                            log.Warning("Current Dialogstate: " + intentRequest.DialogState);
                            log.Info("UpdatedIntent value: " + updatedIntent.Slots["gender"].Value + updatedIntent.Slots["health"].Value + updatedIntent.Slots["postcode"].Value);

                            return ResponseBuilder.DialogDelegate(updatedIntent);

                        }
                        else
                        {
                            //call API here it now complete
                            log.Warning("Current Dialogstate: " + intentRequest.DialogState + " and calling API");
                            MakeRequest();
                            return CreateResponse("called the API", "");
                            //var speech =  ""+ intentRequest.Intent.Slots["postcode"].Value + intentRequest.Intent.Slots["gender"].Value + intentRequest.Intent.Slots["health"].Value;
                            //return CreateResponse(speech,"");
                        }


                }
            }
            else if (requestType == typeof(LaunchRequest))
            {
                // create the welcome message
                var speech = new SsmlOutputSpeech();
                speech.Ssml = "<speak>Welcome to Hymans Robertson! We can find life expectancy for you.</speak>";

                // create the speech reprompt
                var repromptMessage = new PlainTextOutputSpeech();
                repromptMessage.Text = "Let's try. How long I can live?";

                // create the reprompt
                var repromptBody = new Reprompt();
                repromptBody.OutputSpeech = repromptMessage;

                // create the response
                var finalResponse = ResponseBuilder.Ask(speech, repromptBody);
                return finalResponse;
            }
            return response;
        }



        /* HELPER FUNCTIONS */
        //This function will help to create message response with response and repromt message
        public static SkillResponse CreateResponse(string responseMessage, string reprompt)
        {
            var speech = new PlainTextOutputSpeech();
            speech.Text = responseMessage;

            var repromptMessage = new PlainTextOutputSpeech();
            repromptMessage.Text = reprompt;

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
        /*public static void getSlotValues (IntentRequest request)
        {
            var filledSlots = request.Intent.Slots;
            var slotValues = new Slot;
            if (filledSlots.resolutions.resolutionPerAuthority[0].status.code != null)
            {
                switch (filledSlots["health"].resolutions.resolutionPerAuthority[0].status.code)
                {
                    case "ER_SUCCESS_MATH":
                        slotValues[filledSlots.Name] = {
                            synonym: filledSlots.
                        }
                }
            }
            
           

        }*/


        //This function will check all slots in LifeExpectancy all filled
        /*public static bool checkLifeExpectancySlots(IntentRequest req, TraceWriter log)
        {
            var result = false;
            var slots = req.Intent.Slots;

            if (slots["postcode"].Value != null && slots["gender"].Value != null && slots["health"].Value != null)
            {
                req.DialogState = "COMPLETED";
                result = true;
                log.Info("DialogStats = "+ req.DialogState);
                log.Info("Postcode value = "+ slots["postcode"].Value);
                log.Info("Postcode value = " + slots["gender"].Value);
                log.Info("Postcode value = " + slots["health"].Value);

            }

            return result;
        }*/

        public static async void MakeRequest()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            var subsricption_key = "ab05b31f579c4d92aa06bd61d4186b64";


            // Request headers

            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subsricption_key);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/?" + queryString;

            HttpResponseMessage response;

            // Request body
            dynamic body = new JObject();
            body.PersonA.age = "60";
            body.PersonA.gender = "male";
            body.PersonA.healthRelativeToPeers = "same";
            body.PersonA.postcodeProxy = "171001411";

            byte[] byteData = Encoding.UTF8.GetBytes(body);
            var content = new ByteArrayContent(byteData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await client.PostAsync(uri, content);

        }

    }

}

