# Xray Lite Android（开发中）

目标：基于 Xray-core 的 VLESS + REALITY + TUN Android 客户端，界面类似 WireGuard。

当前进度：

- 已建立原生 Android 项目（Java，无第三方 UI 依赖）；
- 已内置 Xray Android 原生二进制（x86_64 / arm64-v8a）；
- 已实现 VpnService、TUN 建立、VLESS 链接解析和简单界面；
- 已通过 APK 编译；
- **尚未通过真机/模拟器端到端验证**：Android 的 TUN FD 通过 `XRAY_TUN_FD`
  传给子进程时，当前被 `bad file descriptor` 拒绝，需要改用 libXray 或
  自定义 fd 传递方式。请暂时使用 v2rayNG。

因此本目录的 APK **不要作为可用版本分发**。

后续工作：

1. 用 libXray/gomobile 在应用进程内运行 Xray，或实现可靠的 FD 传递；
2. Android 14/15 前台服务类型与 VpnService 兼容；
3. 真机测试 YouTube/百度/DNS 泄漏；
4. 与 Windows 客户端统一界面。
