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
        private static List<string> MonitorProcessNames;
        public MonitorManager()
        {
            UserQuit = false;
            //initialize thread list
            MonitorThread = new List<Thread>();
            InitUserInput();
        }
        private void InitUserInput()
        {
            MonitorProcessNames = new List<string>();
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
                //validate input
                if (string.IsNullOrEmpty(pName))
                {
                    Console.WriteLine("Invalid process name. Please try again.");
                    continue;
                }
                //get duration
                Console.WriteLine($"Enter maximum lifetime for '{pName}'(in minutes):");
                
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
                //get check interval
                Console.WriteLine($"Enter monitoring frequency for '{pName}'(in minutes):");
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
                //check if already monitoring process
                if (MonitorProcessNames.Contains(pName))
                {
                    Console.WriteLine($"Already Monitoring '{pName}'.\nPlease enter another process.");
                    continue;
                }


                //add the method to run to a new thread
                MonitorThread.Add(new Thread(() =>MonitorProcessThread(pDuration, pMonitoringFrequency, pName)));
                MonitorProcessNames.Add(pName);
                //start the freshly added thread
                MonitorThread.Last().Start();

                Console.WriteLine($"Started tracking '{pName} for {pDuration} minute(s) every {pMonitoringFrequency} minute(s) at {DateTime.Now.ToString("HH:mm:ss")}.");

            }
        }

        private static Task MonitorProcessThread(int processDuration, int MonitorFrequency, string processName)
        {
            DateTime monitorStartTime = DateTime.Now;
            //DateTime LastCheckTime = monitorStartTime;
            //bool StartCheck = false;
           // bool resetStartTime = false;
            while (!UserQuit)
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    monitorStartTime = DateTime.Now;
                    //LastCheckTime = monitorStartTime;
                }
                //n minutes in seconds to ms : n * 60 * 1000
                Thread.Sleep(MonitorFrequency * 60 * 1000);

                //get time elapsed since the start of the thread
                TimeSpan elapsedTime = DateTime.Now - monitorStartTime;

                //if the total amount of minutes is greater than the time duration alotted for the process, it is shutdown
                if (elapsedTime.TotalMinutes >= processDuration)
                {
                    processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        foreach (Process process in processes)
                        {
                            Console.WriteLine($"Killing process {process.ProcessName} (PID: {process.Id}) at {DateTime.Now.ToString("HH:mm:ss")}");
                            process.Kill();
                        }
                        //remove thread from list
                        //MonitorThread.Remove(currThread);
                        //break out of the while loop and end the thread
                        //break;
                    }
                    else
                    {
                        Console.WriteLine($"No '{processName}' available yet.");
                    }
                }
            }
            return Task.CompletedTask;
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
        //custom function for user input and 
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
