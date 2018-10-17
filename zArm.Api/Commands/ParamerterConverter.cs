using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{
    public static class ParamerterConverter
    {
        public static T ChangeType<T>(string value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static object ChangeType(string value, Type convertionType, bool useDefaultValue = false)
        {
            var baseType = Nullable.GetUnderlyingType(convertionType) ?? convertionType;
            object convertedValue;

            //use default value
            if (useDefaultValue && value == CommandBuilder.DefaultValueWhenMissingParam)
                return null;

            //convert to int for enum
            if (baseType.IsEnum)
                convertedValue = Enum.Parse(baseType, value);

            //convert to bool
            else if (baseType == typeof(bool))
                convertedValue = value == "1";

            //convert empty string to null (if nullable type)
            else if (convertionType != baseType && string.IsNullOrEmpty(value))
                return null;

            else
                convertedValue = Convert.ChangeType(value, baseType);

            return NullablePackage(convertionType, baseType, convertedValue);
        }

        static object NullablePackage(Type convertionType, Type baseType, object baseValue)
        {
            if (convertionType == baseType)
                return baseValue;
            var value = Activator.CreateInstance(convertionType, baseValue);
            return value;
        }

        public static string SerializeValue(object value, bool longFloat = false, bool useDefaultValue = false)
        {
            //null
            if (value == null)
            {
                if (useDefaultValue)
                    return CommandBuilder.DefaultValueWhenMissingParam;
                else
                    return string.Empty;
            }

            //bool
            if (value is bool)
                return (bool)value ? "1" : "0";

            //float
            if (value is float)
                if (longFloat)
                    return string.Format("{0:0.#######}", value);
                else
                    return string.Format("{0:0.##}", value);

            //default
            return value.ToString();
        }
    }
}
