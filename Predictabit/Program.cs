using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using Serilog;
using Serilog.Core;

namespace Predictabit
{
    class PredictabitProgram
    {
        private static string _currentWindowTitle = "";
        private static DateTime windowStartTime;
        private DatabaseConnection _dbConnection;

        
        public PredictabitProgram(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        
        public static void Main(string[] args)
        {
            DatabaseConnection dbConnection = new DatabaseConnection(); // Create an instance of DatabaseConnection
            PredictabitProgram program = new PredictabitProgram(dbConnection); // Create an instance of PredictabitProgram
            program.Start(args); // Call the Start method on the instance
        }

        public void Start(string[] args)
        {
            string windowTitle = "";
            DateTime startTime = DateTime.Now;
            bool isTyping = false;
            StringBuilder typedString = new StringBuilder();

            // Get the current Windows user's account name
            string currentUser = WindowsIdentity.GetCurrent().Name;

            // Check if the user exists in the database, if not, insert the username
            if (!_dbConnection.UserExists(currentUser))
            {
                _dbConnection.InsertUserName(currentUser);
            }

            // Initialize last typing start time
            DateTime lastTypingStartTime = DateTime.Now;

            while (true)
            {
                string activeWindow = GetActiveWindowTitle();

                if (activeWindow != windowTitle)
                {
                    LogWindowChange(windowTitle, startTime);

                    if (isTyping)
                    {
                        LogTypingEvent(currentUser, typedString.ToString(), lastTypingStartTime);
                        isTyping = false;
                        typedString.Clear();
                    }

                    // Update the current window and start time
                    windowTitle = activeWindow;
                    startTime = DateTime.Now;
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