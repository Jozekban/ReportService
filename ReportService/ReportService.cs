using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReportService
{
    public partial class ReportService : ServiceBase
    {
        private Thread _job;
        private Timer _timer;
        private int _waitTime;
        private int _interval;
        private readonly string _path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\plik.txt";
        private readonly string _dateFormat = "dd/MM/yyyy HH:mm:ss";
        private ServiceStatus _status;

        public ReportService()
        {
            InitializeComponent();
            _status = new ServiceStatus();
            _job = new Thread((data) => WriteToFile(data));
            _status.dwWaitHint = 100000;
        }

        private void SetServiceStatus(ServiceState state)
        {
            _status.dwCurrentState = state;
            SetServiceStatus(ServiceHandle, ref _status);
        }

        protected override void OnStart(string[] args)
        {
            SetServiceStatus(ServiceState.SERVICE_START_PENDING);
            _waitTime = 1000;
            _interval = 1000;
            _timer = new Timer(new TimerCallback(doJob),null,_waitTime,_interval);
            SetServiceStatus(ServiceState.SERVICE_RUNNING);
        }

        protected override void OnStop()
        {
            SetServiceStatus(ServiceState.SERVICE_STOP_PENDING);
            _waitTime = Timeout.Infinite;
            _interval = Timeout.Infinite;
            _timer.Change(_waitTime, _interval);
            SetServiceStatus(ServiceState.SERVICE_STOPPED);
        }

        protected override void OnPause()
        {
            SetServiceStatus(ServiceState.SERVICE_PAUSE_PENDING);
            _waitTime = Timeout.Infinite;
            _interval = Timeout.Infinite;
            _timer.Change(_waitTime, _interval);
            SetServiceStatus(ServiceState.SERVICE_PAUSED);
        }

        protected override void OnContinue()
        {
            SetServiceStatus(ServiceState.SERVICE_CONTINUE_PENDING);
            _waitTime = 1000;
            _interval = 1000;
            _timer.Change(_waitTime, _interval);
            SetServiceStatus(ServiceState.SERVICE_RUNNING);
        }

        private void doJob(object state)
        {
            object data = "text to write";
            _job.Start(data);
            _interval += 100;
            _timer.Change(_waitTime,_interval);
        }

        private void WriteToFile(object data)
        {
            var text = (string)data;
            try
            {
                using (var writer = new StreamWriter(_path, true))
                {
                    writer.WriteLine($"{text} - {DateTime.Now.ToString(_dateFormat)}");
                    writer.Close();
                }
            }
            catch (Exception exception)
            {
                if (!EventLog.SourceExists(Constants.EventLogSource))
                {
                    EventLog.CreateEventSource(Constants.EventLogSource, Constants.EventLogType);
                }
                var error = $"{Constants.UnauthorizedAccess}\n{exception.Message}\n{exception.Data}";
                EventLog.WriteEntry(Constants.EventLogSource, error, EventLogEntryType.Error);

                _waitTime = Timeout.Infinite;
                _interval = Timeout.Infinite;
                _timer.Change(_waitTime, _interval);
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public long dwServiceType;
            public ServiceState dwCurrentState;
            public long dwControlsAccepted;
            public long dwWin32ExitCode;
            public long dwServiceSpecificExitCode;
            public long dwCheckPoint;
            public long dwWaitHint;
        };
    }
}
