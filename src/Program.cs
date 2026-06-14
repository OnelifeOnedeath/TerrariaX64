using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;

namespace TerrariaX64
{
    class Program
    {
        private static bool _isRussian;

        static void Main(string[] args)
        {
            _isRussian = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            bool hasVanilla = File.Exists(Path.Combine(currentDir, "Terraria.exe"));
            bool hastMod = File.Exists(Path.Combine(currentDir, "tModLoader.dll")) || 
                           File.Exists(Path.Combine(currentDir, "tModLoader.exe"));

            if (!hasVanilla && !hastMod)
            {
                LogWarning(_isRussian 
                    ? "Ошибка: Файлы Terraria или tModLoader не найдены в текущей директории." 
                    : "Error: Terraria or tModLoader files not found in the current directory.");
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    LogInfo(_isRussian ? "ОС: Обнаружена Windows." : "OS: Windows detected.");
                    ExecuteWindowsWorkflow(currentDir, hasVanilla, hastMod);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LogInfo(_isRussian ? "ОС: Обнаружен Linux." : "OS: Linux detected.");
                    ExecuteLinuxWorkflow(currentDir, hasVanilla, hastMod);
                }
                else
                {
                    LogWarning(_isRussian ? "Ошибка: Неподдерживаемая операционная система." : "Error: Unsupported operating system.");
                }
            }
            catch (Exception ex)
            {
                LogError(_isRussian ? $"Критическая ошибка при выполнении: {ex.Message}" : $"Critical error during execution: {ex.Message}");
            }
        }

        private static void ExecuteWindowsWorkflow(string gameDir, bool hasVanilla, bool hastMod)
        {
            if (hasVanilla)
            {
                PatchWindowsVanilla(gameDir);
            }
            if (hastMod)
            {
                OptimizeTModRuntime(gameDir);
            }
        }

        private static void ExecuteLinuxWorkflow(string gameDir, bool hasVanilla, bool hastMod)
        {
            if (hasVanilla)
            {
                PatchLinuxVanilla(gameDir);
            }
            if (hastMod)
            {
                OptimizeTModRuntime(gameDir);
            }
        }

        private static void PatchWindowsVanilla(string gameDir)
        {
            LogInfo(_isRussian ? "Запуск патча для ванильной Terraria (Windows)..." : "Patching Vanilla Terraria for Windows...");

            string exePath = Path.Combine(gameDir, "Terraria.exe");
            string backupPath = Path.Combine(gameDir, "Terraria.bak");

            if (!File.Exists(backupPath))
            {
                File.Copy(exePath, backupPath);
                LogInfo(_isRussian ? "- Создан бэкап оригинального Terraria.exe" : "- Created backup of original Terraria.exe");
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
            LogInfo(_isRussian ? "- Флаг LARGE_ADDRESS_AWARE успешно применен." : "- Applied IMAGE_FILE_LARGE_ADDRESS_AWARE flag.");
        }

        private static void PatchLinuxVanilla(string gameDir)
        {
            LogInfo(_isRussian ? "Настройка среды x64 FNA для Linux..." : "Preparing Linux x64 FNA environment...");
            
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
                LogInfo(_isRussian ? "- Создан файл конфигурации FNA.dll.config." : "- Generated FNA.dll.config for library mapping.");
            }
        }

        private static void OptimizeTModRuntime(string gameDir)
        {
            LogInfo(_isRussian ? "Оптимизация среды tModLoader..." : "Optimizing tModLoader runtime...");

            string configPath = Path.Combine(gameDir, "tModLoader.runtimeconfig.json");

            if (!File.Exists(configPath))
            {
                LogWarning(_isRussian ? "- Файл конфигурации tModLoader не найден. Пропуск." : "- tModLoader runtime config not found. Skipping.");
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
                LogInfo(_isRussian ? "- Серверный сборщик мусора (GC) успешно включен." : "- Server GC options injected successfully.");
            }
            else
            {
                LogInfo(_isRussian ? "- Оптимизация памяти уже была применена." : "- Runtime configuration is already optimized.");
            }
        }

        private static void LogInfo(string message) => Console.WriteLine(message);
        private static void LogWarning(string message) => Console.WriteLine($"[!] {message}");
        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }
}
