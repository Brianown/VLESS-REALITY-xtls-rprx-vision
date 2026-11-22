package com.xraylite.app;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.net.VpnService;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

public final class MainActivity extends Activity {
    private static final int VPN_REQUEST = 1001;
    private EditText urlInput;
    private TextView status;
    private Button connectButton;
    private Button disconnectButton;
    private final Handler handler = new Handler(Looper.getMainLooper());

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        buildUi();
        SharedPreferences prefs = getSharedPreferences("xray", Context.MODE_PRIVATE);
        urlInput.setText(prefs.getString("url", ""));
        refreshStatus();
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                refreshStatus();
                handler.postDelayed(this, 1000);
            }
        }, 1000);
    }

    private int dp(int value) {
        return (int) (value * getResources().getDisplayMetrics().density + 0.5f);
    }

    private void buildUi() {
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(Color.rgb(248, 250, 252));
        root.setPadding(dp(22), dp(26), dp(22), dp(20));
        setContentView(root);

        TextView title = new TextView(this);
        title.setText("Xray Lite");
        title.setTextSize(30);
        title.setTextColor(Color.rgb(22, 119, 255));
        title.setTypeface(null, android.graphics.Typeface.BOLD);
        root.addView(title);

        TextView subtitle = new TextView(this);
        subtitle.setText("VLESS · REALITY · TLS 1.3 · TUN");
        subtitle.setTextSize(13);
        subtitle.setTextColor(Color.rgb(100, 116, 139));
        subtitle.setPadding(0, dp(4), 0, dp(22));
        root.addView(subtitle);

        TextView label = new TextView(this);
        label.setText("VLESS 链接");
        label.setTextSize(14);
        label.setTextColor(Color.rgb(15, 23, 42));
        root.addView(label);

        urlInput = new EditText(this);
        urlInput.setHint("vless://...");
        urlInput.setTextSize(13);
        urlInput.setSingleLine(false);
        urlInput.setMinLines(3);
        urlInput.setMaxLines(5);
        urlInput.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        urlInput.setBackgroundColor(Color.WHITE);
        urlInput.setPadding(dp(12), dp(10), dp(12), dp(10));
        LinearLayout.LayoutParams inputParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        inputParams.topMargin = dp(8);
        root.addView(urlInput, inputParams);

        LinearLayout buttons = new LinearLayout(this);
        buttons.setOrientation(LinearLayout.HORIZONTAL);
        buttons.setPadding(0, dp(16), 0, dp(14));
        root.addView(buttons);

        connectButton = new Button(this);
        connectButton.setText("连接");
        connectButton.setAllCaps(false);
        connectButton.setTextColor(Color.WHITE);
        connectButton.setBackgroundColor(Color.rgb(22, 119, 255));
        connectButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                connect();
            }
        });
        buttons.addView(connectButton, new LinearLayout.LayoutParams(0, dp(48), 1f));

        disconnectButton = new Button(this);
        disconnectButton.setText("断开");
        disconnectButton.setAllCaps(false);
        disconnectButton.setTextColor(Color.rgb(15, 23, 42));
        disconnectButton.setBackgroundColor(Color.WHITE);
        disconnectButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                stopVpn();
            }
        });
        LinearLayout.LayoutParams disconnectParams = new LinearLayout.LayoutParams(0, dp(48), 1f);
        disconnectParams.leftMargin = dp(10);
        buttons.addView(disconnectButton, disconnectParams);

        status = new TextView(this);
        status.setTextSize(14);
        status.setTextColor(Color.rgb(15, 23, 42));
        status.setPadding(dp(14), dp(14), dp(14), dp(14));
        status.setBackgroundColor(Color.WHITE);
        root.addView(status);

        TextView footer = new TextView(this);
        footer.setText("TUN 全局模式 · 只使用 Xray 内核\nDNS 泄漏和 IPv6 由 Xray TUN 处理");
        footer.setTextSize(12);
        footer.setTextColor(Color.rgb(100, 116, 139));
        footer.setPadding(0, dp(18), 0, 0);
        root.addView(footer);
    }

    private void connect() {
        String url = urlInput.getText().toString().trim();
        if (!url.startsWith("vless://")) {
            status.setText("请粘贴完整的 vless:// 链接");
            return;
        }
        getSharedPreferences("xray", Context.MODE_PRIVATE).edit().putString("url", url).apply();
        Intent prepare = VpnService.prepare(this);
        if (prepare != null) {
            startActivityForResult(prepare, VPN_REQUEST);
        } else {
            startVpn(url);
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == VPN_REQUEST && resultCode == RESULT_OK) {
            startVpn(urlInput.getText().toString().trim());
        } else if (requestCode == VPN_REQUEST) {
            status.setText("VPN 授权被拒绝");
        }
    }

    private void startVpn(String url) {
        Intent intent = new Intent(this, XrayVpnService.class);
        intent.putExtra("url", url);
        startForegroundService(intent);
    }

    private void stopVpn() {
        Intent intent = new Intent(this, XrayVpnService.class);
        intent.setAction(XrayVpnService.ACTION_STOP);
        startService(intent);
    }

    private void refreshStatus() {
        boolean running = XrayVpnService.isRunning;
        String error = XrayVpnService.lastError;
        if (running) {
            status.setText("已连接\nTUN 全局模式 · TLS 1.3 REALITY");
            status.setTextColor(Color.rgb(22, 119, 255));
        } else if (error != null && error.length() > 0) {
            status.setText("未连接\n" + error);
            status.setTextColor(Color.rgb(180, 30, 30));
        } else {
            status.setText("未连接\n粘贴 VLESS 链接后点击连接");
            status.setTextColor(Color.rgb(15, 23, 42));
        }
        connectButton.setEnabled(!running);
        disconnectButton.setEnabled(running);
    }

    @Override
    protected void onDestroy() {
        handler.removeCallbacksAndMessages(null);
        super.onDestroy();
    }
}
