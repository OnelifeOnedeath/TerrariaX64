using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;

namespace TerrariaX64
{
    class Program
    {
        // Interface language
        private enum Language { En, Ru, Zh }
        private static Language _currentLang = Language.En;

        static void Main(string[] args)
        {
            DetermineLanguage();
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            bool hasVanilla = File.Exists(Path.Combine(currentDir, "Terraria.exe"));
            bool hastMod = File.Exists(Path.Combine(currentDir, "tModLoader.dll")) || 
                           File.Exists(Path.Combine(currentDir, "tModLoader.exe")) ||
                           File.Exists(Path.Combine(currentDir, "tModLoader"));

            if (!hasVanilla && !hastMod)
            {
                LogWarning(_currentLang == Language.Ru ? "Ошибка: Компоненты Terraria или tModLoader не найдены." : _currentLang == Language.Zh ? "错误：未找到 Terraria 或 tModLoader 组件。" : "Error: Terraria or tModLoader components not found.");
                return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    LogInfo(_currentLang == Language.Ru ? "ОС: Windows" : _currentLang == Language.Zh ? "操作系统: Windows" : "OS: Windows");
                    if (hasVanilla) PatchWindowsVanilla(currentDir);
                }
                else
                {
                    LogWarning(_currentLang == Language.Ru ? "Ошибка: Неподдерживаемая ОС." : _currentLang == Language.Zh ? "错误：不支持的操作系统。" : "Error: Unsupported OS.");
                }
            }
            catch (Exception ex)
            {
                LogError(_currentLang == Language.Ru ? $"Критическая ошибка: {ex.Message}" : _currentLang == Language.Zh ? $"严重错误: {ex.Message}" : $"Critical error: {ex.Message}");
            }

            LogInfo(_currentLang == Language.Ru ? "Процесс оптимизации завершен." : _currentLang == Language.Zh ? "优化过程完成。" : "Optimization process completed.");
        }

        private static void DetermineLanguage()
        {
            string langCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            if (langCode == "ru") _currentLang = Language.Ru;
            else if (langCode == "zh") _currentLang = Language.Zh;
            else _currentLang = Language.En;
        }

        private static void PatchWindowsVanilla(string gameDir)
        {
            LogInfo(_currentLang == Language.Ru ? "Патчинг Terraria для Windows..." : _currentLang == Language.Zh ? "正在运行 Windows 补丁..." : "Patching Terraria for Windows...");

            string exePath = Path.Combine(gameDir, "Terraria.exe");
            string backupPath = Path.Combine(gameDir, "Terraria.bak");

            if (!File.Exists(backupPath))
            {
                File.Copy(exePath, backupPath);
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
                    characteristics |= 0x0020; // LAA Flag

                    stream.Position = characteristicsOffset;
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(characteristics); // ТУТ БЫЛА ОШИБКА
                    }
                }
            }
            LogInfo(_currentLang == Language.Ru ? "- Флаг Large Address Aware установлен." : _currentLang == Language.Zh ? "- 已设置 Large Address Aware 标志。" : "- Large Address Aware flag set.");
        }

        private static void LogInfo(string msg) => Console.WriteLine($"[INFO] {msg}");
        private static void LogWarning(string msg) => Console.WriteLine($"[WARN] {msg}");
        private static void LogError(string msg) => Console.WriteLine($"[ERROR] {msg}");
    }
}