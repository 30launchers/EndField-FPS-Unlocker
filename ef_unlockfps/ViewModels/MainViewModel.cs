using ef_unlockfps.Models;

namespace ef_unlockfps.ViewModels
{
    public class MainViewModel : ConfigViewModel<EFConfig>
    {
        public int Fps
        {
            get => GetConfigValue<int>();
            set => SetConfigValue(value);
        }

        public int SliderValue
        {
            get => GetConfigValue<int>();
            set => SetConfigValue(value);
        }

        public bool AutoStart
        {
            get => GetConfigValue<bool>();
            set => SetConfigValue(value);
        }

        public bool PowerSaving
        {
            get => GetConfigValue<bool>();
            set => SetConfigValue(value);
        }

        public bool UseDx11
        {
            get => GetConfigValue<bool>();
            set => SetConfigValue(value);
        }

        public bool UnlimitedFps
        {
            get => GetConfigValue<bool>();
            set => SetConfigValue(value);
        }

        public string GamePath
        {
            get => GetConfigValue<string>();
            set => SetConfigValue(value);
        }

        public string CustomLaunchParamCfg
        {
            get => GetConfigValue<string>();
            set => SetConfigValue(value);
        }

        public int CustomResolutionCfg
        {
            get => GetConfigValue<int>();
            set => SetConfigValue(value);
        }

        public string ProcessPriority
        {
            get => GetConfigValue<string>();
            set
            {
                SetConfigValue(value);
                UpdatePriorityCheckboxes();
            }
        }

        public bool AutoCloseCfg
        {
            get => GetConfigValue<bool>();
            set => SetConfigValue(value);
        }

        public bool IsRealtime
        {
            get => ProcessPriority == "Realtime";
            set
            {
                if (value) ProcessPriority = "Realtime";
            }
        }

        public bool IsHigh
        {
            get => ProcessPriority == "High";
            set
            {
                if (value) ProcessPriority = "High";
            }
        }

        public bool IsAboveNormal
        {
            get => ProcessPriority == "Above Normal";
            set
            {
                if (value) ProcessPriority = "Above Normal";
            }
        }

        public bool IsNormal
        {
            get => ProcessPriority == "Normal";
            set
            {
                if (value) ProcessPriority = "Normal";
            }
        }

        public bool IsBelowNormal
        {
            get => ProcessPriority == "Below Normal";
            set
            {
                if (value) ProcessPriority = "Below Normal";
            }
        }

        public bool IsLow
        {
            get => ProcessPriority == "Low";
            set
            {
                if (value) ProcessPriority = "Low";
            }
        }

        public bool IsDefault
        {
            get => ProcessPriority == "Default";
            set
            {
                if (value) ProcessPriority = "Default";
            }
        }

        private void UpdatePriorityCheckboxes()
        {
            OnPropertyChanged(nameof(IsRealtime));
            OnPropertyChanged(nameof(IsHigh));
            OnPropertyChanged(nameof(IsAboveNormal));
            OnPropertyChanged(nameof(IsNormal));
            OnPropertyChanged(nameof(IsBelowNormal));
            OnPropertyChanged(nameof(IsLow));
            OnPropertyChanged(nameof(IsDefault));
        }
    }
}