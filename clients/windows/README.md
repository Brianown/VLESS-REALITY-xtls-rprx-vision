# Windows 客户端

当前推荐使用 **v2rayN**：

1. 下载：<https://github.com/2dust/v2rayN/releases/latest>
2. 解压运行（如缺少 .NET 8 Desktop Runtime，按提示安装）；
3. 复制服务器上的 `vless-url.txt` 内容；
4. v2rayN → 服务器 → 从剪贴板导入；
5. 选中节点；
6. 打开 **TUN 模式**（需要管理员权限）；
7. 浏览器测试 YouTube。

TUN 模式会接管全局流量；如果只想让浏览器走代理，可以改用系统代理模式。

## 关于 LiteTunnel 同款界面

本仓库暂时不包含 WireGuard 同款 GUI。把 LiteTunnel 界面移植到 Xray/sing-box
需要独立的 Windows TUN 客户端工程，后续再实现；现在用 v2rayN 的 TUN 模式即可达到
TLS 1.3 REALITY + 全局代理的效果。
