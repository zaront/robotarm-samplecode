using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Behaviors;
using zArm.Api.Commands;
using zArm.Api.Motion;

namespace zArm.Api
{
    public class Arm
    {
        CommandRunner _runner;
        Settings _settings;
        public Led Led { get; }
        public Button Button { get; }
        public Knob Knob { get; }
        public Sound Sound { get; }
        public Servos Servos { get; }
        public BehaviorManager Behaviors { get; }
        public ICommunication Communication { get; }

        public event EventHandler<DataEventArg<ErrorResponse>> ErrorReceived;
        public event EventHandler<DataEventArg<InfoResponse>> InfoReceived;
        public event EventHandler<DataEventArg<SoftResetResponse>> SoftResetReceived;
        private event Action<Settings> _autoSyncSettings;

        public Arm(ICommunication comm, int servoCount = 5)
        {
            //set fields
            Communication = comm;
            Behaviors = new BehaviorManager(this);
            _runner = new CommandRunner(comm);
            Led = new Led(_runner, this);
            Button = new Button(_runner, this);
            Knob = new Knob(_runner, this);
            Sound = new Sound(_runner, this);
            Servos = new Servos(_runner, this, servoCount);

            //register responses
            _runner.RegisterForResponse<ErrorResponse>(ErrorResponse);
            _runner.RegisterForResponse<InfoResponse>(InfoResponse);
            _runner.RegisterForResponse<SoftResetResponse>(SoftResetResponse);
        }

        /// <summary>
        /// perform a soft reset.  turn off motors, LED, sound, but retain settings.
        /// </summary>
        public void SoftReset()
        {
            _runner.Execute(new SoftResetCommand());
        }

        /// <summary>
        /// Send a ping and awaits a reply
        /// </summary>
        public async Task<bool> PingAsync()
        {
            var command = new AsyncCommand<bool>(new PingCommand(), i => 
            {
                var info = i as InfoResponse;
                if (info != null && info.Message == CommandBuilder.PingMessage)
                    return true;
                return false;
            });
            return await _runner.ExecuteAsync(command);
        }

        /// <summary>
        /// Get help information on availible commands.  the InfoReceived event will fire after this call
        /// </summary>
        public void GetHelp()
        {
            _runner.Execute(new HelpCommand());
        }


        /// <summary>
        /// get stored settings
        /// </summary>
        /// <param name="settingID">null = all settings</param>
        /// <param name="autoSync">returns a settings object that will automaticly stay in sync with future GetSettings and SetSettings calls.  It will also automaticly call SetSettings when its properties are changed</param>
        public async Task<Settings> GetSettingsAsync(SettingsID? settingID = null, bool autoSync = false)
        {
            var result = new Settings();
            var readSettings = new SettingReadCommand();
            if (settingID != null)
                readSettings.SettingID = (int)settingID.Value;
            var command = new AsyncCommand<Settings>(readSettings, i => 
            {
                var setting = i as SettingReadResponse;
                if (setting != null)
                {
                    //get all settings
                    if (settingID == null)
                    {
                        var allSettingsReceived = result.Set(setting);
                        if (allSettingsReceived)
                            return result;
                    }
                    //get a specific setting
                    else
                    {
                        if (setting.SettingID == (int)settingID.Value)
                        {
                            result.Set(setting);
                            return result;
                        }
                    }
                }
                return null;
            });
            var settings = await _runner.ExecuteAsync(command);

            //setup auto sync
            if (autoSync && settings != null)
            {
                _autoSyncSettings += settings.Merge;
                settings.SettingChanged += Settings_SettingChanged;

            }

            return settings;
        }

        void Settings_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            _runner.Execute(e.WriteCommand);
        }

        public void SetSettings(Settings settings)
        {
            //validate
            if (settings == null)
                return;
            var writeSettings = settings.GetWriteCommands();
            if (writeSettings == null || writeSettings.Length == 0)
                return;

            _runner.Execute(writeSettings);

            //auto sync
            _autoSyncSettings?.Invoke(settings);
        }

        public async Task ResetSettingsAsync()
        {
            _runner.Execute(new SettingWriteCommand());

            //reload all settings
            await Task.Yield();
            await LoadSettingsAsync();
        }

        /// <summary>
        /// Populates the "Settings" property of this Arm object with the lattest settings
        /// </summary>
        public async Task LoadSettingsAsync()
        {
            var settings = await GetSettingsAsync(autoSync: true);
            if (settings != null)
                _settings = settings;
        }

        /// <summary>
        /// all settings - need to call LoadSettingsAsync to populate
        /// </summary>
        public Settings Settings
        {
            get { return _settings; }
        }

		[Obsolete]
        /// <summary>
        /// get the current pose of all the servos
        /// </summary>
        /// <returns></returns>
        public async Task<MotionPose> GetCurrentPoseAsync()
        {
            var currentPose = new MotionPose();
            var command = new AsyncCommand<MotionPose>(new ServoStatusCommand(), i =>
            {
                var servoPos = i as ServoPositionChangedResponse;
                if (servoPos != null)
                {
                    //record current position
                    currentPose[servoPos.ServoID - 1] = servoPos.Position;

                    //return pose if all servo positions are received
                    if (currentPose.Servos.Count(e => e != null) == Servos.Count)
                        return currentPose;
                }
                return null;
            });
            return await _runner.ExecuteAsync(command);
        }

        void ErrorResponse(ErrorResponse status)
        {
            //fire the event
            ErrorReceived?.Invoke(this, new DataEventArg<ErrorResponse>(status));
        }

        void InfoResponse(InfoResponse status)
        {
            //fire the event
            InfoReceived?.Invoke(this, new DataEventArg<InfoResponse>(status));
        }

        void SoftResetResponse(SoftResetResponse status)
        {
            //fire the event
            SoftResetReceived?.Invoke(this, new DataEventArg<SoftResetResponse>(status));
        }
    }
}
