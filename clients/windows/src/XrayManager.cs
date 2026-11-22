using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace XrayLite
{
    public sealed class XrayManager
    {
        private readonly string baseDirectory;
        private Process process;
        private readonly object logLock = new object();
        private string logPath;

        public XrayManager(string baseDirectory)
        {
            this.baseDirectory = baseDirectory;
        }

        public string XrayPath
        {
            get { return Path.Combine(baseDirectory, "xray.exe"); }
        }

        public bool IsRunning
        {
            get
            {
                try
                {
                    return process != null && !process.HasExited;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public void Start(VlessProfile profile)
        {
            Stop();
            if (!File.Exists(XrayPath))
                throw new FileNotFoundException("缺少 xray.exe，请重新下载完整客户端压缩包。", XrayPath);

            string config = BuildConfig(profile);
            string configPath = Path.Combine(baseDirectory, "config.json");
            File.WriteAllText(configPath, config, new UTF8Encoding(false));
            logPath = Path.Combine(baseDirectory, "xray.log");
            try { File.WriteAllText(logPath, string.Empty, new UTF8Encoding(false)); } catch (Exception) { }

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = XrayPath;
            info.Arguments = "run -config \"" + configPath + "\"";
            info.WorkingDirectory = baseDirectory;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            process = new Process();
            process.StartInfo = info;
            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data)) AppendLog(args.Data);
            };
            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data)) AppendLog(args.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Thread.Sleep(4000);
            if (process.HasExited)
            {
                int code = process.ExitCode;
                string log = ReadLogTail();
                process = null;
                throw new InvalidOperationException(
                    "Xray 内核启动失败（退出码 " + code + "）。\r\n" + log);
            }
        }

        public void Stop()
        {
            if (process == null)
                return;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                try { process.Dispose(); } catch (Exception) { }
                process = null;
            }
        }

        private void AppendLog(string line)
        {
            try
            {
                lock (logLock)
                {
                    if (!string.IsNullOrEmpty(logPath))
                        File.AppendAllText(logPath, line + "\r\n", new UTF8Encoding(false));
                }
            }
            catch (Exception)
            {
            }
        }

        private string ReadLogTail()
        {
            try
            {
                if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
                    return string.Empty;
                string text = File.ReadAllText(logPath);
                return text.Length > 3000 ? text.Substring(text.Length - 3000) : text;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string BuildConfig(VlessProfile profile)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"log\": { \"loglevel\": \"warning\" },");
            builder.AppendLine("  \"inbounds\": [");
            builder.AppendLine("    {");
            builder.AppendLine("      \"protocol\": \"tun\",");
            builder.AppendLine("      \"settings\": {");
            builder.AppendLine("        \"name\": \"xray0\",");
            builder.AppendLine("        \"desc\": \"XrayLite\",");
            builder.AppendLine("        \"mtu\": 1500,");
            builder.AppendLine("        \"gateway\": [ \"10.0.0.1/16\" ],");
            builder.AppendLine("        \"dns\": [ \"1.1.1.1\", \"8.8.8.8\" ],");
            builder.AppendLine("        \"autoSystemRoutingTable\": [ \"0.0.0.0/0\" ],");
            builder.AppendLine("        \"autoOutboundsInterface\": \"auto\",");
            builder.AppendLine("        \"autoSystemWfpBlockLeak\": [ \"dns\", \"misconfigtun\" ]");
            builder.AppendLine("      }");
            builder.AppendLine("    }");
            builder.AppendLine("  ],");
            builder.AppendLine("  \"outbounds\": [");
            builder.AppendLine("    {");
            builder.AppendLine("      \"protocol\": \"vless\",");
            builder.AppendLine("      \"settings\": {");
            builder.AppendLine("        \"vnext\": [");
            builder.AppendLine("          {");
            builder.AppendLine("            \"address\": " + Json(profile.Host) + ",");
            builder.AppendLine("            \"port\": " + profile.Port + ",");
            builder.AppendLine("            \"users\": [");
            builder.AppendLine("              {");
            builder.AppendLine("                \"id\": " + Json(profile.Uuid) + ",");
            builder.AppendLine("                \"encryption\": \"none\",");
            builder.AppendLine("                \"flow\": " + Json(profile.Flow));
            builder.AppendLine("              }");
            builder.AppendLine("            ]");
            builder.AppendLine("          }");
            builder.AppendLine("        ]");
            builder.AppendLine("      },");
            builder.AppendLine("      \"streamSettings\": {");
            builder.AppendLine("        \"network\": \"tcp\",");
            builder.AppendLine("        \"security\": \"reality\",");
            builder.AppendLine("        \"realitySettings\": {");
            builder.AppendLine("          \"serverName\": " + Json(profile.Sni) + ",");
            builder.AppendLine("          \"fingerprint\": " + Json(profile.Fingerprint) + ",");
            builder.AppendLine("          \"publicKey\": " + Json(profile.PublicKey) + ",");
            builder.AppendLine("          \"shortId\": " + Json(profile.ShortId) + ",");
            builder.AppendLine("          \"spiderX\": " + Json(profile.SpiderX));
            builder.AppendLine("        }");
            builder.AppendLine("      }");
            builder.AppendLine("    }");
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string Json(string value)
        {
            if (value == null)
                value = string.Empty;
            StringBuilder builder = new StringBuilder("\"");
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 32)
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append("\"");
            return builder.ToString();
        }
    }
}
