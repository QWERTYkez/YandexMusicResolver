using System.Security.Authentication;
using System.Threading.Tasks;
using YandexMusicResolver.Config;
using YandexMusicResolver.Requests;
using YandexMusicResolver.Responses;

namespace YandexMusicResolver {
    /// <summary>
    /// Represents a set of methods that serve for authorization in Yandex Music
    /// </summary>
    public static class YandexMusicAuth {
        /// <summary>
        /// Validates token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <param name="proxyHolder">Container for proxy, which should be used for request</param>
        /// <returns>True if token valid</returns>
        public static async Task<bool> ValidateTokenAsync(string token, IYandexProxyHolder? proxyHolder = null) {
            var metaAccountResponse = await new YandexCustomRequest(proxyHolder, new TokenHolder(token))
                                           .Create("https://api.music.yandex.net/account/status")
                                           .GetResponseAsync<MetaAccountResponse>();
            return !string.IsNullOrEmpty(metaAccountResponse.Account?.Uid);
        }

        /// <summary>
        /// Attempt to authorise
        /// </summary>
        /// <param name="login">Login from Yandex account</param>
        /// <param name="password">Password from Yandex account</param>
        /// <param name="proxyHolder">Container for proxy, which should be used for request</param>
        /// <param name="Token">AccessToken</param>
        /// <exception cref="InvalidCredentialException">Throws when failed to authorize with provided login and password</exception>
        /// <returns>True if succesful</returns>
        public static bool Login(string login, string password, out string? Token, IYandexProxyHolder? proxyHolder = null) {
            var res = new YandexAuthRequest(proxyHolder).Create(login, password).ParseResponseAsync().GetAwaiter().GetResult();
            if (res != null)
            {
                Token = res.AccessToken;
                return true;
            }
            else
            {
                Token = null;
                return false;
            }
        }

        /// <summary>
        /// Try to validate token or get new one using login and password
        /// </summary>
        /// <param name="config">Config to validate</param>
        /// <param name="proxyHolder">Container for proxy, which should be used for request</param>
        /// <returns>True if succesful</returns>
        public static bool ValidateOrLogin(FileYandexConfig config,
                                             IYandexProxyHolder? proxyHolder = null) 
        {
            var token = config.YandexToken;
            if (string.IsNullOrWhiteSpace(token) || !ValidateTokenAsync(token, proxyHolder).GetAwaiter().GetResult()) {
                if (Login(config.YandexLogin, config.YandexPassword, out token, proxyHolder))
                {
                    config.YandexToken = token;
                    return true;
                } else return false;
            } else return true;
        }
    }
}