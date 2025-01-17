﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using YandexMusicResolver.Config;
using YandexMusicResolver.Responses;

namespace YandexMusicResolver.Requests
{
    internal class YandexRequest
    {
        private HttpRequestMessage? _fullRequest;
        private IYandexProxyHolder? _proxyHolder;
        private IYandexTokenHolder? _tokenHolder;

        public YandexRequest(IYandexProxyHolder? proxyHolder, IYandexTokenHolder? tokenHolder)
        {
            _tokenHolder = tokenHolder;
            _proxyHolder = proxyHolder;
        }

        public YandexRequest(IYandexProxyTokenHolder? config = null) : this(config, config) { }

        protected string GetQueryString(Dictionary<string, string> query)
        {
            return string.Join("&", query.Select(p => $"{p.Key}={HttpUtility.UrlEncode(p.Value)}"));
        }

        protected virtual void FormRequest(string url, HttpMethod method,
                                           Dictionary<string, string>? query = null, 
                                           List<KeyValuePair<string, string>>? headers = null,
                                           string? body = null)
        {
            var queryStr = string.Empty;
            if (query != null && query.Count > 0)
                queryStr = "?" + GetQueryString(query);

            var uri = new Uri($"{url}{queryStr}");
            HttpRequestMessage request = new HttpRequestMessage(method, uri);

            if (headers != null && headers.Count > 0)
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

            TryAddHeader("User-Agent", "Yandex-Music-API");
            TryAddHeader("X-Yandex-Music-Client", "WindowsPhone/3.20");
            if (_tokenHolder?.YandexToken != null) TryAddHeader("Authorization", "OAuth " + _tokenHolder.YandexToken);

            if (!string.IsNullOrEmpty(body))
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            request.Headers.Add("AcceptCharset", Encoding.UTF8.WebName);
            request.Headers.Add("AcceptEncoding", "gzip");

            _fullRequest = request;

            void TryAddHeader(string name, string value)
            {
                if (!request.Headers.Contains(name))
                {
                    request.Headers.Add(name, value);
                }
            }
        }

        public async Task EnsureOk()
        {
            var httpWebResponse = await GetResponseAsync();
            if (httpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException("Invalid status code: " + httpWebResponse.StatusCode);
            }
        }

        public async Task<HttpResponseMessage> GetResponseAsync()
        {
            if (_fullRequest == null)
                throw new NullReferenceException("Create request before getting response");
            try
            {
                using var client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip,
                    Proxy = _proxyHolder?.YandexProxy
                });
                return await client.SendAsync(_fullRequest);
            }
            catch (WebException e)
            {
                if (e.Response is HttpWebResponse { StatusCode: HttpStatusCode.Unauthorized })
                {
                    throw new AuthenticationException(e.Message, e);
                }

                throw;
            }
        }

        public async Task<T> GetResponseAsync<T>()
        {
            var content = await GetResponseBodyAsync();
            YandexApiResponse<T> yandexApiResponse;
            try
            {
                yandexApiResponse = JsonConvert.DeserializeObject<YandexApiResponse<T>>(content);
            }
            catch (Exception e)
            {
                throw new Exception("Couldn't get valid API response.", e);
            }

            if (yandexApiResponse.Result != null) return yandexApiResponse.Result;
            if (yandexApiResponse.Error != null) throw new YandexApiResponseException("Couldn't get API response result.", yandexApiResponse.Error);
            throw new Exception("Couldn't get API response result.");
        }

        public async Task<string> GetResponseBodyAsync(HttpResponseMessage? response = null)
        {
            response ??= await GetResponseAsync();
            return await response.Content.ReadAsStringAsync();
        }
    }
}