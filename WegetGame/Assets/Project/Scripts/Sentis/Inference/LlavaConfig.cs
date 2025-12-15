using System;

namespace Unity.InferenceEngine.Samples.Chat
{
    class LlavaConfig
    {
        public const string ModelId = "llava-hf/llava-onevision-qwen2-0.5b-si-hf";
        public const string DownloadPath = "/ChatLLM/Resources/Models";

        public const string DecoderModelName = "decoder_model_merged";
        public const string DecoderModelFile =  DecoderModelName + ".onnx";
        public const string DecoderModelPath = "Models/onnx/" + DecoderModelName;

        public const string VisionEncoderModelName = "vision_encoder";
        public const string VisionEncoderModelFile = VisionEncoderModelName + ".onnx";
        public const string VisionEncoderModelPath = "Models/onnx/" + VisionEncoderModelName;

        public const string EmbeddingModelName = "embed_tokens";
        public const string EmbeddingModelFile = EmbeddingModelName + ".onnx";
        public const string EmbeddingModelPath = "Models/onnx/" + EmbeddingModelName;

        public const string TokenizerModelName = "tokenizer";
        public const string TokenizerModelFile = TokenizerModelName + ".json";
        public const string TokenizerConfigPath = "Models/" + TokenizerModelName;

        public const int TokenIdEndOfText = 151645;
        public const int TokenIdImage = 151646;

        public BackendType BackendType { get; private set; } = BackendType.GPUCompute;
        public LlavaTokenizer Tokenizer { get; private set; }

        public LlavaConfig(BackendType backendType)
        {
            BackendType = backendType;
            Tokenizer = new LlavaTokenizer();
        }

        public static string ApplyChatTemplate(string userPrompt)
        {
            // [System Prompt] 영어로 작성하여 AI가 더 정확하게 이해하도록 합니다.
            string systemPrompt =
                "You are a cute 'Onion Tamagotchi' living in the user's smartphone. " + // 넌 스마트폰에 사는 귀여운 양파야.
                "You must ALWAYS reply in Korean. " + // 무조건 한국어로 대답해.
                "Keep your answers short, cute, and friendly. " + // 대답은 짧고, 귀엽고, 친근하게 해.
                "End every sentence with 'yang!' (양!). " + // 문장 끝마다 '양!'을 붙여.
                "Analyze the user's input sentiment: " + // 사용자의 기분을 분석해:
                "If the user is nice or praises you, start your reply with '{HAPPY}'. " + // 칭찬하면 {HAPPY}로 시작.
                "If the user is mean or scolds you, start your reply with '{SAD}'. " + // 혼내면 {SAD}로 시작.
                "Otherwise, do not use any tag."; // 그 외엔 태그 없음.

            // <image> 태그는 AI가 이미지를 인식하는 위치이므로 절대 지우면 안 됩니다!
            return $"<|im_start|>system\n{systemPrompt}<|im_end|>\n<|im_start|>user <image>\n{userPrompt}<|im_end|>\n<|im_start|>assistant\n";
        }
    }
}
