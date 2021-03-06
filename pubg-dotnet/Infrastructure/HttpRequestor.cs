﻿using Pubg.Net.Exceptions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Pubg.Net.Infrastructure
{
    internal static class HttpRequestor
    {
        internal static HttpClient HttpClient { get; private set; }

        static HttpRequestor()
        {
            var clientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            HttpClient = new HttpClient(clientHandler);

            var timeout = PubgApiConfiguration.GetHttpTimeout();

            if (timeout.HasValue)
                HttpClient.Timeout = timeout.Value;
        }

        public static string GetString(string url, string apiToken = null)
        {
            var request = BuildRequest(url, apiToken);

            var response = HttpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            return HandleResponse(response, responseContent);
        }

        public async static Task<string> GetStringAsync(string url, CancellationToken cancellationToken, string apiToken = null)
        {
            var request = BuildRequest(url, apiToken);

            var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return HandleResponse(response, responseContent);
        }

        private static HttpRequestMessage BuildRequest(string url, string apiToken)
        {          
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if(!string.IsNullOrEmpty(apiToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            request.Headers.Add("Accept-Encoding", "gzip");
            request.Headers.Add("Accept", "application/vnd.api+json");

            return request;
        }

        private static string HandleResponse(HttpResponseMessage response, string responseContent)
        {
            if (response.IsSuccessStatusCode)
                return responseContent;

            throw BuildException(response, responseContent);
        }

        private static PubgException BuildException(HttpResponseMessage response, string responseContent)
        {
            switch(response.StatusCode)
            {
                case HttpStatusCode.Unauthorized: return new PubgUnauthorizedException();
                case HttpStatusCode.UnsupportedMediaType: return new PubgContentTypeException();
                case HttpStatusCode.NotFound: return new PubgNotFoundException();
                case (HttpStatusCode) 429: return new PubgTooManyRequestsException(response.Headers.GetValues("X-RateLimit-Reset").FirstOrDefault()); 
                default:
                    var errors = ErrorMapper.MapErrors(responseContent);
                    return new PubgException("Errors have occured with your request", response.StatusCode, errors);
            }
        }
    }
}
