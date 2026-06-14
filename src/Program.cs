using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace TerrariaX64
{
    class Program
    {
        // Перечисление для поддерживаемых языков
        private enum Language { En, Ru, Zh }
        private static Language _currentLang = Language.En;

        static void Main(string[] args)
        {
            DetermineLanguage();

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            bool hasVanilla = File.Exists(Path.Combine(currentDir, "Terraria.exe"));
            bool hastMod = File.Exists(Path.Combine(currentDir, "tModLoader.dll")) || 
                           File.Exists(Path.Combine(currentDir, "tModLoader.exe"));

            if (!hasVanilla && !hastMod)
            {
                LogWarning(
                    _currentLang == Language.Ru ? "Ошибка: Файлы Terraria или tModLoader не найдены в текущей директории." :
                    _currentLang == Language.Zh ? "错误：在当前目录中未找到 Terraria 或 tModLoader 文件。" :
                    "Error: Terraria or tModLoader files not found in the current directory."
                );
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    LogInfo(
                        _currentLang == Language.Ru ? "ОС: Обнаружена Windows." : 
                        _currentLang == Language.Zh ? "操作系统：检测到 Windows。" : 
                        "OS: Windows detected."
                    );
                    ExecuteWindowsWorkflow(currentDir, hasVanilla, hastMod);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LogInfo(
                        _currentLang == Language.Ru ? "ОС: Обнаружен Linux." : 
                        _currentLang == Language.Zh ? "操作系统：检测到 Linux。" : 
                        "OS: Linux detected."
                    );
                    ExecuteLinuxWorkflow(currentDir, hasVanilla, hastMod);
                }
                else
                {
                    LogWarning(
                        _currentLang == Language.Ru ? "Ошибка: Неподдерживаемая операционная система." : 
                        _currentLang == Language.Zh ? "错误：不支持的操作系统。" : 
                        "Error: Unsupported operating system."
                    );
                }
            }
            catch (Exception ex)
            {
                LogError(
                    _currentLang == Language.Ru ? $"Критическая ошибка при выполнении: {ex.Message}" : 
                    _currentLang == Language.Zh ? $"执行过程中 weights 严重错误: {ex.Message}" : 
                    $"Critical error during execution: {ex.Message}"
                );
            }
        }

        private static void DetermineLanguage()
        {
            string langCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            if (langCode == "ru") _currentLang = Language.Ru;
            else if (langCode == "zh") _currentLang = Language.Zh;
            else _currentLang = Language.En;
        }

        private static void ExecuteWindowsWorkflow(string gameDir, bool hasVanilla, bool hastMod)
        {
            if (hasVanilla) PatchWindowsVanilla(gameDir);
            if (hastMod) OptimizeTModRuntime(gameDir);
        }

        private static void ExecuteLinuxWorkflow(string gameDir, bool hasVanilla, bool hastMod)
        {
            if (hasVanilla) PatchLinuxVanilla(gameDir);
            if (hastMod) OptimizeTModRuntime(gameDir);
        }

        private static void PatchWindowsVanilla(string gameDir)
        {
            LogInfo(
                _currentLang == Language.Ru ? "Запуск патча для ванильной Terraria (Windows)..." : 
                _currentLang == Language.Zh ? "正在 Launch 启动原版 Terraria 补丁 (Windows)..." : 
                "Patching Vanilla Terraria for Windows..."
            );

            string exePath = Path.Combine(gameDir, "Terraria.exe");
            string backupPath = Path.Combine(gameDir, "Terraria.bak");

            if (!File.Exists(backupPath))
            {
                File.Copy(exePath, backupPath);
                LogInfo(
                    _currentLang == Language.Ru ? "- Создан бэкап оригинального Terraria.exe" : 
                    _currentLang == Language.Zh ? "- 已创建原版 Terraria.exe 备份" : 
                    "- Created backup of original Terraria.exe"
                );
            }

            using (var stream = new FileStream(exePath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = 0x3C;
                    int peOffset = reader.ReadInt32();

                    long characteristicsOffset = peOffset + 4 + 18;
                    stream.Position = characteristicsOffset;
                    
                    short characteristics = reader.ReadInt16();
                    characteristics |= 0x0020; // IMAGE_FILE_LARGE_ADDRESS_AWARE

                    stream.Position = characteristicsOffset;
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(characteristics);
                    }
                }
            }
            LogInfo(
                _currentLang == Language.Ru ? "- Флаг LARGE_ADDRESS_AWARE успешно применен." : 
                _currentLang == Language.Zh ? "- LARGE_ADDRESS_AWARE 标志已成功应用。" : 
                "- Applied IMAGE_FILE_LARGE_ADDRESS_AWARE flag."
            );
        }

        private static void PatchLinuxVanilla(string gameDir)
        {
            LogInfo(
                _currentLang == Language.Ru ? "Настройка среды x64 FNA для Linux..." : 
                _currentLang == Language.Zh ? "正在配置 Linux x64 FNA 环境..." : 
                "Preparing Linux x64 FNA environment..."
            );
            
            string configPath = Path.Combine(gameDir, "FNA.dll.config");
            if (!File.Exists(configPath))
            {
                string configContent = 
@"<configuration>
    <dllmap dll=""i:sharpdx_direct3d11.dll"" target=""FNA3D.dll""/>
    <dllmap dll=""SDL2.dll"" os=""osx"" target=""libSDL2-2.0.0.dylib""/>
    <dllmap dll=""SDL2.dll"" os=""linux"" target=""libSDL2-2.0.so.0""/>
    <dllmap dll=""FAudio.dll"" os=""osx"" target=""libFAudio.0.dylib""/>
    <dllmap dll=""FAudio.dll"" os=""linux"" target=""libFAudio.so.0""/>
</configuration>";
                
                File.WriteAllText(configPath, configContent);
                LogInfo(
                    _currentLang == Language.Ru ? "- Создан файл конфигурации FNA.dll.config." : 
                    _currentLang == Language.Zh ? "- 已生成 FNA.dll.config 配置文件。" : 
                    "- Generated FNA.dll.config for library mapping."
                );
            }
        }

        private static void OptimizeTModRuntime(string gameDir)
        {
            LogInfo(
                _currentLang == Language.Ru ? "Оптимизация среды tModLoader..." : 
                _currentLang == Language.Zh ? "正在优化 tModLoader 运行环境..." : 
                "Optimizing tModLoader runtime..."
            );

            string configPath = Path.Combine(gameDir, "tModLoader.runtimeconfig.json");

            if (!File.Exists(configPath))
            {
                LogWarning(
                    _currentLang == Language.Ru ? "- Файл конфигурации tModLoader не найден. Пропуск." : 
                    _currentLang == Language.Zh ? "- 未找到 tModLoader 配置文件。跳过。" : 
                    "- tModLoader runtime config not found. Skipping."
                );
                return;
            }

            string json = File.ReadAllText(configPath);

            if (!json.Contains("System.GC.Server"))
            {
                string optimizedJson = json.Replace(
                    "\"configProperties\": {",
                    "\"configProperties\": {\n      \"System.GC.Server\": true,\n      \"System.GC.Concurrent\": true,"
                );
                
                File.WriteAllText(configPath, optimizedJson);
                LogInfo(
                    _currentLang == Language.Ru ? "- Серверный сборщик мусора (GC) успешно включен." : 
                    _currentLang == Language.Zh ? "- 服务器垃圾回收机制 (Server GC) 已成功启用。" : 
                    "- Server GC options injected successfully."
                );
            }
            else
            {
                LogInfo(
                    _currentLang == Language.Ru ? "- Оптимизация памяти уже была применена." : 
                    _currentLang == Language.Zh ? "- 内存优化选项之前已应用。" : 
                    "- Runtime configuration is already optimized."
                );
            }
        }

        private static void LogInfo(string message) => Console.WriteLine(message);
        private static void LogWarning(string message) => Console.WriteLine($"[!] {message}");
        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }
}
