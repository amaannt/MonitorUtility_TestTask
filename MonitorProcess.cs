using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorUtility_TestTask
{
    internal class MonitorProcess
    {

       internal int Process_ID;
       internal string Process_Name;
       internal DateTime Process_StartTime;
       internal DateTime Process_LastCheckTime;

        public MonitorProcess(int process_ID, string process_Name, DateTime process_StartTime, DateTime process_LastCheckTime)
        {
            Process_ID = process_ID;
            Process_Name = process_Name;
            Process_StartTime = process_StartTime;
            Process_LastCheckTime = process_LastCheckTime;
        }
        public MonitorProcess(Process proc, DateTime process_StartTime, DateTime process_LastCheckTime)
        {
            Process_ID = proc.Id;
            Process_Name = proc.ProcessName;
            Process_StartTime = process_StartTime;
            Process_LastCheckTime = process_LastCheckTime;
        }
    }
}
