using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Walkability
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        string AirKey = " "; //Air Quality Index API key
        string OPWKey = " "; //Open Weather API key
        string ZipKey = " "; //Zip code API key
        string RfKey = " "; //Red Fin API key

        public int WalkZip(string zip)
        {
            int score = 0;
            string url = "http://api.zippopotam.us/us/" + zip;
            Console.WriteLine(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // get City from zip code
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sreader = new StreamReader(dataStream);
            string responsereader = sreader.ReadToEnd();
            response.Close();

            zipObject zipobject = JsonConvert.DeserializeObject<zipObject>(responsereader);

            int AQ = GetAirQuality(zipobject.places[0].placename); //Get a value for air quality //Get a value for air quality
            Console.WriteLine("Air Quality = " + AQ);
            int UV = GetUvIndex(zipobject.places[0].latitude, zipobject.places[0].longitude);//Get a value for UV index
            Console.WriteLine("UV index = " + UV);
            int level = GetBaseValue(zipobject.places[0].latitude, zipobject.places[0].longitude); // Get a base Value score for Walkability
            Console.WriteLine("Base score = " + level);
            AQ = (AQ - 50) / 5; //Normalize air quality value to be used with walk score
            score = level - AQ; //Decrease walkability score in proportion to bad air quality
            score = score - UV;//Decrease walkability score in proportion to excessive UV radiation

            return score;
        }

        private int GetBaseValue(decimal lat, decimal lng)
        {
            int value = 50;
            string url = "http://api.walkscore.com/score?format=json&lat=" + lat + "&lon=" + lng + "&transit=1&bike=1&wsapikey=" + RfKey;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // get City from zip code
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sreader = new StreamReader(dataStream);
            string responsereader = sreader.ReadToEnd();
            response.Close();
            rfObject rfobject = JsonConvert.DeserializeObject<rfObject>(responsereader);
            if (rfobject.status == 1) //If walkability data is available
            {
                value = rfobject.walkscore + rfobject.bike.score; //Average walking and biking score 
                value = value / 2;
            }

            return value;
        }

        private int GetUvIndex(decimal lat, decimal lng)
        {
            int n = 0;
            double value = 0;
            string url = "http://api.openweathermap.org/data/2.5/uvi/history?appid=" + OPWKey + "&lat=" + lat + "&lon=" + lng + "&cnt=10&start=1554120000&end=1554724800";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // get City from zip code
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sreader = new StreamReader(dataStream);
            string responsereader = sreader.ReadToEnd();
            response.Close();

            var result = JsonConvert.DeserializeObject<uvObject[]>(responsereader);
            foreach (var r in result)
            {
                n++;
                value = value + r.value; //Average UV index values for given place
            }
            if (n > 0)
            {
                value = value / n;
            }
            else
            {
                value = 0;
            }
            return Convert.ToInt32(value);
        }

        private int GetAirQuality(string city)
        {
            string url = "https://api.waqi.info/feed/" + city + "/?token=" + AirKey;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // get City from zip code
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sreader = new StreamReader(dataStream);
            string responsereader = sreader.ReadToEnd();
            response.Close();
            TestObject test = JsonConvert.DeserializeObject<TestObject>(responsereader);
            if (test.status == "ok") // if Air quality index is available for given place
            {
                airObject airobject = JsonConvert.DeserializeObject<airObject>(responsereader);
                return airobject.data.aqi;
            }

            else
            {
                return 0;
            }
        }

        public int WalkCity(string city, string state)
        {
            string url = "http://api.zippopotam.us/us/" + state + "/" + city;
            Console.WriteLine(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // get City from zip code
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader sreader = new StreamReader(dataStream);
            string responsereader = sreader.ReadToEnd();
            response.Close();
            cityObject cityobject = JsonConvert.DeserializeObject<cityObject>(responsereader);

            string zip = cityobject.places[0].postcode;
            Console.WriteLine(zip);
            int value = WalkZip(zip);
            return value;
        }
        
    }

    internal class rfObject
    {
        public int status { get; set; }
        public int walkscore { get; set; }
        public Bike bike { get; set; }
    }

    public class Bike
    {
        public int score { get; set; }
        public string description { get; set; }
    }

    internal class uvObject
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public string date_iso { get; set; }
        public int date { get; set; }
        public double value { get; set; }
    }

    internal class airObject
    {
        public string status { get; set; }

        [JsonProperty(PropertyName = "data")]
        public Data data { get; set; }
    }

    public class Data
    {
        public int aqi { get; set; }
        public int idx { get; set; }
    }

    internal class TestObject
    {
        public string status { get; set; }
    }

    public class zipObject
    {

        [JsonProperty(PropertyName = "post code")]
        public string postcode { get; set; }

        public string country { get; set; }

        [JsonProperty(PropertyName = "country abbreviation")]
        public string countryabbreviation { get; set; }
        public List<Places> places { get; set; }
    }

    public class Places
    {
        [JsonProperty(PropertyName = "place name")]
        public string placename { get; set; }

        public decimal longitude { get; set; }
        public string state { get; set; }

        [JsonProperty(PropertyName = "state abbreviation")]
        public string stateabbreviation { get; set; }

        public decimal latitude { get; set; }
    }

    internal class cityObject
    {
        [JsonProperty(PropertyName = "country abbreviation")]
        public string countryabbreviation { get; set; }

        public List<Places2> places { get; set; }
    }

    public class Places2
    {
        [JsonProperty(PropertyName = "place name")]
        public string placename { get; set; }

        [JsonProperty(PropertyName = "longitude")]
        public decimal longitude { get; set; }

        [JsonProperty(PropertyName = "post code")]
        public string postcode { get; set; }

        [JsonProperty(PropertyName = "latitude")]
        public decimal latitude { get; set; }
    }
}
