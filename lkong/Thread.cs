using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;

namespace lkong
{
    public class Thread
    {
        public string Url { get; private set; }

        public static readonly int FloorCountPerPage = 20;

        public class Floor
        {
            internal HtmlNode Value { get; set; }
            public string AuthorName { get; internal set; }
            public string AuthorUID { get; internal set; }

            public DateTime CreateTime { get; internal set; }

            public DateTime LastWriteTime { get; internal set; }
        }

        public DateTime CreateTime => _floors.First().CreateTime;
        public DateTime LastWriteTime => _floors.First().LastWriteTime;

        public string AuthorName => _floors.First().AuthorName;
        public string AuthorUID => _floors.First().AuthorUID;

        public int FloorCount => _floors.Count;

        List<Floor> _floors = new List<Floor>();
        public ICollection<Floor> Floors => _floors;

        public Floor this[int floorIndex] => _floors[floorIndex];

        protected HtmlNode _root;

        public Thread(string url, CreditCookie cookie = null)
        {
            Url = url;
            var webClient = new HtmlWeb()
            {
                PreRequest = (handler, request) =>
                {
                    if(cookie != null)
                    {
                        handler.CookieContainer = cookie.CookieContainer;
                    }
                    return true;
                }
            };
           
            var doc = webClient.Load(url);
            Parse(webClient, doc.DocumentNode);
            foreach (var floor in _floors)
                ParseFloorTime(floor);
        }

        void ParseFloorTime(Floor floor)
        {
            var root = floor.Value.ParentNode.ParentNode;
            var authorposton = root.SelectSingleNode(".//em[starts-with(@id,'authorposton')]");
            var timestr = authorposton.InnerText.Replace("发表于 ", "");
            floor.CreateTime = DateTime.Parse(timestr);
            floor.LastWriteTime = floor.CreateTime;
        }

        void Parse(HtmlWeb webClient, HtmlNode root)
        {
            _root = root;
            //获取当前有多少页
            var pg = root.SelectSingleNode("//div[@class='pgs mtm mbm cl']/div[@class='pg']");
            var hrefs = pg.SelectNodes("a[not(@class)]");
            //先解析当前页
            ParseFloors(root);
            foreach (var href in hrefs)
            {
                var uri = WebUtility.HtmlDecode(href.Attributes["href"].Value);
                ParseFloors(webClient.Load(uri).DocumentNode);
            }
        }

        void ParseFloors(HtmlNode root)
        {
            var postlist = root.SelectSingleNode(".//div[@id='postlist']");
            var floors = postlist.SelectNodes("./div[not(not(@id)) and starts-with(@id,'post_')]");
            foreach (var floor in floors)
            {
                _floors.Add(ParseFloor(floor));
            }
        }

        Floor ParseFloor(HtmlNode root)
        {
            var ret = new Floor();
            //Parse Author Info
            {
                var pls = root.SelectSingleNode("./table/tr/td[@class='pls']");
                var author_a = pls.SelectSingleNode("./div/div/a");
                var uid_url = author_a.Attributes["href"].Value;
                ret.AuthorUID = Regex.Match(uid_url, @"\d+").Value;
                var name_label = author_a.SelectSingleNode("font").InnerText;
                ret.AuthorName = name_label;
            }

            //Get Contetnt HtmlNode
            {
                var plc_pct_pcb = root.SelectSingleNode("./table/tr/td[@class='plc']/div[@class='pct']/div[@class='pcb']");
                ret.Value = plc_pct_pcb;
            }

            return ret;
        }
    }
}
