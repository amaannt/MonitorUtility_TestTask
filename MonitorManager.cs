using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorUtility_TestTask
{
    internal class MonitorManager
    {
        //Flag for when and if user quits the app
        internal static bool UserQuit;
        private List<Thread> MonitorThread;
        public MonitorManager()
        {
            UserQuit = false;
            //initialize thread list
            MonitorThread = new List<Thread>();
            InitUserInput();
        }
        private void InitUserInput()
        {
            //start a new thread to detect escape key
            InitQuitThread();

            //runs until user presses 'q'
            while (!UserQuit)
            { 
                //get process name
                Console.WriteLine("Enter process name:");
                if(!CancelableReadLine(out string pName))
                {
                    UserQuit = true;
                    break;
                }
                if (string.IsNullOrEmpty(pName))
                {
                    Console.WriteLine("Invalid process name. Please try again.");
                    continue;
                }
                Process[] processes = Process.GetProcessesByName(pName);
                if(processes.Length == 0)
                {
                    Console.WriteLine("Process not found! Try again!");
                    continue;
                }
                Console.WriteLine(string.Format("Enter maximum lifetime for '{0}'(in minutes):", pName));
                if (!CancelableReadLine(out string input_pDuration))
                {
                    UserQuit = true;
                    break;
                }
                int pDuration = -1;
                if (!HandleIntegerInputValidation(input_pDuration, out pDuration))
                {
                    continue;
                }

                Console.WriteLine(string.Format("Enter monitoring frequency for '{0}'(in minutes):", pName));
                if (!CancelableReadLine(out string input_pfreq))
                {
                    UserQuit = true;
                    break;
                }
                int pMonitoringFrequency = -1;
                if (!HandleIntegerInputValidation(input_pfreq, out pMonitoringFrequency))
                {
                    continue;
                }
                //validate larger value


                //add the method to run to a new thread
                MonitorThread.Add(new Thread(() => MonitorProcessThread(pDuration, pMonitoringFrequency, pName, MonitorThread.Last())));
                //start the freshly added thread
                MonitorThread.Last().Start();

                Console.WriteLine(string.Format("Started tracking '{0} for {1} every {2} minutes):", pName, pDuration, pMonitoringFrequency));

            }
        }

        private void MonitorProcessThread(int processDuration, int MonitorFrequency, string processName, Thread currThread)
        {
            DateTime threadStartTime = DateTime.Now;
            DateTime LastCheckTime = threadStartTime;
            bool StartCheck = false;
            while (!UserQuit)
            {
                
                if (!StartCheck)
                {
                    int MinutesElapsedSinceLastCheck = DateTime.Now.Minute - LastCheckTime.Minute;
                    StartCheck = MinutesElapsedSinceLastCheck >= MonitorFrequency;
                }
                if (StartCheck)
                {
                    LastCheckTime = DateTime.Now;
                    //get time elapsed since the start of the thread
                    TimeSpan elapsedTime = DateTime.Now - threadStartTime;

                    //if the total amount of minutes is greater than the time duration alotted for the process, it is shutdown
                    if (elapsedTime.TotalMinutes >= processDuration)
                    {
                        Process[] processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            
                            foreach(Process process in processes)
                            {
                                Console.WriteLine($"Killing process {process.ProcessName} (PID: {process.Id})...");
                                process.Kill();
                            } 
                            //remove thread from list
                            //MonitorThread.Remove(currThread);
                            //break out of the while loop and end the thread
                           // break;
                        }
                    }
                    StartCheck = false;
                } 
            }
        }

        private void InitQuitThread()
        {
            Console.WriteLine("Press escape to exit,");
            //Thread thread = new(QuitDetection);
            //thread.Start();
        }

        private void QuitDetection(object? obj)
        {
            do
            {
                Thread.Sleep(5);
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            
            UserQuit = true;
        }

        private bool HandleIntegerInputValidation(string? inputString, out int IntegerResult)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                Console.WriteLine("Input is empty. Please try again.");
                IntegerResult = -1;
                return false;
            }
            if (!int.TryParse(inputString, out IntegerResult))
            {
                Console.WriteLine("Unable to parse input, please try again.");
                return false;
            }
            if (IntegerResult < 0)
            {
                Console.WriteLine("Invalid input, please try again.");
                return false;
            }
            return true;
        }
        //Returns null if ESC key pressed during input.
        public static bool CancelableReadLine(out string value)
        {
            var clOffset = Console.CursorLeft;
            value = string.Empty;
            var buffer = new StringBuilder();
            var key = Console.ReadKey(true);
            while (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Escape)
            {
                if (key.Key == ConsoleKey.Backspace && Console.CursorLeft - clOffset > 0)
                {
                    var cli = Console.CursorLeft - clOffset - 1;
                    buffer.Remove(cli, 1);
                    Console.CursorLeft = clOffset;
                    Console.Write(new string(' ', buffer.Length + 1));
                    Console.CursorLeft = clOffset;
                    Console.Write(buffer.ToString());
                    Console.CursorLeft = cli + clOffset;
                    key = Console.ReadKey(true);
                }
                else if (key.Key == ConsoleKey.LeftArrow && Console.CursorLeft > 0)
                {
                    Console.CursorLeft--;
                    key = Console.ReadKey(true);
                }
                else if (key.Key == ConsoleKey.RightArrow && Console.CursorLeft < buffer.Length)
                {
                    Console.CursorLeft++;
                    key = Console.ReadKey(true);
                }
                else if (Char.IsAscii(key.KeyChar) || Char.IsWhiteSpace(key.KeyChar))
                {
                    var cli = Console.CursorLeft;
                    buffer.Insert(cli, key.KeyChar);
                    Console.CursorLeft = 0;
                    Console.Write(buffer.ToString());
                    Console.CursorLeft = cli + 1;
                    key = Console.ReadKey(true);
                }
                else
                {
                    key = Console.ReadKey(true);
                }
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                value = buffer.ToString();
                return true;
            }
            return false;
        }
    }
} 
