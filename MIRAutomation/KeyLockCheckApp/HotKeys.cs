using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Security.Principal;
using System.ComponentModel;

namespace KeyLockCheckApp
{
    internal class HotKeys
    {
        private static Thread altKeyMonitorThread;
        private static bool isAltPressed;
        private static DateTime altKeyDownTime = DateTime.MinValue;
        private static int ShutdownThreshold = 20000; // 20 seconds (adjust as needed)
        private static List<string> ProcessesToClose = new List<string>();
        private static bool hasShutdownOccurred = false;

        public static void Init()
        {
            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string processesToCloseSetting = ConfigurationManager.AppSettings["ProcessesToClose"];

                if (string.IsNullOrEmpty(processesToCloseSetting))
                {
                    Console.WriteLine("[ERROR] ProcessesToClose setting is missing or empty.");
                    ProcessesToClose = new List<string>(); // Set default value or handle accordingly
                }
                else
                {
                    ProcessesToClose = processesToCloseSetting.Split(',').ToList();
                }

                string shutdownThresholdSetting = ConfigurationManager.AppSettings["WaitTimeinms"];
                if (!string.IsNullOrEmpty(shutdownThresholdSetting))
                {
                    if (int.TryParse(shutdownThresholdSetting, out int shutdownThreshold))
                    {
                        ShutdownThreshold = shutdownThreshold;
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Invalid value for WaitTimeinms setting.");
                        ShutdownThreshold = 20000; // Default value of 20 seconds
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] WaitTimeinms setting is missing.");
                    ShutdownThreshold = 20000; // Default value of 20 seconds
                }

                altKeyMonitorThread = new Thread(MonitorAltKey);
                altKeyMonitorThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Init method: {ex.Message}");
                throw;
            }
        }


        private static void MonitorAltKey()
        {
            while (true)
            {
                bool altState = (GetAsyncKeyState((int)Keys.LMenu) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.RMenu) & 0x8000) != 0;

                if (altState && !isAltPressed)
                {
                    isAltPressed = true;
                    altKeyDownTime = DateTime.Now;
                    hasShutdownOccurred = false;
                    Console.WriteLine($"[DEBUG] Alt key pressed at {altKeyDownTime}");
                }
                else if (!altState && isAltPressed)
                {
                    isAltPressed = false;
                    Console.WriteLine("[DEBUG] Alt key released.");
                    LogKeyPress(Keys.LMenu, DateTime.Now - altKeyDownTime, true);
                    altKeyDownTime = DateTime.MinValue;
                    hasShutdownOccurred = false;
                }

                if (isAltPressed && !hasShutdownOccurred)
                {
                    TimeSpan elapsed = DateTime.Now - altKeyDownTime;
                    if (elapsed.TotalMilliseconds >= ShutdownThreshold)
                    {
                        Console.WriteLine($"[DEBUG] Shutdown triggered after {elapsed.TotalMilliseconds} ms");
                        ShutdownBrowsers();
                        LogKeyPress(Keys.LMenu, elapsed, false);
                        hasShutdownOccurred = true;
                    }
                }

                Thread.Sleep(100); // Adjust the polling interval as needed
            }
        }

        private static void ShutdownBrowsers()
        {
            foreach (var procName in ProcessesToClose)
            {
                var processes = Process.GetProcessesByName(procName);
                Console.WriteLine($"[INFO] Found {processes.Length} {procName} processes to terminate.");

                foreach (Process process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Console.WriteLine($"[INFO] Process {process.ProcessName} (ID: {process.Id}) terminated.");
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] Process {process.ProcessName} (ID: {process.Id}) has already exited.");
                        }
                    }
                    catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005) // E_ACCESSDENIED
                    {
                        Console.WriteLine($"[INFO] Process {process.ProcessName} (ID: {process.Id}) is already terminating.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Unable to terminate process {process.ProcessName} (ID: {process.Id}): {ex.Message}");
                    }
                }
            }
        }

        private static void LogKeyPress(Keys key, TimeSpan elapsed, bool isKeyRelease)
        {
            const string LogFilePath = @"KeyMonitorLog.txt";
            try
            {
                string username = GetCurrentUsername();
                string eventType = isKeyRelease ? "Key Released" : "Shutdown Triggered";

                using (var fs = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var sw = new StreamWriter(fs))
                {
                    string time = DateTime.Now.ToString("F");
                    sw.WriteLine($"{time} | {username} | {eventType} | {key} | {elapsed.TotalMilliseconds} ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception when writing log: {ex.Message}");
            }
        }

        private static string GetCurrentUsername()
        {
            try
            {
                return WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unable to get current username: {ex.Message}");
                return "Unknown";
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}