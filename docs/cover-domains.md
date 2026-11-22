# 伪装域名选择

REALITY 的 `dest` / `serverNames` 不是随便填一个域名就行，建议满足：

1. 支持 **TLS 1.3**；
2. 支持 **HTTP/2**；
3. 在你的网络里可以正常访问，没有被 GFW 重点封锁；
4. 在你服务器附近有 CDN 节点，握手延迟低；
5. 不是特别敏感的网站，避免主动探测时引起注意。

`server/cover-domains.txt` 是一份候选列表，可用：

```bash
sudo SERVER_NAME=random bash server/install.sh
```

随机选择。

## 测试一个域名是否适合

```bash
openssl s_client -connect www.apple.com:443 -servername www.apple.com -brief
```

看到：

```text
Protocol version: TLSv1.3
Verification: OK
```

说明 TLS 1.3 正常。

再看 HTTP/2：

```bash
curl -sI --http2 https://www.apple.com | head
```

## 重要限制

- 伪装域名只影响 **本地网络/GFW 看到的外层 TLS 握手**；
- 服务器到目标网站的流量仍然是普通 HTTPS；
- 域名列表不能保证在某一个地区一定可用，部署前最好自己测试；
- 如果 VPS IP 已经被重点标记，换域名也未必能恢复，需要换 IP。

## 候选列表

见 `server/cover-domains.txt`。默认使用 `www.apple.com`。
