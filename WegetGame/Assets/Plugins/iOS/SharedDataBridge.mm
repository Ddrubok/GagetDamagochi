#import <Foundation/Foundation.h>
#import <WidgetKit/WidgetKit.h> 
static NSString *const APP_GROUP_NAME = @"group.com.ddrubok.wegetgame"; 

extern "C" {

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
}