//using System.ComponentModel;
//using System.Reflection;
//using System.Runtime.CompilerServices;

//namespace ef_unlockfps.ViewModels
//{
//    public class ConfigViewModel<T> : ObservableObject where T : class, new()
//    {
//        private readonly T _config;

//        public ConfigViewModel()
//        {
//            _config = LoadConfig();
//        }

//        protected T Config => _config;

//        protected virtual T LoadConfig()
//        {
//            var configType = typeof(T);
//            var configFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

//            if (!System.IO.File.Exists(configFilePath))
//            {
//                var defaultConfig = new T();
//                SaveConfig();
//                return defaultConfig;
//            }

//            try
//            {
//                var json = System.IO.File.ReadAllText(configFilePath);
//                var config = System.Text.Json.JsonSerializer.Deserialize<T>(json);
//                return config ?? new T();
//            }
//            catch
//            {
//                return new T();
//            }
//        }

//        protected void SaveConfig()
//        {
//            var configType = typeof(T);
//            var configFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

//            var options = new System.Text.Json.JsonSerializerOptions
//            {
//                WriteIndented = true
//            };
//            var json = System.Text.Json.JsonSerializer.Serialize(_config, options);
//            System.IO.File.WriteAllText(configFilePath, json);
//        }

//        protected TValue GetConfigValue<TValue>([CallerMemberName] string? propertyName = null)
//        {
//            if (propertyName == null)
//                return default!;

//            var property = typeof(T).GetProperty(propertyName);
//            return property != null ? (TValue)property.GetValue(_config)! : default!;
//        }

//        protected void SetConfigValue<TValue>(TValue value, [CallerMemberName] string? propertyName = null)
//        {
//            if (propertyName == null)
//                return;

//            var property = typeof(T).GetProperty(propertyName);
//            if (property != null && !Equals(property.GetValue(_config), value))
//            {
//                property.SetValue(_config, value);
//                OnPropertyChanged(propertyName);
//                SaveConfig();
//            }
//        }
//    }
//}



//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Text.Json;

//namespace ef_unlockfps.ViewModels
//{
//    public class ConfigViewModel<T> : ObservableObject where T : class, new()
//    {
//        private readonly T _config;

//        public ConfigViewModel()
//        {
//            _config = LoadConfig(out bool needsSave);
//            if (needsSave)
//                SaveConfig();
//        }

//        protected T Config => _config;

//        protected virtual T LoadConfig(out bool saveNeeded)
//        {
//            saveNeeded = false;
//            var configType = typeof(T);
//            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

//            if (!File.Exists(configFilePath))
//            {
//                // file missing -> use defaults and mark to save
//                saveNeeded = true;
//                return new T();
//            }

//            try
//            {
//                var json = File.ReadAllText(configFilePath);
//                var loaded = JsonSerializer.Deserialize<T>(json) ?? new T();

//                // detect which top-level properties actually exist in JSON
//                using var doc = JsonDocument.Parse(json);
//                var root = doc.RootElement;
//                var presentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                if (root.ValueKind == JsonValueKind.Object)
//                {
//                    foreach (var prop in root.EnumerateObject())
//                        presentProperties.Add(prop.Name);
//                }

//                // create result starting from defaults, then copy values for properties that were present in the file
//                var result = new T();
//                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
//                foreach (var prop in props)
//                {
//                    if (!prop.CanWrite)
//                        continue;

//                    // match by name (case-insensitive to tolerate camelCase/json naming)
//                    if (presentProperties.Contains(prop.Name))
//                    {
//                        var val = prop.GetValue(loaded);
//                        prop.SetValue(result, val);
//                    }
//                }

//                // if file had fewer properties than writable properties, we consider it "missing items"
//                var writableCount = props.Count(p => p.CanWrite);
//                if (presentProperties.Count < writableCount)
//                    saveNeeded = true;

//                return result;
//            }
//            catch
//            {
//                // on error, return defaults and schedule save to repair file
//                saveNeeded = true;
//                return new T();
//            }
//        }

//        protected void SaveConfig()
//        {
//            var configType = typeof(T);
//            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

//            var options = new JsonSerializerOptions
//            {
//                WriteIndented = true
//            };
//            var json = JsonSerializer.Serialize(_config, options);
//            File.WriteAllText(configFilePath, json);
//        }

//        protected TValue GetConfigValue<TValue>([CallerMemberName] string? propertyName = null)
//        {
//            if (propertyName == null)
//                return default!;

//            var property = typeof(T).GetProperty(propertyName);
//            return property != null ? (TValue)property.GetValue(_config)! : default!;
//        }

//        protected void SetConfigValue<TValue>(TValue value, [CallerMemberName] string? propertyName = null)
//        {
//            if (propertyName == null)
//                return;

//            var property = typeof(T).GetProperty(propertyName);
//            if (property != null && !Equals(property.GetValue(_config), value))
//            {
//                property.SetValue(_config, value);
//                OnPropertyChanged(propertyName);
//                SaveConfig();
//            }
//        }
//    }
//}



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ef_unlockfps.ViewModels
{
    public class ConfigViewModel<T> : ObservableObject where T : class, new()
    {
        private readonly T _config;

        public ConfigViewModel()
        {
            _config = LoadConfig(out bool needsSave);
            if (needsSave)
                SaveConfig();
        }

        protected T Config => _config;

        protected virtual T LoadConfig(out bool saveNeeded)
        {
            saveNeeded = false;
            var configType = typeof(T);
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

            if (!File.Exists(configFilePath))
            {
                // file missing -> use defaults and mark to save
                saveNeeded = true;
                return new T();
            }

            try
            {
                var json = File.ReadAllText(configFilePath);
                var loaded = JsonSerializer.Deserialize<T>(json) ?? new T();

                // detect which top-level properties actually exist in JSON
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var presentProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                        presentProperties.Add(prop.Name);
                }

                // create result starting from defaults, then copy values for properties that were present in the file
                var result = new T();
                var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in props)
                {
                    if (!prop.CanWrite)
                        continue;

                    // determine JSON name for this property:
                    // 1) if JsonPropertyNameAttribute is present use it
                    // 2) otherwise use the CLR property name
                    // also allow a camelCase variant of the CLR name to be matched
                    var jsonNameFromAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                    var jsonCandidates = new[]
                    {
                        jsonNameFromAttr,
                        prop.Name,
                        ToCamelCase(prop.Name)
                    }.Where(s => !string.IsNullOrEmpty(s)).ToArray();

                    bool existsInJson = jsonCandidates.Any(c => presentProperties.Contains(c));
                    if (existsInJson)
                    {
                        var val = prop.GetValue(loaded);
                        prop.SetValue(result, val);
                    }
                }

                // if file had fewer properties than writable properties, we consider it "missing items"
                var writableCount = props.Count(p => p.CanWrite);
                if (presentProperties.Count < writableCount)
                    saveNeeded = true;

                return result;
            }
            catch
            {
                // on error, return defaults and schedule save to repair file
                saveNeeded = true;
                return new T();
            }
        }

        protected void SaveConfig()
        {
            var configType = typeof(T);
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{configType.Name}.json");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(configFilePath, json);
        }

        protected TValue GetConfigValue<TValue>([CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null)
                return default!;

            var property = typeof(T).GetProperty(propertyName);
            return property != null ? (TValue)property.GetValue(_config)! : default!;
        }

        protected void SetConfigValue<TValue>(TValue value, [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null)
                return;

            var property = typeof(T).GetProperty(propertyName);
            if (property != null && !Equals(property.GetValue(_config), value))
            {
                property.SetValue(_config, value);
                OnPropertyChanged(propertyName);
                SaveConfig();
            }
        }

        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}