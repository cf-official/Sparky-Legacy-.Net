using HtmlAgilityPack;
using Sparky.Database;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public sealed class ForumScraper
    {
        private const string url = "https://forum.codeforge.cc";

        private const string profilePage = "/member.php?action=profile&uid={0}";

        private readonly HttpClient _http = new HttpClient();

        public ForumScraper()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "Sparky");
        }

        public async Task<int?> GetRepAsync(int forumUid)
        {
            var doc = new HtmlDocument();
            doc.Load(await GetProfileAsync(forumUid));

            var repNode = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[3]/div[1]/table[1]/tr[1]/td[1]/table[1]/tr[8]/td[2]/strong[1]");
            return repNode?.InnerText == null ? default(int?) : int.Parse(repNode.InnerText);
        }

        public async Task<string> GetVerificationCodeAsync(int forumUid)
        {
            var doc = new HtmlDocument();
            doc.Load(await GetProfileAsync(forumUid));

            var verifNode = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[3]/div[1]/table[1]/tr[1]/td[3]/table[1]/tr[4]/td[1]/strong[1]");
            return verifNode?.InnerText;
        }

        private Task<Stream> GetProfileAsync(int uid)
            => _http.GetStreamAsync(url + string.Format(profilePage, uid));
    }
}
