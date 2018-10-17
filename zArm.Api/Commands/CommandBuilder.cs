using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{
    public static class CommandBuilder
    {
        static Dictionary<int, CommandInfo> _commandsInfo;
        static Dictionary<int, CommandInfo> _responseInfo;

        public static char EndChar = '\n';
        public static char ParamSeporatorChar = ' ';
        public static char EscapeChar = '"';
        public static int MaxServos = 7;
        public static string PingMessage = "connected_to_zArm";
        public static int MaxSettingStringLength = 20;
        public static float MaxServoPosition = 360f;
        public static float MinServoPosition = -180f;
        public static string DefaultValueWhenMissingParam = "?";
        public static int MaxSendSpeed = 18; //in miliseconds

        static CommandBuilder()
        {
            //init the commandsInfo, used for parsing and serialization
            _commandsInfo = CreateCommandInfos<Command>();
            _responseInfo = CreateCommandInfos<Response>();
        }

        static Dictionary<int, CommandInfo> CreateCommandInfos<T>()
            where T : Message
        {
            var result = new Dictionary<int, CommandInfo>();
            var commandTypes = typeof(T).Assembly.GetTypes()
                .Where(i => i.IsSubclassOf(typeof(T)) && !i.IsAbstract)
                .Select(i => i);
            foreach (var commandType in commandTypes)
            {
                var orderedProperties = (from property in commandType.GetProperties()
                                         let orderAttribute = Attribute.GetCustomAttribute(property, typeof(OrderAttribute), true) as OrderAttribute
                                         where (orderAttribute != null)
                                         orderby orderAttribute?.Order ?? 0
                                         let requiredAttribute = Attribute.GetCustomAttribute(property, typeof(RequiredAttribute), true) as RequiredAttribute
                                         select new CommandPropertyInfo() { Property = property, HasDefaultValue = requiredAttribute != null, RequiresServoID = (requiredAttribute == null) ? null : requiredAttribute.ServoID }).ToArray();

                var fastCommandAttribute = Attribute.GetCustomAttribute(commandType, typeof(FastCommandAttribute), false) as FastCommandAttribute;
                var commandInfo = new CommandInfo() { ID = ((T)Activator.CreateInstance(commandType)).ID, CommandType = commandType, Properties = orderedProperties, ServoCount = commandType.GetField("ServoCount"), FastCommand = fastCommandAttribute != null };
                result.Add(commandInfo.ID, commandInfo);
            }
            return result;
        }

        public static Command[] ParseCommands(string commands, int? servoCount)
        {
            return Parse<Command>(commands, _commandsInfo, servoCount);
        }

        public static Response[] ParseResponses(string responses)
        {
            return Parse<Response>(responses, _responseInfo);
        }

        static T[] Parse<T>(string data, Dictionary<int, CommandInfo> infos, int? servoCount = null)
        {
            //validate
            if (string.IsNullOrWhiteSpace(data))
                return null;

            //parse the response
            var result = new List<T>();
            var responseStrings = data.Split(EndChar);
            foreach (var responseString in responseStrings)
            {
                var parameters = SplitParameters(responseString);

                //ignore a blank command
                if (parameters.Length == 0 || parameters[0].Length == 0)
                    continue;

                //get the type
                int id;
                if (!int.TryParse(parameters[0], out id))
                    continue;
                CommandInfo info = null;
                if (!infos.TryGetValue(id, out info))
                    continue;

                //construct the response
                try
                {
                    var response = (T)Activator.CreateInstance(info.CommandType);
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        if (i < info.Properties.Length)
                        {
                            var propertyInfo = info.Properties[i];

                            //skip param if servo is missing
                            if (propertyInfo.RequiresServoID != null && servoCount != null && propertyInfo.RequiresServoID.Value > servoCount.Value)
                                continue;

                            propertyInfo.Property.SetValue(response, ParamerterConverter.ChangeType(parameters[i], info.Properties[i].Property.PropertyType, propertyInfo.HasDefaultValue));
                        }
                    }

                    //add it to the results
                    result.Add(response);
                }
                catch
                {
                    //ignore parsing errors for now
                }
            }

            return result.ToArray();
        }

        static string[] SplitParameters(string responseString)
        {
            var parameters = new List<string>();
            var escapes = responseString.Split(EscapeChar);
            for (int i = 0; i < escapes.Length; i++)
            {
                //even numbers need further splitting
                if (i % 2 == 0)
                {
                    var seporators = escapes[i].Split(ParamSeporatorChar);
                    foreach (var seporator in seporators)
                    {
                        if (!string.IsNullOrEmpty(seporator))
                            parameters.Add(seporator);
                    }
                }
                else
                {
                    //add escaped parameter
                    if (!string.IsNullOrEmpty(escapes[i]))
                        parameters.Add(escapes[i]);
                }
            }

            return parameters.ToArray();
        }

        static IEnumerable<string> Split(this string input,
                                        string separator,
                                        char escapeCharacter)
        {
            int startOfSegment = 0;
            int index = 0;
            while (index < input.Length)
            {
                index = input.IndexOf(separator, index);
                if (index > 0 && input[index - 1] == escapeCharacter)
                {
                    index += separator.Length;
                    continue;
                }
                if (index == -1)
                {
                    break;
                }
                yield return input.Substring(startOfSegment, index - startOfSegment);
                index += separator.Length;
                startOfSegment = index;
            }
            yield return input.Substring(startOfSegment);
        }

        public static SerializedCommands SerializeCommands(params Command[] commands)
        {
            return Serialize(commands, _commandsInfo);
        }

        public static SerializedCommands SerializeResponses(params Response[] responses)
        {
            return Serialize(responses, _responseInfo);
        }

        static SerializedCommands Serialize(Message[] messages, Dictionary<int, CommandInfo> infos)
        {
            //validate
            if (messages == null || messages.Length == 0)
                return new SerializedCommands();

            //serialize the command and add to the string
            StringBuilder sb = new StringBuilder();
            bool fastCommand = false;
            foreach (var message in messages)
            {
                CommandInfo commandInfo = null;
                if (infos.TryGetValue(message.ID, out commandInfo))
                {
                    if (commandInfo.FastCommand)
                        fastCommand = true;
                    bool paramOmitted = false;
                    int? servoCount = (commandInfo.ServoCount != null) ? (int?)commandInfo.ServoCount.GetValue(message) : null;
                    foreach (var propertyInfo in commandInfo.Properties)
                    {
                        //skip param if servo is missing
                        if (propertyInfo.RequiresServoID != null && servoCount != null && propertyInfo.RequiresServoID.Value > servoCount.Value)
                            continue;

                        var parm = propertyInfo.Property.GetValue(message);
                        if (parm == null && !propertyInfo.HasDefaultValue)
                            paramOmitted = true;
                        else if (paramOmitted)
                            throw new CommandException($"{propertyInfo.Property.Name} must be omitted when a previous parameter in the command is omitted");

                        sb.Append(ParamerterConverter.SerializeValue(parm, false, propertyInfo.HasDefaultValue));
                        if (commandInfo.Properties.Last() != propertyInfo)
                            sb.Append(ParamSeporatorChar);
                    }
                    sb.Append(EndChar);
                }
            }

            //return the commands
            return new SerializedCommands() { Commands = sb.ToString(), FastCommand = fastCommand };
        }

        class CommandInfo
        {
            public Type CommandType;
            public CommandPropertyInfo[] Properties;
            public int ID;
            public FieldInfo ServoCount;
            public bool FastCommand;
        }

        class CommandPropertyInfo
        {
            public PropertyInfo Property;
            public bool HasDefaultValue;
            public int? RequiresServoID;
        }

        
    }




    public struct SerializedCommands
    {
        public string Commands;
        public bool FastCommand;
    }
    
}
