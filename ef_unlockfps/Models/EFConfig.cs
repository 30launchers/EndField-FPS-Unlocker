using System.Text.Json.Serialization;

namespace ef_unlockfps.Models
{
    public class EFConfig
    {
        [JsonPropertyName("fps")]
        public int Fps { get; set; } = 300;

        [JsonPropertyName("sliderValue")]
        public int SliderValue { get; set; } = 300;

        [JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = false;

        [JsonPropertyName("powerSaving")]
        public bool PowerSaving { get; set; } = false;

        [JsonPropertyName("useDx11")]
        public bool UseDx11 { get; set; } = false;

        [JsonPropertyName("processPriority")]
        public string ProcessPriority { get; set; } = "Normal";

        [JsonPropertyName("unlimitedFPS")]
        public bool UnlimitedFps { get; set; } = false;

        [JsonPropertyName("gamePath")]
        public string GamePath { get; set; } = "";

        [JsonPropertyName("customLaunchParam")]
        public string CustomLaunchParamCfg { get; set; } = "";

        [JsonPropertyName("customResolution")]
        public int CustomResolutionCfg { get; set; } = 0;

        [JsonPropertyName("autoClose")]
        public bool AutoCloseCfg { get; set; } = false;
    }
}
