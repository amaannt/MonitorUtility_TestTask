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
        private bool UserQuit;
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
                string? pName = Console.ReadLine();
                if(string.IsNullOrEmpty(pName))
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
                string? input_pDuration = Console.ReadLine();
                int pDuration = -1;
                if (!HandleIntegerInputValidation(input_pDuration, out pDuration))
                {
                    continue;
                }

                Console.WriteLine("Enter monitoring frequency for '{0}'(in minutes):");
                string? input_pfreq = Console.ReadLine();
                int pMonitoringFrequency = -1;
                if (!HandleIntegerInputValidation(input_pfreq, out pMonitoringFrequency))
                {
                    continue;
                }
                //ignore decimal points

                //monitor all processes in separate threads
                foreach (Process p in processes)
                {
                    //add the method to run to a new thread
                    MonitorThread.Add(new Thread(() => MonitorProcessThread(pDuration, pMonitoringFrequency, p)));
                    //start the freshly added thread
                    MonitorThread.Last().Start();
                }
            }
        }

        private void MonitorProcessThread(int processDuration, int MonitorFrequency, Process process)
        {
            DateTime threadStartTime = DateTime.Now; 
            float TimePassedSinceLastCheck = 0.0f; 
            while (!UserQuit)
            {
                bool StartCheck;
                if ((TimePassedSinceLastCheck / 60) < MonitorFrequency)
                {
                    StartCheck = false;
                    TimePassedSinceLastCheck += DateTime.Now.Second;
                }
                else
                {
                    StartCheck = true;
                    TimePassedSinceLastCheck = 0.0f;
                }
                if (StartCheck)
                {
                    //get time elapsed since the start of the thread
                    TimeSpan elapsedTime = DateTime.Now - threadStartTime;

                    //if the total amount of minutes is greater than the time duration alotted for the process, it is shutdown
                    if (elapsedTime.TotalMinutes >= processDuration)
                    {
                        Console.WriteLine($"Killing process {process.ProcessName} (PID: {process.Id})...");
                        process.Kill();
                        //break out of the while loop and end the thread
                        break;
                    }

                } 
            }
        }

        private void InitQuitThread()
        {
            Console.WriteLine("Press escape to exit,");
            Thread thread = new Thread(QuitDetection);
            thread.Start();
        }

        private void QuitDetection(object? obj)
        {
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true); // read a key without displaying it

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Escape key pressed. Exiting...");
                    break;
                }

                // do something else
                Console.WriteLine($"Key pressed: {keyInfo.KeyChar}");
            }
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
    }
}
