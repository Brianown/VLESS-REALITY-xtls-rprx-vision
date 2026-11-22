# Xray Lite for Windows

基于 Xray-core 的 VLESS + REALITY + TUN 客户端，界面简洁，类似 WireGuard。

## 使用

1. 下载 `XrayLite-Windows.zip` 并解压；
2. 运行 `XrayLite.exe`，UAC 选择“是”（TUN 需要管理员权限）；
3. 点“导入链接”，粘贴服务器生成的 `vless://` 链接；
4. 选中节点 → 连接；
5. TUN 全局模式自动接管 IPv4，并阻止 DNS/IPv6 泄漏。

## 目录

```text
XrayLite.exe      # 界面
xray.exe          # Xray-core 内核
wintun.dll        # Windows TUN 驱动
config.json       # 连接时自动生成
xray.log          # 内核日志
```

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

需要 .NET Framework 4.x 的 `csc.exe`。构建产物在 `dist/`。

## 说明

- 只支持 Windows 10/11 x64；
- 只用 Xray 内核，不使用 WireGuard；
- 退出程序会停止 Xray 并移除 TUN。