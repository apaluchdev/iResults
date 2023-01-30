using irsdkSharp;
using irsdkSharp.Serialization;
using irsdkSharp.Serialization.Models.Data;
using irsdkSharp.Serialization.Models.Fastest;
using irsdkSharp.Serialization.Models.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iResults
{
    public class SessionTracker
    {
        private Guid? SessionId;

        private DateTime SessionStartTime;

        private int SessionStint = 0;

        private bool IsOnPitRoad = true;

        private IRacingSDK _irsdk;

        private IRacingSessionModel? _session;

        int lastUpdate = -1;

        private int _driverId;

        private int _currentLapsCompleted = -1;

        private float _lastLapTime = 0;

        public bool IsConnected { get; private set; }

        public SessionTracker()
        {
            _irsdk = new IRacingSDK();
            _irsdk.OnConnected += _irsdk_OnConnected;
            _irsdk.OnDisconnected += _irsdk_OnDisconnected;
        }

        public async Task StartSessionTracking()
        {
            // Poll for new data to send every second
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync())
            {
                if (!IsConnected)
                {
                    await Task.Delay(1000);
                    continue;
                }

                try
                {
                    ProcessData();
                }
                catch
                {
                    Console.WriteLine("iRacing connection error");
                }
                finally
                {

                }
            }
        }

        public void StopSessionTracking()
        {

        }

        private void ProcessData()
        {
            // Parse out your own driver Id
            if (_driverId == -1)
            {
                _driverId = (int)_irsdk.GetData("PlayerCarIdx");
            }

            var data = _irsdk.GetSerializedData();     

            // Is the session info updated?
            int newUpdate = _irsdk.Header.SessionInfoUpdate;
            if (newUpdate != lastUpdate)
            {
                lastUpdate = newUpdate;
                _session = _irsdk.GetSerializedSessionInfo();
            }
            
            var sessionInfoMessage = new SessionInfoMessage(SessionId, SessionStartTime, SessionStint, _session, data);

            // Only send a message if a new laptime was set
            if (data.Data.LapLastLapTime > 30 && _lastLapTime != data.Data.LapLastLapTime)
            {
                SessionMessageAvailable?.Invoke(this, sessionInfoMessage);
                _lastLapTime = data.Data.LapLastLapTime;
            }
            
        }

        public event EventHandler<SessionInfoMessage>? SessionMessageAvailable;

        private void _irsdk_OnDisconnected()
        {
            IsConnected = false;
        }

        private void _irsdk_OnConnected()
        {
            IsConnected = true;          
            SessionId = Guid.NewGuid();
            SessionStartTime = DateTime.Now;
            SessionStint = 0;
            Console.WriteLine("New session id assigned: " + SessionId.ToString());
        }
    }

    public class SessionInfoMessage : EventArgs
    {

        public string SessionType;

        public string SessionName;

        public Guid? SessionId;

        public string TrackName;

        public string CarName;

        public float TrackTemperature;

        public float AirTemperature;

        public float LastLapTime;

        public DateTime SessionStartTime;

        public int Stint;

        private Data Data { get; set; }

        public SessionInfoMessage(Guid? sessionId, DateTime sessionStartTime, int stint, IRacingSessionModel? session, IRacingDataModel data)
        {
            SessionId = sessionId;
            TrackName = session?.WeekendInfo.TrackDisplayName ?? string.Empty;
            TrackTemperature = data.Data.TrackTemp;
            AirTemperature = data.Data.AirTemp;
            LastLapTime = data.Data.LapLastLapTime;
            SessionName = session?.SessionInfo.Sessions[data.Data.SessionNum].SessionName ?? string.Empty;
            SessionType = session?.SessionInfo.Sessions[data.Data.SessionNum].SessionType ?? string.Empty;
            SessionStartTime = sessionStartTime;
            Stint = stint;

            if (session != null && session.DriverInfo.Drivers.Any())
                CarName = session.DriverInfo.Drivers[0].CarScreenName;
        }
    }
}
