using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Settings
    {
        static int _totalSettingCount = (int)Enum.GetValues(typeof(SettingsID)).Cast<SettingsID>().Max();
        Dictionary<int, object> _settings = new Dictionary<int, object>();

        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        public List<SettingsID> ReadOnlySettings { get; } = new List<SettingsID>();

		/* this section is Auto-Generated using a custom tool and is automaticly updated in VS.NET
		 * custom tool : EEPROM Index
		 * do not modify */
		public string ModelNumber { get { return Get<string>(SettingsID.ModelNumber); } set { Set(SettingsID.ModelNumber, Truncate(value)); } }
        public string FirmwareVersion { get { return Get<string>(SettingsID.FirmwareVersion); } set { Set(SettingsID.FirmwareVersion, Truncate(value)); } }
        public string SerialNumber { get { return Get<string>(SettingsID.SerialNumber); } set { Set(SettingsID.SerialNumber, Truncate(value)); } }
        public string NickName { get { return Get<string>(SettingsID.NickName); } set { Set(SettingsID.NickName, Truncate(value)); } }
        public int? ActiveServos { get { return Get<int?>(SettingsID.ActiveServos); } set { Set(SettingsID.ActiveServos, value); } }
        public int? Calibrated { get { return Get<int?>(SettingsID.Calibrated); } set { Set(SettingsID.Calibrated, value); } }
        public int? Servo1_MinRange { get { return Get<int?>(SettingsID.Servo1_MinRange); } set { Set(SettingsID.Servo1_MinRange, value); } }
        public int? Servo1_MaxRange { get { return Get<int?>(SettingsID.Servo1_MaxRange); } set { Set(SettingsID.Servo1_MaxRange, value); } }
        public float? Servo1_ServoLinearization { get { return Get<float?>(SettingsID.Servo1_ServoLinearization); } set { Set(SettingsID.Servo1_ServoLinearization, value); } }
        public float? Servo1_ServoOffset { get { return Get<float?>(SettingsID.Servo1_ServoOffset); } set { Set(SettingsID.Servo1_ServoOffset, value); } }
        public float? Servo1_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo1_FeedbackLinearization); } set { Set(SettingsID.Servo1_FeedbackLinearization, value); } }
        public float? Servo1_FeedbackOffset { get { return Get<float?>(SettingsID.Servo1_FeedbackOffset); } set { Set(SettingsID.Servo1_FeedbackOffset, value); } }
        public int? Servo1_MaxSpeed { get { return Get<int?>(SettingsID.Servo1_MaxSpeed); } set { Set(SettingsID.Servo1_MaxSpeed, value); } }
        public int? Servo2_MinRange { get { return Get<int?>(SettingsID.Servo2_MinRange); } set { Set(SettingsID.Servo2_MinRange, value); } }
        public int? Servo2_MaxRange { get { return Get<int?>(SettingsID.Servo2_MaxRange); } set { Set(SettingsID.Servo2_MaxRange, value); } }
        public float? Servo2_ServoLinearization { get { return Get<float?>(SettingsID.Servo2_ServoLinearization); } set { Set(SettingsID.Servo2_ServoLinearization, value); } }
        public float? Servo2_ServoOffset { get { return Get<float?>(SettingsID.Servo2_ServoOffset); } set { Set(SettingsID.Servo2_ServoOffset, value); } }
        public float? Servo2_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo2_FeedbackLinearization); } set { Set(SettingsID.Servo2_FeedbackLinearization, value); } }
        public float? Servo2_FeedbackOffset { get { return Get<float?>(SettingsID.Servo2_FeedbackOffset); } set { Set(SettingsID.Servo2_FeedbackOffset, value); } }
        public int? Servo2_MaxSpeed { get { return Get<int?>(SettingsID.Servo2_MaxSpeed); } set { Set(SettingsID.Servo2_MaxSpeed, value); } }
        public int? Servo3_MinRange { get { return Get<int?>(SettingsID.Servo3_MinRange); } set { Set(SettingsID.Servo3_MinRange, value); } }
        public int? Servo3_MaxRange { get { return Get<int?>(SettingsID.Servo3_MaxRange); } set { Set(SettingsID.Servo3_MaxRange, value); } }
        public float? Servo3_ServoLinearization { get { return Get<float?>(SettingsID.Servo3_ServoLinearization); } set { Set(SettingsID.Servo3_ServoLinearization, value); } }
        public float? Servo3_ServoOffset { get { return Get<float?>(SettingsID.Servo3_ServoOffset); } set { Set(SettingsID.Servo3_ServoOffset, value); } }
        public float? Servo3_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo3_FeedbackLinearization); } set { Set(SettingsID.Servo3_FeedbackLinearization, value); } }
        public float? Servo3_FeedbackOffset { get { return Get<float?>(SettingsID.Servo3_FeedbackOffset); } set { Set(SettingsID.Servo3_FeedbackOffset, value); } }
        public int? Servo3_MaxSpeed { get { return Get<int?>(SettingsID.Servo3_MaxSpeed); } set { Set(SettingsID.Servo3_MaxSpeed, value); } }
        public int? Servo4_MinRange { get { return Get<int?>(SettingsID.Servo4_MinRange); } set { Set(SettingsID.Servo4_MinRange, value); } }
        public int? Servo4_MaxRange { get { return Get<int?>(SettingsID.Servo4_MaxRange); } set { Set(SettingsID.Servo4_MaxRange, value); } }
        public float? Servo4_ServoLinearization { get { return Get<float?>(SettingsID.Servo4_ServoLinearization); } set { Set(SettingsID.Servo4_ServoLinearization, value); } }
        public float? Servo4_ServoOffset { get { return Get<float?>(SettingsID.Servo4_ServoOffset); } set { Set(SettingsID.Servo4_ServoOffset, value); } }
        public float? Servo4_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo4_FeedbackLinearization); } set { Set(SettingsID.Servo4_FeedbackLinearization, value); } }
        public float? Servo4_FeedbackOffset { get { return Get<float?>(SettingsID.Servo4_FeedbackOffset); } set { Set(SettingsID.Servo4_FeedbackOffset, value); } }
        public int? Servo4_MaxSpeed { get { return Get<int?>(SettingsID.Servo4_MaxSpeed); } set { Set(SettingsID.Servo4_MaxSpeed, value); } }
        public int? Servo5_MinRange { get { return Get<int?>(SettingsID.Servo5_MinRange); } set { Set(SettingsID.Servo5_MinRange, value); } }
        public int? Servo5_MaxRange { get { return Get<int?>(SettingsID.Servo5_MaxRange); } set { Set(SettingsID.Servo5_MaxRange, value); } }
        public float? Servo5_ServoLinearization { get { return Get<float?>(SettingsID.Servo5_ServoLinearization); } set { Set(SettingsID.Servo5_ServoLinearization, value); } }
        public float? Servo5_ServoOffset { get { return Get<float?>(SettingsID.Servo5_ServoOffset); } set { Set(SettingsID.Servo5_ServoOffset, value); } }
        public float? Servo5_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo5_FeedbackLinearization); } set { Set(SettingsID.Servo5_FeedbackLinearization, value); } }
        public float? Servo5_FeedbackOffset { get { return Get<float?>(SettingsID.Servo5_FeedbackOffset); } set { Set(SettingsID.Servo5_FeedbackOffset, value); } }
        public int? Servo5_MaxSpeed { get { return Get<int?>(SettingsID.Servo5_MaxSpeed); } set { Set(SettingsID.Servo5_MaxSpeed, value); } }
        public int? Servo6_MinRange { get { return Get<int?>(SettingsID.Servo6_MinRange); } set { Set(SettingsID.Servo6_MinRange, value); } }
        public int? Servo6_MaxRange { get { return Get<int?>(SettingsID.Servo6_MaxRange); } set { Set(SettingsID.Servo6_MaxRange, value); } }
        public float? Servo6_ServoLinearization { get { return Get<float?>(SettingsID.Servo6_ServoLinearization); } set { Set(SettingsID.Servo6_ServoLinearization, value); } }
        public float? Servo6_ServoOffset { get { return Get<float?>(SettingsID.Servo6_ServoOffset); } set { Set(SettingsID.Servo6_ServoOffset, value); } }
        public float? Servo6_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo6_FeedbackLinearization); } set { Set(SettingsID.Servo6_FeedbackLinearization, value); } }
        public float? Servo6_FeedbackOffset { get { return Get<float?>(SettingsID.Servo6_FeedbackOffset); } set { Set(SettingsID.Servo6_FeedbackOffset, value); } }
        public int? Servo6_MaxSpeed { get { return Get<int?>(SettingsID.Servo6_MaxSpeed); } set { Set(SettingsID.Servo6_MaxSpeed, value); } }
        public int? Servo7_MinRange { get { return Get<int?>(SettingsID.Servo7_MinRange); } set { Set(SettingsID.Servo7_MinRange, value); } }
        public int? Servo7_MaxRange { get { return Get<int?>(SettingsID.Servo7_MaxRange); } set { Set(SettingsID.Servo7_MaxRange, value); } }
        public float? Servo7_ServoLinearization { get { return Get<float?>(SettingsID.Servo7_ServoLinearization); } set { Set(SettingsID.Servo7_ServoLinearization, value); } }
        public float? Servo7_ServoOffset { get { return Get<float?>(SettingsID.Servo7_ServoOffset); } set { Set(SettingsID.Servo7_ServoOffset, value); } }
        public float? Servo7_FeedbackLinearization { get { return Get<float?>(SettingsID.Servo7_FeedbackLinearization); } set { Set(SettingsID.Servo7_FeedbackLinearization, value); } }
        public float? Servo7_FeedbackOffset { get { return Get<float?>(SettingsID.Servo7_FeedbackOffset); } set { Set(SettingsID.Servo7_FeedbackOffset, value); } }
        public int? Servo7_MaxSpeed { get { return Get<int?>(SettingsID.Servo7_MaxSpeed); } set { Set(SettingsID.Servo7_MaxSpeed, value); } }
		/* End of Code-Generation */

        /// <summary>
        /// FirmwareVersion property in Verson format
        /// </summary>
        public Version Version
        {
            get
            {
                var version = new Version();
                Version.TryParse(FirmwareVersion, out version);
                return version;
            }
        }

        T Get<T>(SettingsID settingID)
        {
            var id = (int)settingID;
            object value;
            if (_settings.TryGetValue(id, out value))
            {
                return (T)value;
            }
            return default(T);
        }

        void Set<T>(SettingsID settingID, T value)
        {
            //validate
            var oldValue = Get<T>(settingID);
            bool hasChanged = !EqualityComparer<T>.Default.Equals(value, oldValue);
            if (!hasChanged)
                return;
            if (IsReadOnly(settingID))
                return;

            //set 
            var id = (int)settingID;
            if (_settings.ContainsKey(id))
                _settings[id] = value;
            else
                _settings.Add(id, value);

            //send event
            SettingChanged?.Invoke(this, new SettingChangedEventArgs() { OldValue = oldValue, NewValue = value, WriteCommand = new SettingWriteCommand() { SettingID = (int)settingID, Value = ParamerterConverter.SerializeValue(value, true) } });

        }

        void SetValue(SettingsID settingID, string value)
        {
            //validate
            if (IsReadOnly(settingID))
                return;

            //get type
            var dataType = (settingID.GetType().GetMember(settingID.ToString())[0].GetCustomAttributes(typeof(DataTypeAttribute), false)[0] as DataTypeAttribute).Type;

            //convert
            var convertedValue = ParamerterConverter.ChangeType(value, dataType);

            //set value
            var id = (int)settingID;
            if (_settings.ContainsKey(id))
                _settings[id] = convertedValue;
            else
                _settings.Add(id, convertedValue);
        }

        string Truncate(string value)
        {
            //truncate strings
            if (value != null && value.Length > CommandBuilder.MaxSettingStringLength)
                value = value.Substring(0, CommandBuilder.MaxSettingStringLength);
            return value;
        }

        public bool Set(SettingReadResponse setting)
        {
            //validate
            if (setting == null)
                return false;

            SetValue((SettingsID)setting.SettingID, setting.Value);

            //return true when all settings have values
            return _settings.Count >= _totalSettingCount;
        }

        public object Get(SettingsID settingID)
        {
            var id = (int)settingID;
            object value;
            if (_settings.TryGetValue(id, out value))
            {
                return value;
            }
            return null;
        }

        public SettingWriteCommand[] GetWriteCommands()
        {
            return (from s in _settings
                    select new SettingWriteCommand() { SettingID = s.Key, Value = ParamerterConverter.SerializeValue(s.Value, true) }).ToArray();
        }

        public virtual void Merge(Settings settings)
        {
            //validate
            if (settings == null)
                return;

            foreach (var mergeKey in settings._settings)
            {
                if (!IsReadOnly((SettingsID)mergeKey.Key))
                {
                    if (_settings.ContainsKey(mergeKey.Key))
                        _settings[mergeKey.Key] = mergeKey.Value;
                    else
                        _settings.Add(mergeKey.Key, mergeKey.Value);
                }
            }
        }

        bool IsReadOnly(SettingsID settingID)
        {
            return ReadOnlySettings.Exists(i=> i == settingID);
        }

        public void SetServoCalibration(int servoID, ServoCalibration servoCalibration)
        {
            Set<int?>(GetServoSettingID(servoID, "MinRange"), servoCalibration.MinRange);
            Set<int?>(GetServoSettingID(servoID, "MaxRange"), servoCalibration.MaxRange);
            Set<float?>(GetServoSettingID(servoID, "ServoLinearization"), servoCalibration.ServoLinearization);
            Set<float?>(GetServoSettingID(servoID, "ServoOffset"), servoCalibration.ServoOffset);
            Set<float?>(GetServoSettingID(servoID, "FeedbackLinearization"), servoCalibration.FeedbackLinearization);
            Set<float?>(GetServoSettingID(servoID, "FeedbackOffset"), servoCalibration.FeedbackOffset);
            Set<int?>(GetServoSettingID(servoID, "MaxSpeed"), servoCalibration.MaxSpeed);
        }

        public ServoCalibration GetServoCalibration(int servoID)
        {
            var result = new ServoCalibration();
            result.MinRange = Get<int?>(GetServoSettingID(servoID, "MinRange")).GetValueOrDefault();
            result.MaxRange = Get<int?>(GetServoSettingID(servoID, "MaxRange")).GetValueOrDefault();
            result.ServoLinearization = Get<float?>(GetServoSettingID(servoID, "ServoLinearization")).GetValueOrDefault();
            result.ServoOffset = Get<float?>(GetServoSettingID(servoID, "ServoOffset")).GetValueOrDefault();
            result.FeedbackLinearization = Get<float?>(GetServoSettingID(servoID, "FeedbackLinearization")).GetValueOrDefault();
            result.FeedbackOffset = Get<float?>(GetServoSettingID(servoID, "FeedbackOffset")).GetValueOrDefault();
            result.MaxSpeed = Get<int?>(GetServoSettingID(servoID, "MaxSpeed")).GetValueOrDefault();

            return result;
        }

        SettingsID GetServoSettingID(int servoID, string settingBaseName)
        {
            return (SettingsID)Enum.Parse(typeof(SettingsID), $"Servo{servoID}_{settingBaseName}"); //intended to throw an exception if it fails
        }
    }



    public enum SettingsID : int
    {
		/* this section is Auto-Generated using a custom tool and is automaticly updated in VS.NET
		 * customTool : EEPROM Index
		 * do not modify */
		[DataType(typeof(string))]
        ModelNumber = 1,
        [DataType(typeof(string))]
        FirmwareVersion = 2,
        [DataType(typeof(string))]
        SerialNumber = 3,
        [DataType(typeof(string))]
        NickName = 4,
        [DataType(typeof(int?))]
        ActiveServos = 5,
        [DataType(typeof(int?))]
        Calibrated = 6,
        [DataType(typeof(int?))]
        Servo1_MinRange = 7,
        [DataType(typeof(int?))]
        Servo1_MaxRange = 8,
        [DataType(typeof(float?))]
        Servo1_ServoLinearization = 9,
        [DataType(typeof(float?))]
        Servo1_ServoOffset = 10,
        [DataType(typeof(float?))]
        Servo1_FeedbackLinearization = 11,
        [DataType(typeof(float?))]
        Servo1_FeedbackOffset = 12,
        [DataType(typeof(int?))]
        Servo1_MaxSpeed = 13,
        [DataType(typeof(int?))]
        Servo2_MinRange = 14,
        [DataType(typeof(int?))]
        Servo2_MaxRange = 15,
        [DataType(typeof(float?))]
        Servo2_ServoLinearization = 16,
        [DataType(typeof(float?))]
        Servo2_ServoOffset = 17,
        [DataType(typeof(float?))]
        Servo2_FeedbackLinearization = 18,
        [DataType(typeof(float?))]
        Servo2_FeedbackOffset = 19,
        [DataType(typeof(int?))]
        Servo2_MaxSpeed = 20,
        [DataType(typeof(int?))]
        Servo3_MinRange = 21,
        [DataType(typeof(int?))]
        Servo3_MaxRange = 22,
        [DataType(typeof(float?))]
        Servo3_ServoLinearization = 23,
        [DataType(typeof(float?))]
        Servo3_ServoOffset = 24,
        [DataType(typeof(float?))]
        Servo3_FeedbackLinearization = 25,
        [DataType(typeof(float?))]
        Servo3_FeedbackOffset = 26,
        [DataType(typeof(int?))]
        Servo3_MaxSpeed = 27,
        [DataType(typeof(int?))]
        Servo4_MinRange = 28,
        [DataType(typeof(int?))]
        Servo4_MaxRange = 29,
        [DataType(typeof(float?))]
        Servo4_ServoLinearization = 30,
        [DataType(typeof(float?))]
        Servo4_ServoOffset = 31,
        [DataType(typeof(float?))]
        Servo4_FeedbackLinearization = 32,
        [DataType(typeof(float?))]
        Servo4_FeedbackOffset = 33,
        [DataType(typeof(int?))]
        Servo4_MaxSpeed = 34,
        [DataType(typeof(int?))]
        Servo5_MinRange = 35,
        [DataType(typeof(int?))]
        Servo5_MaxRange = 36,
        [DataType(typeof(float?))]
        Servo5_ServoLinearization = 37,
        [DataType(typeof(float?))]
        Servo5_ServoOffset = 38,
        [DataType(typeof(float?))]
        Servo5_FeedbackLinearization = 39,
        [DataType(typeof(float?))]
        Servo5_FeedbackOffset = 40,
        [DataType(typeof(int?))]
        Servo5_MaxSpeed = 41,
        [DataType(typeof(int?))]
        Servo6_MinRange = 42,
        [DataType(typeof(int?))]
        Servo6_MaxRange = 43,
        [DataType(typeof(float?))]
        Servo6_ServoLinearization = 44,
        [DataType(typeof(float?))]
        Servo6_ServoOffset = 45,
        [DataType(typeof(float?))]
        Servo6_FeedbackLinearization = 46,
        [DataType(typeof(float?))]
        Servo6_FeedbackOffset = 47,
        [DataType(typeof(int?))]
        Servo6_MaxSpeed = 48,
        [DataType(typeof(int?))]
        Servo7_MinRange = 49,
        [DataType(typeof(int?))]
        Servo7_MaxRange = 50,
        [DataType(typeof(float?))]
        Servo7_ServoLinearization = 51,
        [DataType(typeof(float?))]
        Servo7_ServoOffset = 52,
        [DataType(typeof(float?))]
        Servo7_FeedbackLinearization = 53,
        [DataType(typeof(float?))]
        Servo7_FeedbackOffset = 54,
        [DataType(typeof(int?))]
        Servo7_MaxSpeed = 55,
		/* End of Code-Generation */
	}

	[System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class DataTypeAttribute : Attribute
    {
        public Type Type { get; }

        public DataTypeAttribute(Type type)
        {
            Type = type;
        }
    }

    public class SettingChangedEventArgs : EventArgs
    {
        public SettingWriteCommand WriteCommand { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }
}
