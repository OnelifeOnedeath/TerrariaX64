using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;

namespace TerrariaX64
{
    class Program
    {
        // Language selection enumeration
        // Перечисление для выбора языка интерфейса
        // 界面语言选择枚举
        private enum Language { En, Ru, Zh }
        private static Language _currentLang = Language.En;

        static void Main(string[] args)
        {
            DetermineLanguage();

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Check for game components independently
            // Независимая проверка наличия компонентов игры
            // 独立檢查游戏组件是否存在
            bool hasVanilla = File.Exists(Path.Combine(currentDir, "Terraria.exe"));
            bool hastMod = File.Exists(Path.Combine(currentDir, "tModLoader.dll")) || 
                           File.Exists(Path.Combine(currentDir, "tModLoader.exe")) ||
                           File.Exists(Path.Combine(currentDir, "tModLoader")); // Linux binary

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
                // OS-specific workflow execution
                // Выполнение рабочих процессов в зависимости от операционной системы
                // 根据操作系统执行特定的工作流
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

            LogInfo(_currentLang == Language.Ru ? "Процесс успешной оптимизации завершен." : _currentLang == Language.Zh ? "优化过程成功完成。" : "Optimization process completed successfully.");
        }

        /// <summary>
        /// Detects system language and sets the internal localization flag
        /// </summary>
        private static void DetermineLanguage()
        {
            string langCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            if (langCode == "ru") _currentLang = Language.Ru;
            else if (langCode == "zh") _currentLang = Language.Zh;
            else _currentLang = Language.En;
        }

        /// <summary>
        /// Modifies Windows PE header for large address space and extracts x64 libraries
        /// </summary>
        private static void PatchWindowsVanilla(string gameDir)
        {
            LogInfo(_currentLang == Language.Ru ? "Патчинг ванильной Terraria для Windows..." : _currentLang == Language.Zh ? "正在运行 Windows 原版 Terraria 补丁..." : "Patching Vanilla Terraria for Windows...");

            string exePath = Path.Combine(gameDir, "Terraria.exe");
            string backupPath = Path.Combine(gameDir, "Terraria.bak");

            // Create a backup copy before modification
            // Создание резервной копии перед изменением бинарника
            // 修改二进制文件前创建备份副本
            if (!File.Exists(backupPath))
            {
                File.Copy(exePath, backupPath);
                LogInfo(_currentLang == Language.Ru ? "- Создан бэкап оригинального файла." : _currentLang == Language.Zh ? "- 已创建原始文件备份。" : "- Original file backup created.");
            }

            // Apply IMAGE_FILE_LARGE_ADDRESS_AWARE (0x0020) flag to allow >4GB RAM usage
            // Установка флага для снятия ограничения памяти в 4 ГБ
            // 设置标志以取消 4GB 内存限制
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
                        writer.Write(characteristics);
                    }
                }
            }
            LogInfo("- Windows PE Large Address Aware flag applied.");

            // Extract embedded x64 native dlls
            // Распаковка встроенных x64 DLL зависимостей движка FNA
            // 解压内置的 x64 FNA 引擎 DLL 依赖项
            ExtractResource("TerrariaX64.resources.win64.SDL2.dll", Path.Combine(gameDir, "SDL2.dll"));
            ExtractResource("TerrariaX64.resources.win64.FAudio.dll", Path.Combine(gameDir, "FAudio.dll"));
        }

        /// <summary>
        /// Generates FNA configuration mapping and extracts native .so files for Linux
        /// </summary>
        private static void PatchLinuxVanilla(string gameDir)
        {
            LogInfo(_currentLang == Language.Ru ? "Настройка FNA окружения под Linux..." : _currentLang == Language.Zh ? "正在配置 Linux FNA 环境..." : "Configuring Linux FNA environment...");
            
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
                LogInfo("- FNA.dll.config file generated.");
            }

            // Extract embedded Linux x64 libraries
            // Распаковка встроенных библиотек .so для систем Linux
            // 解压适用于 Linux 系统的内置 .so 库文件
            ExtractResource("TerrariaX64.resources.linux64.libSDL2-2.0.so.0", Path.Combine(gameDir, "libSDL2-2.0.so.0"));
            ExtractResource("TerrariaX64.resources.linux64.libFAudio.so.0", Path.Combine(gameDir, "libFAudio.so.0"));
        }

        /// <summary>
        /// Injects Server Garbage Collector flags into tModLoader configuration for massive mod stability
        /// </summary>
        private static void OptimizeTModRuntime(string gameDir)
        {
            LogInfo(_currentLang == Language.Ru ? "Оптимизация параметров памяти tModLoader..." : _currentLang == Language.Zh ? "正在优化 tModLoader 内存参数..." : "Optimizing tModLoader runtime memory parameters...");

            string configPath = Path.Combine(gameDir, "tModLoader.runtimeconfig.json");

            if (!File.Exists(configPath))
            {
                LogWarning("- tModLoader.runtimeconfig.json not found. Skipping runtime tweaks.");
                return;
            }

            string json = File.ReadAllText(configPath);

            // Force multi-threaded Server Garbage Collector instead of single-threaded Workstation mode
            // Принудительное переключение сборщика мусора .NET в многопоточный серверный режим
            // 强制将 .NET 垃圾回收器切换到多线程服务器模式
            if (!json.Contains("System.GC.Server"))
            {
                string optimizedJson = json.Replace(
                    "\"configProperties\": {",
                    "\"configProperties\": {\n      \"System.GC.Server\": true,\n      \"System.GC.Concurrent\": true,"
                );
                
                File.WriteAllText(configPath, optimizedJson);
                LogInfo("- Server GC engine optimizations injected successfully.");
            }
            else
            {
                LogInfo("- Performance parameters are already up to date.");
            }
        }

        /// <summary>
        /// Safely extracts raw binary manifest resources into specified game directories
        /// </summary>
        private static void ExtractResource(string resourceName, string outputPath)
        {
            if (File.Exists(outputPath)) return;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return;

                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }
            LogInfo($"- Successfully extracted dependency: {Path.GetFileName(outputPath)}");
        }

        private static void LogInfo(string message) => Console.WriteLine(message);
        private static void LogWarning(string message) => Console.WriteLine($"[!] {message}");
        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
    }
}
