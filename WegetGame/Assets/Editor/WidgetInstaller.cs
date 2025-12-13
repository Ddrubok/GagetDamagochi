using UnityEngine;
using UnityEditor;
using System.IO;

public class WidgetInstaller : EditorWindow
{
    private const string PACKAGE_NAME = "com.ddrubok.wegetgame";
    private const string WIDGET_LIB_NAME = "MyWidget.androidlib";

    [MenuItem("Tools/🛠️ 위젯 플러그인 재설치 (표준 라이브러리 구조)")]
    public static void InstallWidgetFiles()
    {
        string androidPath = Path.Combine(Application.dataPath, "Plugins/Android");
        string libPath = Path.Combine(androidPath, WIDGET_LIB_NAME);

        // [핵심 변경] Java 소스 코드가 들어갈 표준 경로 (src 폴더 사용)
        // 구조: MyWidget.androidlib/src/com/ddrubok/wegetgame/TinyCapsuleWidget.java
        string srcPath = Path.Combine(libPath, "src");
        string javaPackagePath = Path.Combine(srcPath, "com/ddrubok/wegetgame");
        string resPath = Path.Combine(libPath, "res");

        // 1. 기존 폴더/파일 정리 (찌꺼기 제거)
        if (Directory.Exists(libPath)) Directory.Delete(libPath, true);

        // [중요] 이전에 밖에 만들어둔 잘못된 Java 폴더 삭제 (중복 방지)
        string oldJavaPath = Path.Combine(androidPath, "com");
        if (Directory.Exists(oldJavaPath)) Directory.Delete(oldJavaPath, true);

        // 메인 매니페스트 삭제 (유니티 자동 생성 사용 -> 충돌 방지)
        string mainManifestPath = Path.Combine(androidPath, "AndroidManifest.xml");
        if (File.Exists(mainManifestPath)) File.Delete(mainManifestPath);
        if (File.Exists(mainManifestPath + ".meta")) File.Delete(mainManifestPath + ".meta");

        // 2. 폴더 구조 생성
        if (!Directory.Exists(androidPath)) Directory.CreateDirectory(androidPath);
        Directory.CreateDirectory(libPath);
        Directory.CreateDirectory(srcPath);          // src 폴더 생성
        Directory.CreateDirectory(javaPackagePath);  // 패키지 폴더 생성
        Directory.CreateDirectory(resPath);
        Directory.CreateDirectory(Path.Combine(resPath, "layout"));
        Directory.CreateDirectory(Path.Combine(resPath, "xml"));

        // =========================================================
        // 3. 파일 생성
        // =========================================================

        // [File 1] Java Code (이제 라이브러리 안쪽 src 폴더에 생성됨!)
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
        }} else {{
            views.setTextViewText(textId, ""WegetGame Ready"");
        }}
        try {{
            appWidgetManager.updateAppWidget(new android.content.ComponentName(context, TinyCapsuleWidget.class), views);
        }} catch (Exception e) {{ }}
    }}
}}";
        WriteFile(Path.Combine(javaPackagePath, "TinyCapsuleWidget.java"), javaCode);

        // [File 2] Library Manifest (tools:node="merge" 유지)
        // 위젯 리시버를 확실하게 등록합니다.
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
        // android.library=true가 있어야 src 폴더를 컴파일합니다.
        WriteFile(Path.Combine(libPath, "project.properties"), "target=android-31\nandroid.library=true");
        WriteFile(Path.Combine(libPath, "build.gradle"),
            @"apply plugin: 'com.android.library'
android { 
    namespace 'com.ddrubok.wegetgame.widget' 
    compileSdkVersion 34 
    defaultConfig { minSdkVersion 24 }
    
    // 소스 경로 명시 (혹시 모를 인식 오류 방지)
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
        android:text=""WegetGame 위젯"" android:textColor=""#000000"" android:textSize=""18sp""
        android:textStyle=""bold"" />
</RelativeLayout>");

        AssetDatabase.Refresh();
        Debug.Log("✅ 표준 구조 재설치 완료! (MyWidget.androidlib/src 안에 Java 포함됨)");
    }

    private static void WriteFile(string path, string content)
    {
        try { File.WriteAllText(path, content); } catch { Debug.LogError($"파일 쓰기 실패: {path}"); }
    }
}