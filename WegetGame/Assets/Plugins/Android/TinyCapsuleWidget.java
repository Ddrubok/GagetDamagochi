package com.ddrubok.wegetgame; // 패키지명 수정됨

import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import android.util.Log;
import org.json.JSONObject;

public class TinyCapsuleWidget extends AppWidgetProvider {

    // 유니티에서 보낼 신호 (패키지명 포함)
    private static final String ACTION_UPDATE = "com.ddrubok.wegetgame.ACTION_WIDGET_UPDATE";

    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);
        
        String action = intent.getAction();
        if (ACTION_UPDATE.equals(action)) {
            String jsonString = intent.getStringExtra("EXTRA_DATA_JSON");
            updateAllWidgets(context, jsonString);
        }
    }

    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        updateAllWidgets(context, null);
    }

    private void updateAllWidgets(Context context, String jsonString) {
        AppWidgetManager appWidgetManager = AppWidgetManager.getInstance(context);
        ComponentName thisWidget = new ComponentName(context, TinyCapsuleWidget.class);
        int[] allWidgetIds = appWidgetManager.getAppWidgetIds(thisWidget);

        for (int widgetId : allWidgetIds) {
            // 레이아웃 파일 연결
            int layoutId = context.getResources().getIdentifier("widget_layout", "layout", context.getPackageName());
            RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);

            if (jsonString != null) {
                try {
                    JSONObject data = new JSONObject(jsonString);
                    String state = data.optString("state", "IDLE");
                    int hunger = data.optInt("hunger", 100);

                    // 텍스트뷰 ID 찾아서 내용 변경
                    int textId = context.getResources().getIdentifier("widget_text", "id", context.getPackageName());
                    views.setTextViewText(textId, "상태: " + state + "\n배고픔: " + hunger);
                    
                } catch (Exception e) {
                    Log.e("TinyCapsule", "Error: " + e.getMessage());
                }
            }
            appWidgetManager.updateAppWidget(widgetId, views);
        }
    }
}