package com.ddrubok.wegetgame.bridge;

import android.content.Context;
import com.google.mlkit.genai.prompt.PromptModel;
import com.google.mlkit.genai.prompt.PromptModelOptions;
import com.google.mlkit.genai.prompt.PromptModelDownloader;

public class GemmaLlmBridge {

    private PromptModel gemmaModel = null;

    public interface LlmListener {
        void onPartialResult(String partialText);
        void onComplete(String resultText);
        void onError(String errorMsg);
    }

    // 1. 모델 자동 다운로드 및 초기화 (수동 복사 불필요!)
    public void initModel(final Context context, final LlmListener listener) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    // 구글 플레이 서비스를 통해 Gemma 4 E2B 모델 다운로드 요청
                    PromptModelOptions options = new PromptModelOptions.Builder()
                        .setModelType(PromptModelOptions.MODEL_TYPE_GEMMA_4_E2B)
                        .build();
                        
                    PromptModelDownloader.downloadIfNeeded(options)
                        .addOnSuccessListener(aVoid -> {
                            // 다운로드가 완료되면 모델 객체 생성
                            gemmaModel = new PromptModel(options);
                            listener.onComplete("INIT_SUCCESS_DOWNLOADED");
                        })
                        .addOnFailureListener(e -> {
                            listener.onError("Download Failed: " + e.getMessage());
                        });
                } catch (Exception e) {
                    listener.onError("Init Error: " + e.getMessage());
                }
            }
        }).start();
    }

    // 2. 대답 생성 (스트리밍)
    public void generateAsync(final String prompt, final LlmListener listener) {
        if (gemmaModel == null) {
            listener.onError("Model is not ready yet.");
            return;
        }

        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    // ML Kit는 스트리밍 콜백을 네이티브하게 지원합니다.
                    gemmaModel.generateResponseStream(prompt, partial -> {
                        listener.onPartialResult(partial);
                    }).addOnSuccessListener(finalResult -> {
                        listener.onComplete(finalResult);
                    }).addOnFailureListener(e -> {
                        listener.onError("Generate Error: " + e.getMessage());
                    });
                } catch (Exception e) {
                    listener.onError("Thread Error: " + e.getMessage());
                }
            }
        }).start();
    }
}