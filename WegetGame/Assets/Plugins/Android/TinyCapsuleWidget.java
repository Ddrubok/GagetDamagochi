package com.ddrubok.wegetgame;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
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
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {
            try {
                JSONObject data = new JSONObject(json);
                views.setTextViewText(textId, "상태: " + data.optString("state"));
            } catch (Exception e) { views.setTextViewText(textId, "Err"); }
        }
        try {
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        } catch (Exception e) { }
    }
}