using UnityEngine;
using UnityEditor;
using System.IO;

public class IOSSharedDataInstaller : EditorWindow
{
    private static string PluginPath = Path.Combine(Application.dataPath, "Plugins/iOS");
    private const string FileName = "SharedDataBridge.mm";

    [MenuItem("Tools/ IOS 위젯 데이터 플러그인 설치")]
    public static void InstallIOSDataPlugin()
    {
        if (!Directory.Exists(PluginPath)) Directory.CreateDirectory(PluginPath);

        string fileContent = GetSourceCode();
        string fullPath = Path.Combine(PluginPath, FileName);

        try
        {
            File.WriteAllText(fullPath, fileContent);
            AssetDatabase.Refresh();
            Debug.Log($"IOS 데이터 플러그인 설치 완료! 경로: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"설치 실패: {e.Message}");
        }
    }

    private static string GetSourceCode()
    {
        return @"#import <Foundation/Foundation.h>
#import <WidgetKit/WidgetKit.h> 
static NSString *const APP_GROUP_NAME = @""group.com.ddrubok.wegetgame""; 

extern ""C"" {

        void _SaveToSharedGroup(const char* key, const char* value) {
        NSString *nsKey = [NSString stringWithUTF8String:key];
        NSString *nsValue = [NSString stringWithUTF8String:value];
        
        NSUserDefaults *sharedDefaults = [[NSUserDefaults alloc] initWithSuiteName:APP_GROUP_NAME];
        [sharedDefaults setObject:nsValue forKey:nsKey];
        [sharedDefaults synchronize];
    }

        void _ReloadWidget() {
        if (@available(iOS 14.0, *)) {
            [[WidgetCenter sharedCenter] reloadAllTimelines];
        }
    }
}";
    }
}