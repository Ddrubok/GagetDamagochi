using UnityEngine;
using UnityEditor;
using System.IO;

public class WidgetInstaller : EditorWindow
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string WIDGET_LIB_NAME = "MyWidget.androidlib";

    [MenuItem("Tools/🧅 양파 위젯 업데이트 (Java 코드 변경)")]
    public static void InstallWidgetFiles()
    {
        string androidPath = Path.Combine(Application.dataPath, "Plugins/Android");
        string libPath = Path.Combine(androidPath, WIDGET_LIB_NAME);
        string srcPath = Path.Combine(libPath, "src");
        string javaPackagePath = Path.Combine(srcPath, "com/ddrubok/wegetgame");
        string resPath = Path.Combine(libPath, "res");

        // 1. 기존 폴더 정리 (깔끔하게 재생성)
        if (Directory.Exists(libPath)) Directory.Delete(libPath, true);

        // 폴더 구조 생성
        if (!Directory.Exists(androidPath)) Directory.CreateDirectory(androidPath);
        Directory.CreateDirectory(libPath);
        Directory.CreateDirectory(srcPath);
        Directory.CreateDirectory(javaPackagePath);
        Directory.CreateDirectory(resPath);
        Directory.CreateDirectory(Path.Combine(resPath, "layout"));
        Directory.CreateDirectory(Path.Combine(resPath, "xml"));

        // =========================================================
        // 2. 자바 코드 (여기를 양파 게임 로직으로 바꿨습니다!)
        // =========================================================
        string javaCode = $@"package {PACKAGE_NAME};

import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import android.graphics.Color; // 색상 변경을 위해 추가
import org.json.JSONObject;

public class TinyCapsuleWidget extends AppWidgetProvider {{
    @Override
    public void onReceive(Context context, Intent intent) {{
        super.onReceive(context, intent);
        // 브로드캐스트 수신
        if (""{PACKAGE_NAME}.ACTION_WIDGET_UPDATE"".equals(intent.getAction())) {{
            updateWidget(context, AppWidgetManager.getInstance(context), intent.getStringExtra(""EXTRA_DATA_JSON""));
        }}
    }}

    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {{
        updateWidget(context, appWidgetManager, null);
    }}

    private void updateWidget(Context context, AppWidgetManager appWidgetManager, String json) {{
        int layoutId = context.getResources().getIdentifier(""widget_layout"", ""layout"", context.getPackageName());
        int textId = context.getResources().getIdentifier(""widget_text"", ""id"", context.getPackageName());
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {{
            try {{
                JSONObject data = new JSONObject(json);
                String state = data.optString(""state"");     // HAPPY, SAD, NORMAL
                String message = data.optString(""message""); // 텍스트 내용
                
                // [로직] 상태에 따라 이모지와 글자 색상 변경
                String displayCheck = """";
                int textColor = Color.BLACK;

                if (""HAPPY"".equals(state)) {{
                    displayCheck = ""🧅✨ "" + message;       // 반짝이는 양파
                    textColor = Color.parseColor(""#2E7D32""); // 진한 초록색
                }} else if (""SAD"".equals(state)) {{
                    displayCheck = ""🧅💦 "" + message;       // 우는 양파
                    textColor = Color.parseColor(""#C62828""); // 진한 빨간색
                }} else {{
                    displayCheck = ""🧅 "" + message;          // 평범 양파
                    textColor = Color.BLACK;
                }}

                views.setTextViewText(textId, displayCheck);
                views.setTextColor(textId, textColor);

            }} catch (Exception e) {{ 
                views.setTextViewText(textId, ""Error""); 
            }}
        }} else {{
            // 초기 상태
            views.setTextViewText(textId, ""🧅 양파를 심었습니다."");
            views.setTextColor(textId, Color.BLACK);
        }}
        
        try {{
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        }} catch (Exception e) {{ }}
    }}
}}";
        WriteFile(Path.Combine(javaPackagePath, "TinyCapsuleWidget.java"), javaCode);

        // [File 2] Library Manifest (merge 모드 유지)
        string libManifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    xmlns:tools=""http://schemas.android.com/tools"" 
    package=""{PACKAGE_NAME}.widget""> 
    <application>
        <receiver android:name=""{PACKAGE_NAME}.TinyCapsuleWidget"" 
                  android:exported=""true""
                  tools:node=""merge"">
            <intent-filter>
                <action android:name=""android.appwidget.action.APPWIDGET_UPDATE"" />
                <action android:name=""{PACKAGE_NAME}.ACTION_WIDGET_UPDATE"" />
            </intent-filter>
            <meta-data android:name=""android.appwidget.provider"" android:resource=""@xml/widget_info"" />
        </receiver>
    </application>
</manifest>";
        WriteFile(Path.Combine(libPath, "AndroidManifest.xml"), libManifest);

        // [File 3, 4] Gradle & Properties
        WriteFile(Path.Combine(libPath, "project.properties"), "target=android-31\nandroid.library=true");
        WriteFile(Path.Combine(libPath, "build.gradle"),
            @"apply plugin: 'com.android.library'
android { 
    namespace 'com.ddrubok.wegetgame.widget' 
    compileSdkVersion 34 
    defaultConfig { minSdkVersion 24 }
    sourceSets {
        main {
            manifest.srcFile 'AndroidManifest.xml'
            java.srcDirs = ['src']
            res.srcDirs = ['res']
        }
    }
}");

        // [File 5, 6] Resources
        WriteFile(Path.Combine(resPath, "xml/widget_info.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<appwidget-provider xmlns:android=""http://schemas.android.com/apk/res/android""
    android:minWidth=""110dp"" android:minHeight=""110dp""
    android:updatePeriodMillis=""0"" android:initialLayout=""@layout/widget_layout""
    android:resizeMode=""horizontal|vertical"" android:widgetCategory=""home_screen"">
</appwidget-provider>");

        WriteFile(Path.Combine(resPath, "layout/widget_layout.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<RelativeLayout xmlns:android=""http://schemas.android.com/apk/res/android""
    android:layout_width=""match_parent"" android:layout_height=""match_parent""
    android:background=""#ffffff"" android:padding=""8dp"">
    <TextView android:id=""@+id/widget_text"" android:layout_width=""wrap_content""
        android:layout_height=""wrap_content"" android:layout_centerInParent=""true""
        android:text=""양파 대기중..."" android:textColor=""#000000"" android:textSize=""20sp""
        android:textStyle=""bold"" />
</RelativeLayout>");

        AssetDatabase.Refresh();
        Debug.Log("✅ 양파 게임 로직 적용 완료! (Java 코드 업데이트됨)");
    }

    private static void WriteFile(string path, string content)
    {
        try { File.WriteAllText(path, content); } catch { Debug.LogError($"파일 쓰기 실패: {path}"); }
    }
}