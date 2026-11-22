package com.xraylite.app;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Intent;
import android.content.pm.ServiceInfo;
import android.net.Uri;
import android.net.VpnService;
import android.os.Build;
import android.os.IBinder;
import android.os.ParcelFileDescriptor;
import android.system.Os;
import android.system.OsConstants;
import android.util.Log;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileWriter;
import java.io.InputStreamReader;
import java.net.URLDecoder;

public final class XrayVpnService extends VpnService {
    public static final String ACTION_STOP = "com.xraylite.app.STOP";
    private static final String CHANNEL_ID = "xray_vpn";
    private static final int NOTIFICATION_ID = 1;
    public static volatile boolean isRunning = false;
    public static volatile String lastError = "";

    private ParcelFileDescriptor tun;
    private Process process;
    private Thread reader;
    private volatile boolean stopping = false;

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        if (intent != null && ACTION_STOP.equals(intent.getAction())) {
            stopVpn();
            return START_NOT_STICKY;
        }
        if (isRunning) {
            return START_STICKY;
        }
        String url = intent == null ? null : intent.getStringExtra("url");
        if (url == null || !url.startsWith("vless://")) {
            lastError = "链接为空";
            stopSelf();
            return START_NOT_STICKY;
        }
        startForegroundNotification();
        startVpn(url);
        return START_STICKY;
    }

    private void startForegroundNotification() {
        NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    CHANNEL_ID, "Xray Lite", NotificationManager.IMPORTANCE_LOW);
            manager.createNotificationChannel(channel);
        }
        Intent open = new Intent(this, MainActivity.class);
        PendingIntent pending = PendingIntent.getActivity(this, 0, open,
                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
        Notification.Builder builder = Build.VERSION.SDK_INT >= Build.VERSION_CODES.O
                ? new Notification.Builder(this, CHANNEL_ID)
                : new Notification.Builder(this);
        builder.setContentTitle("Xray Lite")
                .setContentText("VLESS REALITY TUN 正在运行")
                .setSmallIcon(android.R.drawable.ic_dialog_info)
                .setContentIntent(pending)
                .setOngoing(true);
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(NOTIFICATION_ID, builder.build(),
                    ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC);
        } else {
            startForeground(NOTIFICATION_ID, builder.build());
        }
    }

    private void startVpn(String url) {
        try {
            VlessConfig config = VlessConfig.parse(url);
            Builder builder = new Builder();
            builder.setSession("Xray Lite");
            builder.addAddress("10.0.0.2", 30);
            builder.addRoute("0.0.0.0", 0);
            builder.addDnsServer("1.1.1.1");
            builder.addDnsServer("8.8.8.8");
            builder.setMtu(1500);
            // Xray 自己连接服务器的流量必须绕过 TUN，否则会回环。
            try {
                builder.addDisallowedApplication(getPackageName());
            } catch (Exception ignored) {
            }

            tun = builder.establish();
            if (tun == null) {
                throw new IllegalStateException("VPN 授权失败，TUN 未建立");
            }

            // 让子进程继承 TUN fd，并固定到 fd 3（Xray 要求 >=3）。
            try {
                java.io.FileDescriptor fd3 = Os.dup2(tun.getFileDescriptor(), 3);
                Os.fcntlInt(fd3, OsConstants.F_SETFD, 0);
            } catch (Exception e) {
                Log.w("XrayLite", "dup2 TUN fd failed", e);
            }

            File configFile = new File(getFilesDir(), "config.json");
            FileWriter writer = new FileWriter(configFile, false);
            writer.write(config.toXrayJson());
            writer.close();

            File binary = new File(getApplicationInfo().nativeLibraryDir, "libxray.so");
            if (!binary.exists()) {
                throw new IllegalStateException("缺少 libxray.so");
            }
            binary.setExecutable(true);

            ProcessBuilder pb = new ProcessBuilder(binary.getAbsolutePath(), "run",
                    "-config", configFile.getAbsolutePath());
            pb.environment().put("XRAY_TUN_FD", "3");
            pb.redirectErrorStream(true);
            process = pb.start();
            isRunning = true;
            lastError = "";

            reader = new Thread(new Runnable() {
                @Override
                public void run() {
                    StringBuilder tail = new StringBuilder();
                    try {
                        BufferedReader br = new BufferedReader(
                                new InputStreamReader(process.getInputStream()));
                        String line;
                        while ((line = br.readLine()) != null) {
                            Log.i("XrayLite", line);
                            tail.append(line).append('\n');
                            if (tail.length() > 3000) {
                                tail.delete(0, tail.length() - 3000);
                            }
                        }
                    } catch (Exception e) {
                        Log.w("XrayLite", "read xray output failed", e);
                    }
                    if (!stopping) {
                        lastError = "Xray 已退出\n" + tail.toString();
                        isRunning = false;
                        stopVpn();
                    }
                }
            }, "xray-log");
            reader.start();
        } catch (Exception e) {
            lastError = "连接失败：" + e.getMessage();
            isRunning = false;
            stopVpn();
        }
    }

    private synchronized void stopVpn() {
        if (stopping) {
            return;
        }
        stopping = true;
        isRunning = false;
        try {
            if (process != null) {
                process.destroy();
                process = null;
            }
        } catch (Exception ignored) {
        }
        try {
            if (tun != null) {
                tun.close();
                tun = null;
            }
        } catch (Exception ignored) {
        }
        try {
            stopForeground(true);
        } catch (Exception ignored) {
        }
        stopSelf();
    }

    @Override
    public void onRevoke() {
        stopVpn();
        super.onRevoke();
    }

    @Override
    public void onDestroy() {
        stopping = true;
        isRunning = false;
        try {
            if (process != null) {
                process.destroy();
                process = null;
            }
        } catch (Exception ignored) {
        }
        try {
            if (tun != null) {
                tun.close();
                tun = null;
            }
        } catch (Exception ignored) {
        }
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return super.onBind(intent);
    }

    private static final class VlessConfig {
        String host;
        int port;
        String uuid;
        String sni;
        String publicKey;
        String shortId;
        String flow;
        String fingerprint;
        String spiderX;

        static VlessConfig parse(String url) {
            Uri uri = Uri.parse(url);
            VlessConfig result = new VlessConfig();
            result.host = uri.getHost();
            result.port = uri.getPort() > 0 ? uri.getPort() : 443;
            result.uuid = uri.getUserInfo();
            result.sni = uri.getQueryParameter("sni");
            result.publicKey = uri.getQueryParameter("pbk");
            result.shortId = uri.getQueryParameter("sid");
            result.flow = uri.getQueryParameter("flow");
            result.fingerprint = uri.getQueryParameter("fp");
            result.spiderX = uri.getQueryParameter("spx");
            if (result.flow == null || result.flow.length() == 0) result.flow = "xtls-rprx-vision";
            if (result.fingerprint == null || result.fingerprint.length() == 0) result.fingerprint = "chrome";
            if (result.spiderX == null || result.spiderX.length() == 0) result.spiderX = "/";
            if (result.host == null || result.uuid == null || result.sni == null || result.publicKey == null) {
                throw new IllegalArgumentException("VLESS 链接不完整");
            }
            return result;
        }

        String toXrayJson() {
            StringBuilder b = new StringBuilder();
            b.append("{\n");
            b.append("  \"log\": {\"loglevel\": \"warning\"},\n");
            b.append("  \"inbounds\": [{\n");
            b.append("    \"protocol\": \"tun\",\n");
            b.append("    \"settings\": {\"name\": \"xray0\", \"mtu\": 1500}\n");
            b.append("  }],\n");
            b.append("  \"outbounds\": [{\n");
            b.append("    \"protocol\": \"vless\",\n");
            b.append("    \"settings\": {\"vnext\": [{\n");
            b.append("      \"address\": ").append(q(host)).append(",\n");
            b.append("      \"port\": ").append(port).append(",\n");
            b.append("      \"users\": [{\"id\": ").append(q(uuid))
                    .append(", \"encryption\": \"none\", \"flow\": ").append(q(flow)).append("}]\n");
            b.append("    }]},\n");
            b.append("    \"streamSettings\": {\n");
            b.append("      \"network\": \"tcp\",\n");
            b.append("      \"security\": \"reality\",\n");
            b.append("      \"realitySettings\": {\n");
            b.append("        \"serverName\": ").append(q(sni)).append(",\n");
            b.append("        \"fingerprint\": ").append(q(fingerprint)).append(",\n");
            b.append("        \"publicKey\": ").append(q(publicKey)).append(",\n");
            b.append("        \"shortId\": ").append(q(shortId)).append(",\n");
            b.append("        \"spiderX\": ").append(q(spiderX)).append("\n");
            b.append("      }\n");
            b.append("    }\n");
            b.append("  }]\n");
            b.append("}\n");
            return b.toString();
        }

        private static String q(String value) {
            if (value == null) value = "";
            return "\"" + value.replace("\\", "\\\\").replace("\"", "\\\"")
                    .replace("\r", "").replace("\n", "") + "\"";
        }
    }
}
