using System;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace lkong.bookrecommandread
{
    public class ThreeRiverThread : Thread
    {
        public class ThreeRiverFloor : Floor
        {
            public ThreeRiverFloor(Floor floor)
            {
                AuthorName = floor.AuthorName;
                AuthorUID = floor.AuthorUID;
                Value = floor.Value;
                CreateTime = floor.CreateTime;
                LastWriteTime = floor.LastWriteTime;
            }
            public string Content { get; internal set; }
        }

        //key 是书名,value是评价
        Dictionary<string, List<ThreeRiverFloor>> _three_floors = new Dictionary<string, List<ThreeRiverFloor>>();

        public ICollection<string> Books => _three_floors.Keys;
        public ICollection<ThreeRiverFloor> this[string bookname] => _three_floors[bookname];

        public string RankOwnerName => AuthorName;

        public ThreeRiverThread(string url, CreditCookie cookie = null)
            : base(url, cookie)
        {
            ParseFirstFloor();
        }


        public static DateTime ExtractTitleThreadRiverTime(string title)
        {
            //quick patch
            var match = Regex.Match(title, @"\d{1,2}\.\d{1,2}");
            var splists = match.Value.Split('.');
            return new DateTime(DateTime.Now.Year, int.Parse(splists[0]), int.Parse(splists[1]));
        }

        void ParseFirstFloor()
        {
            //find table
            var floor = this[0];
            var t_f = floor.Value.SelectSingleNode("div/table/tr/td");
            var three_river_thread = t_f.SelectSingleNode("table");
            ParseTable(three_river_thread);
        }

        void ParseTable(HtmlNode table)
        {
            var trs = table.SelectNodes("tr") ?? table.SelectNodes("tbody/tr");
            foreach (var tr in trs.Skip(1))
            {
                var cols = tr.SelectNodes("td");
                if (cols.Count != 4)
                    throw new ArgumentException(nameof(table));
                //parse 书目
                var a = cols[0].SelectSingleNode(".//a[starts-with(@href,'http')]");
                var bookname = a.InnerText;
                bookname = bookname.Replace("《", "").Replace("》", "");
                _three_floors[bookname] = new List<ThreeRiverFloor>();

                //parse楼层
                var floors = cols[2].SelectNodes(".//a");
                IEnumerable<string> floor_indices = floors?.Select(floor => floor.InnerText) ??
                    Regex.Matches(cols[2].InnerText, @"\d*#").Select(match => match.Value);//没有编辑跳转链接

                foreach (var index_str in floor_indices)
                {
                    var floor_index = int.Parse(index_str.Replace("#", "")) - 1;
                    var three_floor = new ThreeRiverFloor(this[floor_index]);
                    var content = three_floor.Value.InnerText;
                    content = FilterContent(content, bookname, three_floor);
                    three_floor.Content = content;
                    _three_floors[bookname].Add(three_floor);
                }
            }
        }

        string FilterContent(string content, string bookname, Floor floor)
        {
            content = content.Replace("《", "").Replace("》", "").Replace(bookname, "");
            string modify_tip = "\r\n\r\n 本帖最后由 ";
            if (content.Contains(modify_tip))
            {
                content = content.Substring(content.IndexOf("辑") + 1);
            }

            return content;
        }
    }
}