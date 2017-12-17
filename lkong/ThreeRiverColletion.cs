using System.Collections.Generic;
using HtmlAgilityPack;
using System;
using System.Linq;
using System.Collections.Concurrent;

namespace lkong.bookrecommandread
{
    public class ThreeRiverColletion
    {
        string ranklisturl = "http://www.lkong.net/forum.php?mod=forumdisplay&fid=60&filter=typeid&typeid=620";

        public string[] title_filter_keys = {
            "【全火力吐槽】",//懒人周佳
            "单刷",//大哥别闹
            "强推",//
            "刷榜",
            "试毒"
        };

        private Dictionary<DateTime, ThreeRiverThread> _threeRiverThreads = new Dictionary<DateTime, ThreeRiverThread>();

        public Dictionary<DateTime, ThreeRiverThread> ThreeRiverThreads => _threeRiverThreads;

        public ThreeRiverColletion(CreditCookie cookie = null)
            : this(DateTime.MinValue, cookie)
        {
        }

        public ThreeRiverColletion(DateTime earliest, CreditCookie cookie = null)
        {
            //获取网页内容
            var webClient = new HtmlWeb()
            {
                PreRequest = (handler, request) =>
                {
                    if (cookie != null)
                    {
                        handler.CookieContainer = cookie.CookieContainer;
                    }
                    return true;
                }
            };
            var doc = webClient.Load(ranklisturl);

            var root = doc.DocumentNode;
            //获取当前页所有帖子
            if (ParseThreadList(root, earliest))
            {
                //获取分页个数
                var last = root.SelectSingleNode("//div[@class='pg']/a[@class='last']");
                var pagecount = int.Parse(last.InnerText.Trim().Replace("...", ""));
                for (var i = 2; i <= pagecount; ++i)
                {
                    root = webClient.Load(ranklisturl + $"&page={i}").DocumentNode;
                    //获取该页的所有帖子
                    if (!ParseThreadList(root, earliest))
                        break;
                }
            }
            //过滤所有帖子
            FilterThreeRiverThreadList();

            //分析所有帖子
            GroupThreadList(cookie);
        }
        //key是URL,value是标题
        Dictionary<string, string> thread_lists = new Dictionary<string, string>();
        bool ParseThreadList(HtmlNode root, DateTime earliest)
        {
            var threadlist = root.SelectSingleNode("//div[@id='threadlist']");

            var last_threads_count = thread_lists.Count;
            //标题与链接
            var threads = threadlist.SelectNodes(".//tbody[starts-with(@id,'normalthread')]");
            foreach (var tbody_root in threads)
            {
                if (ExtractCreateTime(tbody_root) < earliest)
                    continue;
                var url_title = ExtratUrlAndTitle(tbody_root);
                thread_lists.Add(url_title.Item1, url_title.Item2);
            }

            return thread_lists.Count > last_threads_count + threads.Count - 5;
        }

        DateTime ExtractCreateTime(HtmlNode tbody_root)
        {
            var em = tbody_root.SelectSingleNode("tr/td[@class='by']/em");
            return DateTime.Parse(em.InnerText);
        }

        Tuple<string, string> ExtratUrlAndTitle(HtmlNode tbody_root)
        {
            var a = tbody_root.SelectSingleNode("tr/th[@class='new']/a");
            var url = a.Attributes["href"].Value;
            var title = a.InnerText;
            return Tuple.Create(url, title);
        }

        void FilterThreeRiverThreadList()
        {
            var keysToRemove = new List<string>();
            foreach (var pair in thread_lists)
            {
                try
                {
                    ThreeRiverThread.ExtractTitleThreadRiverTime(pair.Value);
                    if (title_filter_keys.Any(filter_key => pair.Value.Contains(filter_key)))
                        keysToRemove.Add(pair.Key);
                }
                catch (Exception)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (var key in keysToRemove)
                thread_lists.Remove(key);
        }

        void GroupThreadList(CreditCookie cookie)
        {
            var bag = new ConcurrentBag<ThreeRiverThread>();
            Array.ForEach(thread_lists.Keys.ToArray(), key =>
            {
                var threeriver_thread = new ThreeRiverThread(key, cookie);
                bag.Add(threeriver_thread);
            }
            );
            foreach (var thread in bag)
            {
                _threeRiverThreads.Add(thread.CreateTime, thread);
            }
        }
    }
}
