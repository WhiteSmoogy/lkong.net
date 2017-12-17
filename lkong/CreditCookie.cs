using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace lkong
{
    public class CreditCookie
    {
        public CookieContainer CookieContainer { get; private set; } = new CookieContainer();

        public CreditCookie(string username, string paswword, string answer)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = CookieContainer })
            using (var client = new HttpClient(handler))
            {
                Task.Run(async ()  => {
                    //先请求获取hash
                    var login = await client.GetAsync("http://www.lkong.net/member.php?mod=logging&action=login");
                    var loginhtml = await login.Content.ReadAsStringAsync();
                    var document = new HtmlDocument();
                    document.LoadHtml(loginhtml);
                    var hash = document.DocumentNode.SelectSingleNode("//input[@name='formhash']").Attributes["value"].Value;

                   await client.PostAsync(" http://www.lkong.net/member.php?mod=logging&action=login&loginsubmit=yes&loginhash=Lo6FM&inajax=1", new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        ["answer"] = answer,
                        ["cookietime"] = "2592000",
                        ["formhash"] = hash,
                        ["password"] = paswword,
                        ["questionid"] = "0",
                        ["referer"] = "%2forum.php",
                        ["username"] = username,
                    }));
                }).Wait();
            }
        }
    }
}
