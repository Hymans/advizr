using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;


namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CallLifeExpectancyAPI();
        }

        public static class Globals
        {
            public static string user_ages = "";
            public static string user_health = "";
            public static string user_gender = "";
            public static string user_postcode = "";
            public static string user_postcode_proxy = "";
            public static HttpClient client;
            public static HttpResponseMessage lifeExpectancyResponse;
            public static string lifeExpectancyResponseString;
            public const string subsricption_key = "ab05b31f579c4d92aa06bd61d4186b64";
        }

        public static async void CallLifeExpectancyAPI()
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            Globals.client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;v=1"));
            Globals.client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Globals.subsricption_key);
            Globals.client.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");

            var uri = "https://hymans-labs.co.uk/lifeexpectancydev/?" + queryString;

            // Request body
            dynamic body = new JObject();
            body.PersonA.age = Globals.user_ages;
            body.PersonA.gender = Globals.user_gender;
            body.PersonA.healthRelativeToPeers = Globals.user_health;
            body.PersonA.postcodeProxy = Globals.user_postcode_proxy;

            log.Info(body);

            byte[] byteData = Encoding.UTF8.GetBytes(body);
            var content = new ByteArrayContent(byteData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.ContentLength = byteData.Length;
            Globals.lifeExpectancyResponse = await Globals.client.PostAsync(uri, content);
        }
    }
}
