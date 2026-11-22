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

### Windows：Xray Lite

本仓库包含一个基于 Xray-core TUN 的简单 Windows 客户端：

<https://github.com/Brianown/VLESS-REALITY-xtls-rprx-vision/raw/main/release/XrayLite-Windows.zip>

- 解压后运行 `XrayLite.exe`（需要管理员权限）；
- 导入 `vless://` 链接；
- 连接后使用 Xray TUN 全局模式；
- 不使用 WireGuard。

### Android

Android 客户端仍在开发中，当前请使用 v2rayNG：

<https://github.com/2dust/v2rayNG/releases/latest>

v2rayNG 导入 `vless://` 链接后使用 VpnService/TUN 即可。

### 关于 LiteTunnel 同款界面

Windows 已提供类似 WireGuard 的简洁界面；Android 界面尚未完成端到端验证。


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
