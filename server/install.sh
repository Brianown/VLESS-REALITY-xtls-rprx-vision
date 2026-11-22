#!/usr/bin/env bash
# VLESS + REALITY + xtls-rprx-vision 一键安装脚本
# 适用：Debian/Ubuntu、RHEL/CentOS/Rocky/Alma/Fedora、openSUSE、Arch（systemd）
# 只需要 TCP 443，不需要内核模块、不需要 WireGuard。
#
# 可选环境变量：
#   XRAY_PORT=443
#   SERVER_NAME=www.apple.com     # 伪装目标；也可设为 random
#   UUID=...                      # 固定 UUID，默认自动生成
#   SHORT_ID=...                  # 固定 shortId，默认自动生成
#   SKIP_DEPS=1                   # 跳过依赖安装
set -Eeuo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BOLD='\033[1m'; RESET='\033[0m'
info() { printf '%b[INFO]%b %s\n' "${YELLOW}" "${RESET}" "$*"; }
ok() { printf '%b[ OK ]%b %s\n' "${GREEN}" "${RESET}" "$*"; }
warn() { printf '%b[WARN]%b %s\n' "${YELLOW}" "${RESET}" "$*"; }
die() { printf '%b[FAILED]%b %s\n' "${RED}" "${RESET}" "$*" >&2; exit 1; }
trap 'die "脚本在第 $LINENO 行执行失败，请检查上面的错误信息。"' ERR

[[ ${EUID} -eq 0 ]] || die "请使用 root 运行：sudo bash server/install.sh"
command -v systemctl >/dev/null 2>&1 || die "需要 systemd。"

XRAY_PORT="${XRAY_PORT:-443}"
SERVER_NAME="${SERVER_NAME:-www.apple.com}"
SKIP_DEPS="${SKIP_DEPS:-0}"

[[ ${XRAY_PORT} =~ ^[0-9]+$ ]] || die "XRAY_PORT 必须是数字"
(( 10#${XRAY_PORT} >= 1 && 10#${XRAY_PORT} <= 65535 )) || die "XRAY_PORT 必须是 1-65535"
[[ ${SERVER_NAME} =~ ^[A-Za-z0-9.-]+$ ]] || die "SERVER_NAME 只能是域名"

SCRIPT_DIR=""
if [[ -n ${BASH_SOURCE[0]:-} && -f ${BASH_SOURCE[0]} ]]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
fi

if [[ ${SERVER_NAME} == "random" ]]; then
    if [[ -n ${SCRIPT_DIR} && -f ${SCRIPT_DIR}/cover-domains.txt ]]; then
        SERVER_NAME="$(shuf -n 1 "${SCRIPT_DIR}/cover-domains.txt" 2>/dev/null || true)"
    fi
    [[ -n ${SERVER_NAME} ]] || SERVER_NAME="www.apple.com"
fi
info "伪装目标：${SERVER_NAME}，Xray 端口：${XRAY_PORT}/tcp"

# ---------------------------------------------------------------- 依赖

detect_pm() {
    if command -v apt-get >/dev/null 2>&1; then PM=apt
    elif command -v dnf >/dev/null 2>&1; then PM=dnf
    elif command -v yum >/dev/null 2>&1; then PM=yum
    elif command -v zypper >/dev/null 2>&1; then PM=zypper
    elif command -v pacman >/dev/null 2>&1; then PM=pacman
    else PM=unknown; fi
}

install_deps() {
    detect_pm
    info "检测到包管理器：${PM}"
    case "${PM}" in
        apt)
            export DEBIAN_FRONTEND=noninteractive
            apt-get update -qq
            apt-get install -y -qq --no-install-recommends curl python3 unzip ca-certificates qrencode >/dev/null || \
                die "依赖安装失败"
            ;;
        dnf)
            "${PM}" install -y curl python3 unzip ca-certificates qrencode >/dev/null 2>&1 || \
                "${PM}" install -y curl python3 unzip ca-certificates >/dev/null
            ;;
        yum)
            yum install -y curl python3 unzip ca-certificates >/dev/null 2>&1 || \
                yum install -y curl python3 unzip ca-certificates >/dev/null
            yum install -y qrencode >/dev/null 2>&1 || warn "qrencode 未安装，二维码将不可用。"
            ;;
        zypper)
            zypper --non-interactive install curl python3 unzip ca-certificates qrencode >/dev/null
            ;;
        pacman)
            pacman -Sy --noconfirm --needed curl python unzip ca-certificates qrencode >/dev/null
            ;;
        *)
            die "未识别的包管理器，请手动安装 curl/python3/unzip 后使用 SKIP_DEPS=1。"
            ;;
    esac
}

if [[ ${SKIP_DEPS} == "1" ]]; then
    warn "SKIP_DEPS=1，跳过依赖安装。"
else
    install_deps
fi
for c in curl python3 systemctl; do
    command -v "${c}" >/dev/null 2>&1 || die "缺少命令 ${c}"
done

PYTHON_BIN="$(command -v python3 || command -v python || true)"
[[ -n ${PYTHON_BIN} ]] || die "缺少 Python 3"

# ---------------------------------------------------------------- 端口检查

if ss -lnt 2>/dev/null | awk '{print $4}' | grep -qE "[:.]${XRAY_PORT}$"; then
    die "TCP ${XRAY_PORT} 已被占用（可能是 nginx）。请先释放该端口，或用 XRAY_PORT=8443 重新运行。"
fi

# ---------------------------------------------------------------- Xray

ARCH="$(uname -m)"
case "${ARCH}" in
    x86_64|amd64) XRAY_ZIP="Xray-linux-64.zip" ;;
    aarch64|arm64) XRAY_ZIP="Xray-linux-arm64-v8a.zip" ;;
    *) die "不支持的 CPU 架构：${ARCH}" ;;
esac

info "下载 Xray-core（${XRAY_ZIP}）……"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT
curl -fL --connect-timeout 15 --max-time 300 \
    -o "${TMP_DIR}/xray.zip" \
    "https://github.com/XTLS/Xray-core/releases/latest/download/${XRAY_ZIP}"
python3 -m zipfile -e "${TMP_DIR}/xray.zip" "${TMP_DIR}/xray"
install -d -m 755 /usr/local/bin /usr/local/etc/xray /var/log/xray
install -m 755 "${TMP_DIR}/xray/xray" /usr/local/bin/xray
/usr/local/bin/xray version | head -n 1

# ---------------------------------------------------------------- 密钥

UUID="${UUID:-$(/usr/local/bin/xray uuid)}"
KEY_OUT="$(/usr/local/bin/xray x25519)"
PRIVATE_KEY="$(printf '%s\n' "${KEY_OUT}" | awk -F': ' '/PrivateKey:/ {print $2; exit}')"
PUBLIC_KEY="$(printf '%s\n' "${KEY_OUT}" | awk -F': ' '/PublicKey:/ {print $2; exit}')"
[[ -n ${PRIVATE_KEY} && -n ${PUBLIC_KEY} ]] || die "生成 REALITY 密钥失败"
SHORT_ID="${SHORT_ID:-$("${PYTHON_BIN}" -c 'import secrets; print(secrets.token_hex(8))')}"

# ---------------------------------------------------------------- 配置

cat > /usr/local/etc/xray/config.json <<EOF
{
  "log": { "loglevel": "warning" },
  "inbounds": [
    {
      "port": ${XRAY_PORT},
      "listen": "0.0.0.0",
      "protocol": "vless",
      "settings": {
        "clients": [
          { "id": "${UUID}", "flow": "xtls-rprx-vision" }
        ],
        "decryption": "none"
      },
      "streamSettings": {
        "network": "tcp",
        "security": "reality",
        "realitySettings": {
          "show": false,
          "dest": "${SERVER_NAME}:443",
          "xver": 0,
          "serverNames": [ "${SERVER_NAME}" ],
          "privateKey": "${PRIVATE_KEY}",
          "shortIds": [ "${SHORT_ID}" ]
        }
      },
      "sniffing": {
        "enabled": true,
        "destOverride": [ "http", "tls", "quic" ],
        "routeOnly": true
      }
    }
  ],
  "outbounds": [
    { "protocol": "freedom", "tag": "direct" }
  ]
}
EOF
chmod 600 /usr/local/etc/xray/config.json
/usr/local/bin/xray -test -config /usr/local/etc/xray/config.json >/dev/null || die "Xray 配置校验失败"

cat > /etc/systemd/system/xray.service <<'EOF'
[Unit]
Description=Xray VLESS REALITY
After=network.target

[Service]
Type=simple
ExecStart=/usr/local/bin/xray run -config /usr/local/etc/xray/config.json
Restart=on-failure
RestartSec=3
LimitNOFILE=65535

[Install]
WantedBy=multi-user.target
EOF
systemctl daemon-reload
systemctl enable --now xray.service >/dev/null 2>&1 || true
sleep 1
systemctl is-active --quiet xray.service || die "Xray 启动失败，请查看：journalctl -u xray -n 50"
ok "Xray 已启动并监听 TCP ${XRAY_PORT}"

# ---------------------------------------------------------------- 防火墙

if command -v ufw >/dev/null 2>&1 && ufw status 2>/dev/null | grep -q "Status: active"; then
    ufw allow "${XRAY_PORT}/tcp" >/dev/null 2>&1 || true
    ok "已放行 UFW TCP ${XRAY_PORT}"
fi
if command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
    firewall-cmd --permanent --add-port="${XRAY_PORT}/tcp" >/dev/null 2>&1 || true
    firewall-cmd --reload >/dev/null 2>&1 || true
    ok "已放行 firewalld TCP ${XRAY_PORT}"
fi

# ---------------------------------------------------------------- 客户端

CLIENT_DIR="/root/vless-reality"
mkdir -p "${CLIENT_DIR}"
chmod 700 "${CLIENT_DIR}"

PUBLIC_IP="$(curl -4fsS --max-time 10 https://api.ipify.org 2>/dev/null || true)"
[[ -n ${PUBLIC_IP} ]] || PUBLIC_IP="$(curl -4fsS --max-time 10 https://ifconfig.me 2>/dev/null || true)"
[[ -n ${PUBLIC_IP} ]] || die "无法检测公网 IPv4，请手动填写客户端地址。"

VLESS_URL="vless://${UUID}@${PUBLIC_IP}:${XRAY_PORT}?encryption=none&flow=xtls-rprx-vision&security=reality&sni=${SERVER_NAME}&fp=chrome&pbk=${PUBLIC_KEY}&sid=${SHORT_ID}&spx=%2F&type=tcp&headerType=none#Reality-${SERVER_NAME}"

cat > "${CLIENT_DIR}/vless-url.txt" <<EOF
${VLESS_URL}
EOF
chmod 600 "${CLIENT_DIR}/vless-url.txt"

cat > "${CLIENT_DIR}/xray-client.json" <<EOF
{
  "log": { "loglevel": "warning" },
  "inbounds": [
    { "listen": "127.0.0.1", "port": 10808, "protocol": "socks", "settings": { "udp": true } },
    { "listen": "127.0.0.1", "port": 10809, "protocol": "http" }
  ],
  "outbounds": [
    {
      "protocol": "vless",
      "settings": {
        "vnext": [
          {
            "address": "${PUBLIC_IP}",
            "port": ${XRAY_PORT},
            "users": [
              { "id": "${UUID}", "encryption": "none", "flow": "xtls-rprx-vision" }
            ]
          }
        ]
      },
      "streamSettings": {
        "network": "tcp",
        "security": "reality",
        "realitySettings": {
          "serverName": "${SERVER_NAME}",
          "fingerprint": "chrome",
          "publicKey": "${PUBLIC_KEY}",
          "shortId": "${SHORT_ID}",
          "spiderX": "/"
        }
      }
    }
  ]
}
EOF
chmod 600 "${CLIENT_DIR}/xray-client.json"

if command -v qrencode >/dev/null 2>&1; then
    qrencode -o "${CLIENT_DIR}/vless.png" -s 8 -m 2 < "${CLIENT_DIR}/vless-url.txt" || \
        warn "二维码生成失败，但链接已保存。"
    chmod 600 "${CLIENT_DIR}/vless.png" 2>/dev/null || true
fi

printf '\n%s安装完成%s\n' "${GREEN}${BOLD}" "${RESET}"
cat <<EOF
服务器地址 : ${PUBLIC_IP}
端口       : ${XRAY_PORT}/tcp
伪装目标   : ${SERVER_NAME}
UUID       : ${UUID}
公钥       : ${PUBLIC_KEY}
ShortId    : ${SHORT_ID}
客户端链接 : ${CLIENT_DIR}/vless-url.txt
客户端配置 : ${CLIENT_DIR}/xray-client.json
二维码     : ${CLIENT_DIR}/vless.png

使用方式：
  Windows：v2rayN 导入上面的 VLESS 链接，开 TUN 模式
  Android：v2rayNG 扫码或导入链接，连接即可
  Xray 核心：xray run -config ${CLIENT_DIR}/xray-client.json
            本地 HTTP 127.0.0.1:10809 / SOCKS5 127.0.0.1:10808

注意：
  1. 云厂商安全组还需放行 TCP ${XRAY_PORT}；
  2. 上面的链接包含凭据，不要发到公开群聊；
  3. 443 端口的伪装效果最好；如果 443 被 nginx 占用，可改用 XRAY_PORT=8443。
EOF
