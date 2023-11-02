using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Serilog;
using Serilog.Core;

namespace Testing
{
    class PredictabitProgram
    {
        private static string currentWindowTitle = "";
        private static DateTime windowStartTime;

        static void Main(string[] args)
        {
            string logFileName = "log.txt";

            // Check if the log file exists and create it if not
            if (!File.Exists(logFileName))
            {
                using (File.Create(logFileName)) { }
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFileName, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            string currentWindowTitle = "";
            DateTime windowStartTime = DateTime.Now;
            bool isTyping = false;
            StringBuilder typedString = new StringBuilder();
            
            // Get the current Windows user's account name
            string currentUser = WindowsIdentity.GetCurrent().Name;
            
            // Initialize last typing start time
            DateTime lastTypingStartTime = DateTime.Now;

            while (true)
            {
                string activeWindow = GetActiveWindowTitle();

                if (activeWindow != currentWindowTitle)
                {
                    LogWindowChange(currentWindowTitle, windowStartTime);

                    if (isTyping)
                    {
                        LogTypingEvent(currentUser,typedString.ToString(), lastTypingStartTime);
                        isTyping = false;
                        typedString.Clear();
                    }

                    // Update the current window and start time
                    currentWindowTitle = activeWindow;
                    windowStartTime = DateTime.Now;
                }

                if (Console.KeyAvailable)
                {
                    if (!isTyping)
                    {
                        // Start a typing event
                        typedString.Clear();
                        isTyping = true;
                        lastTypingStartTime = DateTime.Now; // Update last typing start time
                    }

                    ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                    typedString.Append(keyInfo.KeyChar);
                }
                else
                {
                    if (isTyping)
                    {
                        TimeSpan timeSinceLastKey = DateTime.Now - lastTypingStartTime;
                        if (timeSinceLastKey.TotalSeconds > 20)
                        {
                            LogTypingEvent(currentUser, typedString.ToString(), lastTypingStartTime);
                            isTyping = false;
                            typedString.Clear();
                        }
                    }
                }

                // Sleep to track usage every second
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void LogWindowChange(string windowTitle, DateTime startTime)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                TimeSpan timeSpent = DateTime.Now - startTime;
                Log.Information("Window: {WindowTitle}, Time Spent: {TimeSpent}", windowTitle, timeSpent);
            }
        }

        private static void LogTypingEvent(string user, string typedText, DateTime startTime)
        {
            if (!string.IsNullOrEmpty(typedText))
            {
                TimeSpan elapsed = DateTime.Now - startTime;
                Log.Information("{User} stopped typing for {Elapsed}", user, elapsed);
                // Log the typed text as well
                Log.Information("Typed Text: {TypedText}", typedText);
            }
        }

        private static string GetActiveWindowTitle()
        {
            IntPtr handle = GetForegroundWindow();
            const int nChars = 256;
            var buff = new StringBuilder(nChars);
            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return null;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}