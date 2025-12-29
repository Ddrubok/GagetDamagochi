#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

extern void UnitySendMessage(const char *, const char *, const char *);

@interface VoiceBridge : NSObject <SFSpeechRecognizerDelegate>
@property (nonatomic, strong) SFSpeechRecognizer *speechRecognizer;
@property (nonatomic, strong) SFSpeechAudioBufferRecognitionRequest *recognitionRequest;
@property (nonatomic, strong) SFSpeechRecognitionTask *recognitionTask;
@property (nonatomic, strong) AVAudioEngine *audioEngine;
@end

@implementation VoiceBridge

static VoiceBridge *instance = nil;

+ (VoiceBridge *)sharedInstance {
    if (!instance) {
        instance = [[VoiceBridge alloc] init];
    }
    return instance;
}

- (id)init {
    self = [super init];
    if (self) {
                NSLocale *locale = [[NSLocale alloc] initWithLocaleIdentifier:@"ko-KR"];
        self.speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:locale];
        self.speechRecognizer.delegate = self;
        self.audioEngine = [[AVAudioEngine alloc] init];
    }
    return self;
}

- (void)startListening {
        [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
        if (status != SFSpeechRecognizerAuthorizationStatusAuthorized) {
            UnitySendMessage("UI_Main", "IOS_OnError", "권한이 없습니다.");
            return;
        }
        
        [[NSOperationQueue mainQueue] addOperationWithBlock:^{
            [self startRecording];
        }];
    }];
}

- (void)stopListening {
    if ([self.audioEngine isRunning]) {
        [self.audioEngine stop];
        [self.recognitionRequest endAudio];
    }
}

- (void)startRecording {
        if (self.recognitionTask) {
        [self.recognitionTask cancel];
        self.recognitionTask = nil;
    }

    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    [audioSession setCategory:AVAudioSessionCategoryRecord mode:AVAudioSessionModeMeasurement options:AVAudioSessionCategoryOptionDuckOthers error:nil];
    [audioSession setActive:YES withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation error:nil];

    self.recognitionRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    AVAudioInputNode *inputNode = self.audioEngine.inputNode;

    self.recognitionRequest.shouldReportPartialResults = YES;

        self.recognitionTask = [self.speechRecognizer recognitionTaskWithRequest:self.recognitionRequest resultHandler:^(SFSpeechRecognitionResult * _Nullable result, NSError * _Nullable error) {
        
        if (result) {
            NSString *transcript = result.bestTranscription.formattedString;
                        UnitySendMessage("UI_Main", "IOS_OnResult", [transcript UTF8String]);
        }

        if (error || result.isFinal) {
            [self.audioEngine stop];
            [inputNode removeTapOnBus:0];
            self.recognitionRequest = nil;
            self.recognitionTask = nil;
        }
    }];

    AVAudioFormat *recordingFormat = [inputNode outputFormatForBus:0];
    [inputNode installTapOnBus:0 bufferSize:1024 format:recordingFormat block:^(AVAudioPCMBuffer * _Nonnull buffer, AVAudioTime * _Nonnull when) {
        [self.recognitionRequest appendAudioPCMBuffer:buffer];
    }];

    [self.audioEngine prepare];
    [self.audioEngine startAndReturnError:nil];
}

@end

extern "C" {
    void _StartListening() {
        [[VoiceBridge sharedInstance] startListening];
    }

    void _StopListening() {
        [[VoiceBridge sharedInstance] stopListening];
    }
}