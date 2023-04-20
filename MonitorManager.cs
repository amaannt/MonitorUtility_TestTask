using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorUtility_TestTask
{
    //Manage an instance of process Monitor
    internal class MonitorManager
    {
        // Log file
        private const string LogFileNamePath = "ProcessLog.txt";
        //Flag for when and if user quits the app
        //internal static bool UserQuit;
        private List<Thread> MonitorThread;

        private static List<string> MonitorProcessNames = new();
        private static List<ManualResetEvent> MonitorProcessesWaitEvent = new();

        //ctor
        public MonitorManager()
        {
            //initialize thread list
            MonitorThread = new List<Thread>();
            
            InitUserInput();
            if(MonitorThread.Count > 0)
            {
                Console.WriteLine("Please wait for thread to shutdown...");
            }
        }

        //Start receiving user input and detection once process is given       
        private void InitUserInput()
        {
            
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
                //check if already monitoring process
                if (MonitorProcessNames.Contains(pName))
                {
                    Console.WriteLine($"Already Monitoring '{pName}'.\nPlease enter another process.");
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

                //get monitor frequency
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

                //add wait event to let thread be canceleable anytime 
                MonitorProcessesWaitEvent.Add(new ManualResetEvent(false));

                //add the method to run to a new thread
                MonitorThread.Add(new Thread(() =>MonitorProcessThread(pDuration, pMonitoringFrequency, pName, MonitorProcessesWaitEvent.Last())));

                //Add Name to process names just to track process names
                MonitorProcessNames.Add(pName);

                //start the newly added thread to monitor given process
                MonitorThread.Last().Start();
            }
        } 

        //Thread to monitor process and cancel when needed
        private static void MonitorProcessThread(int processDuration, int MonitorFrequency, string processName, ManualResetEvent threadWaitEvent)
        {
            DateTime monitorStartTime = DateTime.Now;

            int instanceTracker = 0;
            Console.WriteLine($"Started Monitoring instances of {processName} at {monitorStartTime}");

            //tracks process object by id
            Dictionary<int, MonitorProcess> Processes = new Dictionary<int, MonitorProcess>();
            
            //loop runs forever to track processes which may start up later
            while (true)
            {
                //get all the processes by name
                Process[] currentprocesses = Process.GetProcessesByName(processName);
                //incase no processes are found
                if (currentprocesses.Length == 0)
                {
                    Console.WriteLine($"Started Monitoring but no '{processName}' available yet.");
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
                //incase no process exists we just go back to the top of the loop
                if (Processes.Count == 0)
                {
                    continue;
                }

                //run through processes and check if their time has run out
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
                        string killTime = DateTime.Now.ToString("HH:mm:ss");
                        Console.WriteLine($"Killing process {proc.ProcessName} (PID: {proc.Id}) at {killTime}");
                        //Add to log file
                        proc.Kill();

                        //log to text file 
                        string LogString = $"\nKilled process with name: {proc.ProcessName} and ID: {proc.Id} at {killTime}";

                        File.AppendAllTextAsync(LogFileNamePath, LogString);
                        //let the process end
                        Thread.Sleep(50);
                        Processes.Remove(process.Key);
                    }
                }

            }
            Console.WriteLine($"Shutting down '{processName}' thread");
             
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
