package com.ddrubok.wegetgame;

import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import android.graphics.Color;
import org.json.JSONObject;

public class TinyCapsuleWidget extends AppWidgetProvider {
    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);
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
        int imageId = context.getResources().getIdentifier("widget_image", "id", context.getPackageName()); // 이미지 ID 찾기
        
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {
            try {
                JSONObject data = new JSONObject(json);
                String state = data.optString("state");
                String message = data.optString("message");
                
                // 1. 텍스트 설정
                views.setTextViewText(textId, message);

                // 2. 이미지 변경 로직
                String imageName = "onion_normal"; // 기본값
                int textColor = Color.BLACK;

                if ("HAPPY".equals(state)) {
                    imageName = "onion_happy";
                    textColor = Color.parseColor("#2E7D32");
                } else if ("SAD".equals(state)) {
                    imageName = "onion_sad";
                    textColor = Color.parseColor("#C62828");
                }

                // 이미지 리소스 ID 찾아서 적용
                int drawableId = context.getResources().getIdentifier(imageName, "drawable", context.getPackageName());
                if (drawableId != 0) {
                    views.setImageViewResource(imageId, drawableId);
                }
                views.setTextColor(textId, textColor);

            } catch (Exception e) { 
                views.setTextViewText(textId, "Error: " + e.getMessage()); 
            }
        } else {
            // 초기 상태
            views.setTextViewText(textId, "🧅 양파를 심었습니다.");
            int defaultImgId = context.getResources().getIdentifier("onion_normal", "drawable", context.getPackageName());
            if (defaultImgId != 0) views.setImageViewResource(imageId, defaultImgId);
        }
        
        try {
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        } catch (Exception e) { }
    }
}