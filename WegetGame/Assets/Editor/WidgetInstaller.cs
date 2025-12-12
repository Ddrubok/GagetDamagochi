using UnityEngine;
using UnityEditor;
using System.IO;

public class WidgetInstaller : EditorWindow
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string WIDGET_LIB_NAME = "MyWidget.androidlib";

    [MenuItem("Tools/🛠️ 위젯 플러그인 재설치 (Final Fix)")]
    public static void InstallWidgetFiles()
    {
        string androidPath = Path.Combine(Application.dataPath, "Plugins/Android");
        string libPath = Path.Combine(androidPath, WIDGET_LIB_NAME);
        string resPath = Path.Combine(libPath, "res");

        // 1. 기존 폴더 정리
        if (Directory.Exists(libPath))
        {
            Directory.Delete(libPath, true);
            if (File.Exists(libPath + ".meta")) File.Delete(libPath + ".meta");
        }

        if (!Directory.Exists(androidPath)) Directory.CreateDirectory(androidPath);
        Directory.CreateDirectory(libPath);
        Directory.CreateDirectory(resPath);
        Directory.CreateDirectory(Path.Combine(resPath, "layout"));
        Directory.CreateDirectory(Path.Combine(resPath, "xml"));

        // =========================================================
        // 2. 파일 생성
        // =========================================================

        // [File 1] Main AndroidManifest.xml (아이콘 표시 문제 해결!)
        string mainManifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
    package=""{PACKAGE_NAME}"">
    <application>
        <activity android:name=""com.unity3d.player.UnityPlayerActivity""
                  android:theme=""@style/UnityThemeSelector""
                  android:exported=""true"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
            </intent-filter>
        </activity>
    </application>
</manifest>";
        WriteFile(Path.Combine(androidPath, "AndroidManifest.xml"), mainManifest);

        // [File 2] Java Code
        string javaCode = $@"package {PACKAGE_NAME};
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;
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
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);
        
        if (json != null) {{
            try {{
                JSONObject data = new JSONObject(json);
                views.setTextViewText(textId, ""상태: "" + data.optString(""state""));
            }} catch (Exception e) {{ views.setTextViewText(textId, ""Err""); }}
        }}
        try {{
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        }} catch (Exception e) {{ }}
    }}
}}";
        WriteFile(Path.Combine(androidPath, "TinyCapsuleWidget.java"), javaCode);

        // [File 3] Library Manifest (위젯 선언부)
        string libManifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" 
    package=""{PACKAGE_NAME}.widget""> 
    <application>
        <receiver android:name=""{PACKAGE_NAME}.TinyCapsuleWidget"" android:exported=""true"">
            <intent-filter>
                <action android:name=""android.appwidget.action.APPWIDGET_UPDATE"" />
                <action android:name=""{PACKAGE_NAME}.ACTION_WIDGET_UPDATE"" />
            </intent-filter>
            <meta-data android:name=""android.appwidget.provider"" android:resource=""@xml/widget_info"" />
        </receiver>
    </application>
</manifest>";
        WriteFile(Path.Combine(libPath, "AndroidManifest.xml"), libManifest);

        // [File 4] project.properties
        WriteFile(Path.Combine(libPath, "project.properties"), "target=android-31\nandroid.library=true");

        // [File 5] build.gradle
        string buildGradle = @"apply plugin: 'com.android.library'
android {
    namespace 'com.ddrubok.wegetgame.widget'
    compileSdkVersion 34
    defaultConfig { minSdkVersion 24 }
}";
        WriteFile(Path.Combine(libPath, "build.gradle"), buildGradle);

        // [File 6] widget_info.xml
        WriteFile(Path.Combine(resPath, "xml/widget_info.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<appwidget-provider xmlns:android=""http://schemas.android.com/apk/res/android""
    android:minWidth=""110dp"" android:minHeight=""110dp""
    android:updatePeriodMillis=""0"" android:initialLayout=""@layout/widget_layout""
    android:resizeMode=""horizontal|vertical"" android:widgetCategory=""home_screen"">
</appwidget-provider>");

        // [File 7] widget_layout.xml
        WriteFile(Path.Combine(resPath, "layout/widget_layout.xml"),
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<RelativeLayout xmlns:android=""http://schemas.android.com/apk/res/android""
    android:layout_width=""match_parent"" android:layout_height=""match_parent""
    android:background=""#ffffff"" android:padding=""8dp"">
    <TextView android:id=""@+id/widget_text"" android:layout_width=""wrap_content""
        android:layout_height=""wrap_content"" android:layout_centerInParent=""true""
        android:text=""Hello Tiny Capsule!"" android:textColor=""#000000"" android:textSize=""20sp"" />
</RelativeLayout>");

        // 3. 마무리
        AssetDatabase.Refresh();
        Debug.Log("✅ 위젯 파일 업데이트 완료 (Icon fix applied). 유니티를 재시작하고 빌드하세요.");
    }

    private static void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
    }
}