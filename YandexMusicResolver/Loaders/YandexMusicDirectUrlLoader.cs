﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using YandexMusicResolver.Config;
using YandexMusicResolver.Requests;
using YandexMusicResolver.Responces;

namespace YandexMusicResolver.Loaders {
    public class YandexMusicDirectUrlLoader {
        private IYandexConfig _config;

        public YandexMusicDirectUrlLoader(IYandexConfig config) {
            _config = config;
        }

        private const string TrackDownloadInfoFormat = "https://api.music.yandex.net/tracks/{0}/download-info";
        private const string DirectUrlFormat = "https://{0}/get-{1}/{2}/{3}{4}";
        private const string Mp3Salt = "XGRlBW9FXlekgbPrRHuSiA";

        public async Task<string> GetDirectUrl(string trackId, string codec) {
            var trackDownloadInfos = await new YandexCustomRequest(_config).Create(string.Format(TrackDownloadInfoFormat, trackId)).GetResponseAsync<List<MetaTrackDownloadInfo>>();
            var track = trackDownloadInfos.FirstOrDefault(downloadInfo => downloadInfo.Codec == codec);
            if (track == null) {
                throw new Exception("Couldn't find supported track format.");
            }

            var downloadInfoContent = await new YandexCustomRequest(_config).Create(track.DownloadInfoUrl.ToString()).GetResponseBodyAsync();
            var serializer = new XmlSerializer(typeof(MetaTrackDownloadInfoXml));
            using var reader = new StringReader(downloadInfoContent);
            var info = (MetaTrackDownloadInfoXml) serializer.Deserialize(reader);

            var sign = Utilities.CreateMd5(Mp3Salt + info.Path.Substring(1) + info.S);

            return string.Format(DirectUrlFormat, info.Host, codec, sign, info.Ts, info.Path);
        }
    }
}