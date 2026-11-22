# VLESS + REALITY + xtls-rprx-vision

一条命令安装 Xray REALITY 服务端；TLS 1.3 伪装、VLESS + Vision，不需要 WireGuard 内核模块。

> 这不是“绝对匿名”工具。服务器仍然能看到你访问的目标 IP/域名，HTTPS 内容不会被服务器解密；
> 请遵守所在地区法律，仅用于合法的网络访问与隐私保护。

## 快速开始

服务器要求：

- 一台有公网 IPv4 的 Linux VPS；
- 建议 **TCP 443** 空闲（443 伪装效果最好）；
- 支持 systemd；
- root 权限。

一条命令（整行复制，不要拆开）：

```bash
curl -fsSL https://github.com/Brianown/VLESS-REALITY-xtls-rprx-vision/archive/refs/heads/main.tar.gz -o /tmp/vless.tar.gz && sudo tar xzf /tmp/vless.tar.gz -C /opt && sudo bash /opt/VLESS-REALITY-xtls-rprx-vision-main/server/install.sh
```

安装完成后会打印：

- 服务器公网 IP、端口；
- 伪装域名；
- UUID、REALITY 公钥、ShortId；
- `vless://` 客户端链接；
- 二维码。

文件位置：

```text
/root/vless-reality/vless-url.txt
/root/vless-reality/xray-client.json
/root/vless-reality/vless.png
```

> 云厂商安全组还需要放行 **TCP 443**（或你自定义的 `XRAY_PORT`）。

## 自定义

```bash
# 随机选择一个伪装域名
sudo SERVER_NAME=random bash /opt/VLESS-REALITY-xtls-rprx-vision-main/server/install.sh

# 指定伪装域名
sudo SERVER_NAME=www.apple.com bash /opt/VLESS-REALITY-xtls-rprx-vision-main/server/install.sh

# 443 被 nginx 占用时
sudo XRAY_PORT=8443 bash /opt/VLESS-REALITY-xtls-rprx-vision-main/server/install.sh
```

`server/cover-domains.txt` 是候选伪装域名列表，可用 `SERVER_NAME=random` 随机选择。

## 客户端

当前推荐使用支持 VLESS REALITY 的成熟客户端：

- Windows：[v2rayN](https://github.com/2dust/v2rayN/releases/latest)
- Android：[v2rayNG](https://github.com/2dust/v2rayNG/releases/latest)

导入 `/root/vless-reality/vless-url.txt` 中的链接，或扫描二维码。

Windows 建议开启 **TUN 模式**（需要管理员权限），Android 使用 v2rayNG 默认的 VpnService/TUN。

### 关于 LiteTunnel 同款界面

本项目当前提供的是**服务端与客户端配置**，不是 WireGuard 同款 GUI。
把 LiteTunnel 的界面完整移植到 Xray/sing-box 需要重写 Windows TUN 和 Android VpnService
的连接层，属于后续客户端工程；当前可以先使用 v2rayN/v2rayNG 的 TUN 模式。

## 目录

```text
server/
  install.sh          # 一键安装
  cover-domains.txt   # 候选伪装域名
docs/
  cover-domains.md    # 伪装域名选择说明
  privacy.md          # 加密边界
```

## 安全提醒

- 安装脚本会打印凭据，注意终端录屏和日志；
- 不要把 `vless-url.txt` 或二维码发到公开群聊；
- 定期更换 UUID/ShortId；
- 服务器被入侵时，任何代理都无法保证隐私；
- REALITY 只能降低被识别概率，不能保证“永远不会被封锁”。
