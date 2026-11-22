using System;
using System.Collections.Generic;
using System.Globalization;

namespace XrayLite
{
    public sealed class VlessProfile
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Uuid { get; set; }
        public string Sni { get; set; }
        public string PublicKey { get; set; }
        public string ShortId { get; set; }
        public string Flow { get; set; }
        public string Fingerprint { get; set; }
        public string SpiderX { get; set; }

        public VlessProfile()
        {
            Flow = "xtls-rprx-vision";
            Fingerprint = "chrome";
            SpiderX = "/";
            Port = 443;
        }

        public string ServerText
        {
            get { return Host + ":" + Port.ToString(CultureInfo.InvariantCulture); }
        }

        public static VlessProfile Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new FormatException("链接为空");
            url = url.Trim();
            if (!url.StartsWith("vless://", StringComparison.OrdinalIgnoreCase))
                throw new FormatException("只支持 vless:// 链接");

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                throw new FormatException("链接格式不正确");

            string user = Uri.UnescapeDataString(uri.UserInfo ?? string.Empty);
            if (string.IsNullOrWhiteSpace(user))
                throw new FormatException("缺少 UUID");

            Dictionary<string, string> query = ParseQuery(uri.Query);
            string sni = Query(query, "sni");
            string pbk = Query(query, "pbk");
            string sid = Query(query, "sid");
            string flow = Query(query, "flow");
            string fp = Query(query, "fp");
            string spx = Query(query, "spx");
            string name = Uri.UnescapeDataString((uri.Fragment ?? string.Empty).TrimStart('#'));

            if (string.IsNullOrWhiteSpace(sni))
                throw new FormatException("缺少 sni");
            if (string.IsNullOrWhiteSpace(pbk))
                throw new FormatException("缺少 pbk（REALITY 公钥）");

            return new VlessProfile
            {
                Name = string.IsNullOrWhiteSpace(name) ? uri.Host : name,
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 443,
                Uuid = user,
                Sni = sni,
                PublicKey = pbk,
                ShortId = sid ?? string.Empty,
                Flow = string.IsNullOrWhiteSpace(flow) ? "xtls-rprx-vision" : flow,
                Fingerprint = string.IsNullOrWhiteSpace(fp) ? "chrome" : fp,
                SpiderX = string.IsNullOrWhiteSpace(spx) ? "/" : spx
            };
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
                return result;
            foreach (string part in query.TrimStart('?').Split('&'))
            {
                if (part.Length == 0)
                    continue;
                int index = part.IndexOf('=');
                string key = index >= 0 ? part.Substring(0, index) : part;
                string value = index >= 0 ? part.Substring(index + 1) : string.Empty;
                result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
            return result;
        }

        private static string Query(Dictionary<string, string> query, string key)
        {
            string value;
            return query.TryGetValue(key, out value) ? value : null;
        }
    }
}
