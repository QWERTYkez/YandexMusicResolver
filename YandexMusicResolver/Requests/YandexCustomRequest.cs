using System.Net.Http;
using YandexMusicResolver.Config;

namespace YandexMusicResolver.Requests {
    internal class YandexCustomRequest : YandexRequest {
        public YandexCustomRequest(IYandexProxyHolder? proxyHolder, IYandexTokenHolder? tokenHolder) : base(proxyHolder, tokenHolder) { }
        public YandexCustomRequest(IYandexProxyTokenHolder? config = null) : base(config) { }

        public YandexCustomRequest Create(string url) {
            FormRequest(url, HttpMethod.Get);
            return this;
        }
    }
}