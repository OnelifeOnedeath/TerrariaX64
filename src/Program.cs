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
        // Язык интерфейса
        // 界面语言
        private enum Language { En, Ru, Zh }
        private static Language _currentLang = Language.En;

        static void Main(string[] args)
        {
            DetermineLanguage();

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check for game components
            // Проверка наличия компонентов игры
            // 检查游戏组件是否存在
            bool hasVanilla = File.Exists(Path.Combine(currentDir, "Terraria.exe"));
            bool hastMod = File.Exists(Path.Combine(currentDir, "tModLoader.dll")) || 
                           File.Exists(Path.Combine(currentDir, "tModLoader.exe")) ||
                           File.Exists(Path.Combine(currentDir, "tModLoader"));

            if (!hasVanilla && !hastMod)
            {
                LogWarning(
                    _currentLang == Language.Ru ? "Ошибка: Компоненты Terraria или tModLoader не найдены в этой папке." :
                    _currentLang == Language.Zh ? "错误：在此文件夹中未找到 Terraria 或 tModLoader 组件。" :
                    "Error: Terraria or tModLoader components not found in this folder."
                );
                return;
            }

            try
            {
                // OS Check
                // Проверка ОС
                // 操作系统检查
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    LogInfo(_currentLang == Language.Ru ? "ОС: Windows" : _currentLang == Language.Zh ? "操作系统: Windows" : "OS: Windows");
                    
                    if (hasVanilla) PatchWindowsVanilla(currentDir);
                    if (hastMod) OptimizeTModRuntime(currentDir);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LogInfo(_currentLang == Language.Ru ? "ОС: Linux" : _currentLang == Language.Zh ? "操作系统: Linux" : "OS: Linux");
                    
                    if (hasVanilla) PatchLinuxVanilla(currentDir);
                    if (hastMod) OptimizeTModRuntime(currentDir);
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

            LogInfo(_currentLang == Language.Ru ? "Процесс оптимизации успешно завершен." : _currentLang == Language.Zh ? "优化过程成功完成。" : "Optimization process completed successfully.");
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
            LogInfo(_currentLang == Language.Ru ? "Патчинг ванильной Terraria для Windows..." : _currentLang == Language.Zh ? "正在运行 Windows 原版 Terraria 补丁..." : "Patching Vanilla Terraria for Windows...");

            string exePath = Path.Combine(gameDir, "Terraria.exe");
            string backupPath = Path.Combine(gameDir, "Terraria.bak");

            // Backup original binary
            // Бэкап оригинального файла
            // 备份原始文件
            if (!File.Exists(backupPath))
            {
                File.Copy(exePath, backupPath);
                LogInfo(_currentLang == Language.Ru ? "- Создан бэкап оригинального файла." : _currentLang == Language.Zh ? "- 已创建原始文件备份。" : "- Original file backup created.");
            }

            // Set Large Address Aware flag (0x0020) to bypass 4GB RAM limit
            // Установка флага LAA (0x0020) для снятия ограничения памяти в 4GB
            // 设置 LAA 标志 (0x0020) 以解除 4GB 内存限制
            using (var stream = new FileStream(exePath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = 0x3C;
                    int peOffset = reader.ReadInt32();
                    long characteristicsOffset = peOffset + 4 + 18;
                    stream.Position = characteristicsOffset;
                    
                    short characteristics = reader.ReadInt16();
                    characteristics |= 0x0020; 

                    stream.Position = characteristicsOffset;
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.
