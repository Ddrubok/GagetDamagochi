using UnityEngine;
using UnityEditor;
using System.IO;

public class WidgetInstaller : EditorWindow
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string WIDGET_LIB_NAME = "MyWidget.androidlib";

    [MenuItem("Tools/🧅 양파 위젯 최종 업데이트 (이미지+음성)")]
    public static void InstallWidgetFiles()
    {
        string androidPath = Path.Combine(Application.dataPath, "Plugins/Android");
        string libPath = Path.Combine(androidPath, WIDGET_LIB_NAME);
        string srcPath = Path.Combine(libPath, "src");
        string javaPackagePath = Path.Combine(srcPath, "com/ddrubok/wegetgame");
        string resPath = Path.Combine(libPath, "res");
        string drawablePath = Path.Combine(resPath, "drawable"); // 이미지 폴더

        // 1. 기존 폴더 정리
        if (Directory.Exists(libPath)) Directory.Delete(libPath, true);

        // 폴더 구조 생성
        Directory.CreateDirectory(androidPath);
        Directory.CreateDirectory(libPath);
        Directory.CreateDirectory(srcPath);
        Directory.CreateDirectory(javaPackagePath);
        Directory.CreateDirectory(resPath);
        Directory.CreateDirectory(Path.Combine(resPath, "layout"));
        Directory.CreateDirectory(Path.Combine(resPath, "xml"));
        Directory.CreateDirectory(drawablePath); // drawable 폴더 생성

        // ✅ [추가됨] 이미지 파일 복사하기
        CopyImagesToWidget(drawablePath);

        // =========================================================
        // 2. 자바 코드 (이미지 변경 로직 추가됨)
        // =========================================================
        string javaCode = $@"package {PACKAGE_NAME};

import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
import android.graphics.Color;
import org.json.JSONObject;

public class TinyCapsuleWidget extends AppWidgetProvider {{
    @Override
    public void onReceive(Context context, Intent intent) {{
        super.onReceive(context, intent);
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
        int imageId = context.getResources().getIdentifier(""widget_image"", ""id"", context.getPackageName()); // 이미지 ID 찾기
        
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {{
            try {{
                JSONObject data = new JSONObject(json);
                String state = data.optString(""state"");
                String message = data.optString(""message"");
                
                // 1. 텍스트 설정
                views.setTextViewText(textId, message);

                // 2. 이미지 변경 로직
                String imageName = ""onion_normal""; // 기본값
                int textColor = Color.BLACK;

                if (""HAPPY"".equals(state)) {{
                    imageName = ""onion_happy"";
                    textColor = Color.parseColor(""#2E7D32"");
                }} else if (""SAD"".equals(state)) {{
                    imageName = ""onion_sad"";
                    textColor = Color.parseColor(""#C62828"");
                }}

                // 이미지 리소스 ID 찾아서 적용
                int drawableId = context.getResources().getIdentifier(imageName, ""drawable"", context.getPackageName());
                if (drawableId != 0) {{
                    views.setImageViewResource(imageId, drawableId);
                }}
                views.setTextColor(textId, textColor);

            }} catch (Exception e) {{ 
                views.setTextViewText(textId, ""Error: "" + e.getMessage()); 
            }}
        }} else {{
            // 초기 상태
            views.setTextViewText(textId, ""🧅 양파를 심었습니다."");
            int defaultImgId = context.getResources().getIdentifier(""onion_normal"", ""drawable"", context.getPackageName());
            if (defaultImgId != 0) views.setImageViewResource(imageId, defaultImgId);
        }}
        
        try {{
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        }} catch (Exception e) {{ }}
    }}
}}";
        WriteFile(Path.Combine(javaPackagePath, "TinyCapsuleWidget.java"), javaCode);

        // [File 2] Library Manifest (권한 및 쿼리 유지)
        string libManifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    xmlns:tools=""http://schemas.android.com/tools"" 
    package=""{PACKAGE_NAME}.widget""> 

    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.RECORD_AUDIO"" />

    <queries>
        <intent>
            <action android:name=""android.speech.RecognitionService"" />
        </intent>
    </queries>

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

        // [File 3, 4] Gradle
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

        // [File 5] Widget Info
        WriteFile(Path.Combine(resPath, "xml/widget_info.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<appwidget-provider xmlns:android=""http://schemas.android.com/apk/res/android""
    android:minWidth=""150dp"" android:minHeight=""150dp""
    android:updatePeriodMillis=""0"" android:initialLayout=""@layout/widget_layout""
    android:resizeMode=""horizontal|vertical"" android:widgetCategory=""home_screen"">
</appwidget-provider>");

        // [File 6] Layout (이미지뷰 추가됨!)
        WriteFile(Path.Combine(resPath, "layout/widget_layout.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<LinearLayout xmlns:android=""http://schemas.android.com/apk/res/android""
    android:layout_width=""match_parent"" android:layout_height=""match_parent""
    android:orientation=""vertical""
    android:background=""#ffffff"" 
    android:padding=""8dp""
    android:gravity=""center"">

    <ImageView
        android:id=""@+id/widget_image""
        android:layout_width=""80dp""
        android:layout_height=""80dp""
        android:layout_marginBottom=""8dp""
        android:scaleType=""fitCenter"" />

    <TextView android:id=""@+id/widget_text"" 
        android:layout_width=""wrap_content""
        android:layout_height=""wrap_content"" 
        android:text=""양파 대기중..."" 
        android:textColor=""#000000"" 
        android:textSize=""16sp""
        android:textStyle=""bold"" 
        android:gravity=""center"" />

</LinearLayout>");

        AssetDatabase.Refresh();
        Debug.Log("✅ 양파 위젯 최종 업데이트 완료! (이미지 복사됨)");
    }

    // ✅ 이미지 복사 함수
    private static void CopyImagesToWidget(string destPath)
    {
        string sourceFolder = Path.Combine(Application.dataPath, "WidgetImages");
        string[] imageNames = { "onion_normal.png", "onion_happy.png", "onion_sad.png" };

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogError($"🚨 'Assets/WidgetImages' 폴더가 없습니다! 이미지를 넣어주세요.");
            return;
        }

        foreach (string imgName in imageNames)
        {
            string srcFile = Path.Combine(sourceFolder, imgName);
            string destFile = Path.Combine(destPath, imgName);

            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, destFile, true);
                Debug.Log($"🖼️ 이미지 복사 성공: {imgName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 이미지 파일이 없습니다: {imgName} (기본 표정이 안 나올 수 있어요)");
            }
        }
    }

    private static void WriteFile(string path, string content)
    {
        try { File.WriteAllText(path, content); } catch { Debug.LogError($"파일 쓰기 실패: {path}"); }
    }
}