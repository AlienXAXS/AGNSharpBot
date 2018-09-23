using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AGNSharpBot.DiscordHandler.Services
{
    public class PictureService
    {
        private readonly HttpClient _http;

        public PictureService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetCatPictureAsync()
        {
            var resp = await _http.GetAsync("https://cataas.com/cat/says/AGN%20Terry%20Bot%20Boiiiii");
            return await resp.Content.ReadAsStreamAsync();
        }
    }
}
