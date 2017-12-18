using System;
using lkong.bookrecommandread;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace lkong.net
{
    class Program
    {
        static void Main(string[] args)
        {
            var credit_cookie = new CreditCookie("username", "password", "answer");
            var three_river_collection = new ThreeRiverColletion(new DateTime(2017, 1, 1), credit_cookie);
            foreach (var three_river_pair in three_river_collection)
            {
                var three_river = three_river_pair.Value;
                //var three_river = new ThreeRiverThread("http://www.lkong.net/thread-1879329-1-1.html", credit_cookie);
                var jobject = new JObject();
                jobject.Add("Time", three_river.CreateTime);
                foreach (var book in three_river.Books)
                {
                    jobject.Add(book, JArray.FromObject(three_river[book]));
                }

                var filename = three_river.Url.Replace("http://www.lkong.net/", "data/").Replace(".html", ".json");
                File.WriteAllText(filename, JsonConvert.SerializeObject(jobject, Formatting.Indented));
            }
        }
    }
}
