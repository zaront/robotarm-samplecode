using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zArm.Api
{
    /// <summary>
    /// Manages a list of arms connected to the systems
    /// </summary>
    public class ArmListener
    {
        IConnection[] _connections;
        Arm _currentArm;
        ConcurrentDictionary<string,Arm> _arms;
        int _scanningCount;

		public bool IsResettingCurrentArm { get; set; }

        public event EventHandler<CurrentArmEventArgs> CurrentArmChanged;
        public event EventHandler<ScanningChangesEventArgs> ScanningChanges;
        public event EventHandler<PortScannedEventArgs> PortScanned;

        public ArmListener(params IConnection[] connections)
        {
            //set fields
            _connections = connections;
            _arms = new ConcurrentDictionary<string, Arm>();
        }

        public IEnumerable<Arm> Arms
        {
            get { return _arms.Values; }
        }

        public Arm CurrentArm
        {
            get { return _currentArm; }
            set
            {
                if (_currentArm == value)
                    return;
                var prevArm = _currentArm;
                _currentArm = value;
                CurrentArmChanged?.Invoke(this, new CurrentArmEventArgs() { Arm = _currentArm, PrevArm = prevArm });
            }
        }

        public void Listen()
        {
            foreach (var connection in _connections)
            {
                if (!connection.Listen)
                {
                    connection.ConnectionAvailible += Connection_ConnectionAvailible;
                    connection.ConnectionUnavailible += Connection_ConnectionUnavailible;
                    connection.Listen = true;
                }
            }
        }

        public void StopListening()
        {
            foreach (var connection in _connections)
            {
                if (connection.Listen)
                {
                    connection.ConnectionAvailible -= Connection_ConnectionAvailible;
                    connection.ConnectionUnavailible -= Connection_ConnectionUnavailible;
                    connection.Listen = false;
                }
            }
        }

		public void ResetCurrentArm()
		{
			if (CurrentArm != null)
			{
				//reset arm
				CurrentArm.SoftReset();

				//reset connection
				IsResettingCurrentArm = true;
				var connectionEvent = new ConnectionEventArgs()
				{
					Port = CurrentArm.Communication.ConnectedPort,
					PossibleArm = true,
					ExistingDevice = true,
					Communication = CurrentArm.Communication
				};
				Connection_ConnectionUnavailible(this, connectionEvent); //disconnect
				Connection_ConnectionAvailible(this, connectionEvent); //reconnect
				IsResettingCurrentArm = false;
			}
		}

        private void Connection_ConnectionAvailible(object sender, ConnectionEventArgs e)
        {
            //send scanning event
            if (_scanningCount == 0)
                ScanningChanges?.Invoke(this, new ScanningChangesEventArgs() { Scanning = true });
            _scanningCount++;

            Task.Run(async () => 
            {
                var portScan = new PortScannedEventArgs() { Port = e.Port, PossibleArm = e.PossibleArm };
                Arm addArm = null;

                //open the connection
                var arm = new Arm(e.Communication);
                try
                {
                    e.Communication.Connect(e.Port);
                }
                catch (CommunicationException ex)
                {
                    portScan.ErrorMessage = ex.Message;
                }
                catch { }
                if (e.Communication.IsConnected)
                {
                    portScan.CanOpen = true;

                    //ping
                    if (!e.ExistingDevice)
                        await Task.Delay(2000); //wait 2 seconds after plug in to load usb driver - bug fix
                    var ping = await arm.PingAsync();
                    //try one more time
                    if (!ping)
                    {
                        await Task.Delay(2000);
                        ping = await arm.PingAsync();
                    }
                    if (!ping)
                    {
                        e.Communication.Disconnect();
                    }
                    else
                    {
                        portScan.HasArm = true;
                        portScan.Arm = arm;

                        //get all settings
                        await arm.LoadSettingsAsync();

                        //reload the arm if the servo count should be diffrent
                        if (arm.Settings != null)
                        {
                            if (arm.Settings.ActiveServos.HasValue && arm.Settings.ActiveServos.Value != arm.Servos.Count)
                            {
                                arm = new Arm(arm.Communication, arm.Settings.ActiveServos.Value);
                                await arm.LoadSettingsAsync();
                            }

                            portScan.Model = arm.Settings.ModelNumber;
                            portScan.NickName = arm.Settings.NickName;

                            //add the Arm
                            addArm = arm;
                        }
                        else
                        {
                            portScan.NeedsSettingsReset = true;
                            portScan.Arm.Communication.Disconnect();
                        }
                    }

                }

                //send port scanned event
                PortScanned?.Invoke(this, portScan);

                //add to the arm list
                if (addArm != null)
                {
                    _arms.TryAdd(e.Port, addArm);
                    if (CurrentArm == null)
                        CurrentArm = addArm;
                }

                //send finished scanning event
                _scanningCount--;
                if (_scanningCount == 0)
                    ScanningChanges?.Invoke(this, new ScanningChangesEventArgs() { Scanning = false });
            });
        }

        private void Connection_ConnectionUnavailible(object sender, ConnectionEventArgs e)
        {
            //send events
            PortScanned?.Invoke(this, new PortScannedEventArgs() { Port = e.Port, IsRemoved = true });

            //remove from the arm list
            Arm arm;
            if (!_arms.TryRemove(e.Port, out arm))
                return;

            //close connection
            try
            {
                arm.Communication.Disconnect();
            }
            catch { }

            //update current arm
            if (CurrentArm == arm)
                CurrentArm = (_arms.Count != 0) ? _arms.First().Value : null;
        }
    }



    public class CurrentArmEventArgs : EventArgs
    {
        public Arm Arm { get; set; }
        public Arm PrevArm { get; set; }
    }

    public class ScanningChangesEventArgs : EventArgs
    {
        public bool Scanning { get; set; }
    }

    public class PortScannedEventArgs : EventArgs
    {
        public string Port { get; set; }
        public bool IsRemoved { get; set; }
        public bool CanOpen { get; set; }
        public bool PossibleArm { get; set; }
        public bool HasArm { get; set; }
        public string Model { get; set; }
        public string NickName { get; set; }
        public string ErrorMessage { get; set; }
        public bool NeedsSettingsReset { get; set; }
        public Arm Arm { get; set; }
    }
}
