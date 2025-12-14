package com.ddrubok.wegetgame;

import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import android.graphics.Color; // 색상 변경을 위해 추가
import org.json.JSONObject;

public class TinyCapsuleWidget extends AppWidgetProvider {
    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);
        // 브로드캐스트 수신
        if ("com.ddrubok.wegetgame.ACTION_WIDGET_UPDATE".equals(intent.getAction())) {
            updateWidget(context, AppWidgetManager.getInstance(context), intent.getStringExtra("EXTRA_DATA_JSON"));
        }
    }

    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        updateWidget(context, appWidgetManager, null);
    }

    private void updateWidget(Context context, AppWidgetManager appWidgetManager, String json) {
        int layoutId = context.getResources().getIdentifier("widget_layout", "layout", context.getPackageName());
        int textId = context.getResources().getIdentifier("widget_text", "id", context.getPackageName());
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {
            try {
                JSONObject data = new JSONObject(json);
                String state = data.optString("state");     // HAPPY, SAD, NORMAL
                String message = data.optString("message"); // 텍스트 내용
                
                // [로직] 상태에 따라 이모지와 글자 색상 변경
                String displayCheck = "";
                int textColor = Color.BLACK;

                if ("HAPPY".equals(state)) {
                    displayCheck = "🧅✨ " + message;       // 반짝이는 양파
                    textColor = Color.parseColor("#2E7D32"); // 진한 초록색
                } else if ("SAD".equals(state)) {
                    displayCheck = "🧅💦 " + message;       // 우는 양파
                    textColor = Color.parseColor("#C62828"); // 진한 빨간색
                } else {
                    displayCheck = "🧅 " + message;          // 평범 양파
                    textColor = Color.BLACK;
                }

                views.setTextViewText(textId, displayCheck);
                views.setTextColor(textId, textColor);

            } catch (Exception e) { 
                views.setTextViewText(textId, "Error"); 
            }
        } else {
            // 초기 상태
            views.setTextViewText(textId, "🧅 양파를 심었습니다.");
            views.setTextColor(textId, Color.BLACK);
        }
        
        try {
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        } catch (Exception e) { }
    }
}