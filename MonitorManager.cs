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
        //internal static bool UserQuit;
        private List<Thread> MonitorThread;

        private static List<string> MonitorProcessNames;
        private static List<ManualResetEvent> MonitorProcessesWaitEvent;
        public MonitorManager()
        {
            //initialize thread list
            MonitorThread = new List<Thread>();
            MonitorProcessesWaitEvent = new List<ManualResetEvent>();
            InitUserInput();
            if(MonitorThread.Count > 0)
            {
                Console.WriteLine("Please wait for thread to shutdown...");
            }
        }
        private void InitUserInput()
        {
            MonitorProcessNames = new List<string>();
            //Can run multiple processes
            //runs until user presses 'esc'
            while (true)
            { 
                //get process name
                Console.WriteLine("Enter a process name to track:");
                if(!CancelableReadLine(out string pName))
                {
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

                //add wait event
                MonitorProcessesWaitEvent.Add(new ManualResetEvent(false)); 
                //add the method to run to a new thread
                MonitorThread.Add(new Thread(() =>MonitorProcessThread(pDuration, pMonitoringFrequency, pName, MonitorProcessesWaitEvent.Last())));
                MonitorProcessNames.Add(pName);
                //start the freshly added thread
                MonitorThread.Last().Start();
                Thread.Sleep(50);
                //Console.WriteLine($"Started tracking '{pName} for {pDuration} minute(s) every {pMonitoringFrequency} minute(s) at {DateTime.Now.ToString("HH:mm:ss")}.");

            }
        } 
        private static Task MonitorProcessThread(int processDuration, int MonitorFrequency, string processName, ManualResetEvent threadWaitEvent)
        {
            DateTime monitorStartTime = DateTime.Now;

            int instanceTracker = 0;
            Console.WriteLine($"Started Monitoring instances of {processName} at {monitorStartTime}");
            Dictionary<int, MonitorProcess> Processes = new Dictionary<int, MonitorProcess>();
            //DateTime LastCheckTime = monitorStartTime;
            //bool StartCheck = false;
            // bool resetStartTime = false;
            while (true)
            {
                //get all the processes by name
                Process[] currentprocesses = Process.GetProcessesByName(processName);
                //
                if (currentprocesses.Length == 0)
                {
                    Console.WriteLine($"No '{processName}' available yet.");
                }
                else
                {
                    //check if instance is the same as before
                    if (instanceTracker != currentprocesses.Length)
                    {
                        instanceTracker = currentprocesses.Length;
                        Console.WriteLine($"{currentprocesses.Length} instance(s) of '{processName}' detected.");
                    }
                    //if dictionary with key value pairs is empty
                    //fill it up with available processes
                    if (Processes.Count == 0)
                    {
                        for (int i = 0; i < currentprocesses.Length; i++)
                        {
                            Processes.Add(currentprocesses[i].Id, new MonitorProcess(currentprocesses[i].Id, processName, DateTime.Now, DateTime.Now));

                            Console.WriteLine($"Started tracking {currentprocesses[i].Id} for {processDuration} minute(s) every {MonitorFrequency} minute(s) at {DateTime.Now.ToString("HH:mm:ss")}.");
                        }
                    }
                    else
                    {
                        //if it is not empty, add the processes that do no exist
                        for (int i = 0; i < currentprocesses.Length; i++)
                        {
                            if (!Processes.ContainsKey(currentprocesses[i].Id))
                            {
                                Processes.Add(currentprocesses[i].Id, new MonitorProcess(currentprocesses[i].Id, processName, DateTime.Now, DateTime.Now));
                                Console.WriteLine($"Started tracking {currentprocesses[i].Id}  for {processDuration} minute(s) every {MonitorFrequency} minute(s) at {DateTime.Now.ToString("HH:mm:ss")}.");
                            }
                        }
                    }

                } 

                //if wait event is signaled that means we quit this thread immediately without having to wait for timer to run out
                if (threadWaitEvent.WaitOne(MonitorFrequency * 60 * 1000))
                {
                    break;
                }

                if (Processes.Count == 0)
                {
                    continue;
                }
                //run through processes 
                foreach (KeyValuePair<int, MonitorProcess> process in Processes)
                {
                    //get the start time of process
                    DateTime processStartTime = process.Value.Process_StartTime; 

                    //get current uptime duration of process
                    TimeSpan elapsedTime = DateTime.Now - processStartTime;
                    //in case the process duration has been surpassed, process is then shutdown and removed from dictionary 
                    if (elapsedTime.TotalMinutes >= processDuration)
                    {
                        Process proc = Process.GetProcessById(process.Key);
                        Console.WriteLine($"Killing process {proc.ProcessName} (PID: {proc.Id}) at {DateTime.Now.ToString("HH:mm:ss")}");
                        proc.Kill();
                        //let the process end
                        Thread.Sleep(50);
                        Processes.Remove(process.Key);
                    }
                }

            }
            Console.WriteLine($"Shutting down '{processName}' thread");

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

            if (key.Key == ConsoleKey.Escape)
            {
                SignalAndQuitAllThreads();
                return false;
            }
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                value = buffer.ToString();
                return true;
            }
            return false;
        }

        private static void SignalAndQuitAllThreads()
        {
            foreach (ManualResetEvent waitEvent in MonitorProcessesWaitEvent)
            {
                waitEvent.Set();
            }
        }
    }
}
