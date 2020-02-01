// AlwaysTooLate.Commands (c) 2018-2019 Always Too Late.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AlwaysTooLate.Core;
using AlwaysTooLate.CVars;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlwaysTooLate.Commands
{
    /// <summary>
    ///     Command manager class.
    ///     Should be initialized on main (entry) scene.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class CommandManager : BehaviourSingleton<CommandManager>
    {
        private readonly List<Command> _commands = new List<Command>();

        public bool ColoredFindOutput = true;

        protected override void OnAwake()
        {
            base.OnAwake();

            RegisterCommand("help", "Prints all registered commands", () =>
            {
                foreach (var command in GetCommands()) Debug.Log($"{command.Name}: {command.Description}");
            });

            RegisterCommand("find", "Looks for commands that have the given string in its name or description.",
                (string str) =>
                {
                    var sb = new StringBuilder();
                    var list = new List<string>();

                    // Find commands
                    var commands = GetCommands().Where(x => x.Name.Contains(str) || x.Description.Contains(str));

                    // Build list
                    foreach (var command in commands)
                        if (ColoredFindOutput)
                        {
                            // Insert background color
                            sb.Append(RichTextExtensions.ColorInnerString(command.Name, str, "green"));
                            sb.Append(": ");
                            sb.Append(RichTextExtensions.ColorInnerString(command.Description, str, "green"));

                            list.Add(sb.ToString());
                            sb.Clear();
                        }
                        else
                        {
                            list.Add($"{command.Name}: {command.Description}");
                        }

                    // Find config variables
                    var variables = CVarManager.Instance.AllVariables.Where(x =>
                        x.Key.Contains(str) || x.Value.Attribute.Description.Contains(str));

                    // Build list
                    foreach (var variable in variables)
                        if (ColoredFindOutput)
                        {
                            // Insert background color
                            sb.Append(RichTextExtensions.ColorInnerString(variable.Key, str, "green"));
                            sb.Append(": ");
                            sb.Append(RichTextExtensions.ColorInnerString(variable.Value.Attribute.Description, str,
                                "green"));

                            list.Add(sb.ToString());
                            sb.Clear();
                        }
                        else
                        {
                            list.Add($"{variable.Key}: {variable.Value.Attribute.Description}");
                        }

                    // Sort list
                    list.Sort();

                    // Draw all matching commands/cvars
                    foreach (var item in list) Debug.Log(item);
                });

            RegisterCommand("print", "Prints given string to the log.", (string str) => { Debug.Log(str); });

            RegisterCommand("warning", "Prints given warning string to the log.",
                (string str) => { Debug.LogWarning(str); });

            RegisterCommand("error", "Prints given error string to the log.", (string str) => { Debug.LogError(str); });

            RegisterCommand("quit", "Exits the game.", () =>
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        internal void RegisterFunction(string functionName, string functionDescription, object instance,
            MethodInfo method)
        {
            var parameters = method.GetParameters();

            // check commands, do not allow duplicates!
            var commands = _commands.Where(x => x.Name == functionName && x.Parameters.Length == parameters.Length)
                .ToArray();
            if (commands.Length != 0)
            {
                Debug.LogWarning("Command with this name(" + functionName +
                                 ") and the same parameters count already exists.");
                return;
            }

            // register command
            _commands.Add(new Command
            {
                Name = functionName,
                Description = functionDescription,
                Parameters = parameters,
                MethodTarget = instance,
                Method = method
            });
        }

        /// <summary>
        ///     Gets all commands.
        /// </summary>
        /// <returns>The command list.</returns>
        public static IReadOnlyList<Command> GetCommands()
        {
            return Instance._commands;
        }

        /// <summary>
        ///     Executes given command string.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        /// <example>
        ///     print "Hello, World!"
        ///     some_function 2 2.0 'test' true on off
        /// </example>
        /// <example>
        ///     cheats.fly true
        ///     cheats.fly on
        ///     cheats.fly 1
        ///     cheats.fly
        /// </example>
        public static void Execute(string commandString)
        {
            var error = CommandParser.ValidateCommand(commandString);

            if (error.Length > 0)
            {
                Debug.Log($"Invalid command syntax. {error}");
                return;
            }

            var arguments = CommandParser.ParseCommand(commandString, out var commandName);

            // Find proper command
            var commands = Instance._commands.Where(x => x.Name == commandName).ToArray();

            // CVar integration (read value)
            if (commands.Length == 0 && arguments.Count == 0)
            {
                var variable = CVarManager.GetVariable(commandName);
                if (variable != null)
                {
                    Debug.Log(
                        $"{commandName} {variable.GetValue().ToString().ToLower()} (default: {variable.Attribute.DefaultValue.ToString().ToLower()})");
                    return;
                }

                Debug.LogError($"Unknown command '{commandName}'");
                return;
            }

            // CVar integration (write value)
            if (commands.Length == 0 && arguments.Count == 1)
            {
                var variable = CVarManager.GetVariable(commandName);
                if (variable != null)
                {
                    var value = ParseObject(arguments.First(), variable.Field.FieldType.Name.ToLower());

                    if (value != null)
                        // Set variable data
                        variable.SetValue(value);
                    return;
                }

                Debug.LogError($"Unknown command '{commandName}'");
                return;
            }

            var found = false;
            Command command = null;
            foreach (var cmd in commands)
            {
                if (cmd.Parameters.Length != arguments.Count)
                    continue;

                found = true;
                command = cmd;
                break;
            }

            if (!found)
            {
                Debug.LogError("'" + commandName + "' command exists, but invalid arguments were given." +
                               arguments.Count);
                return;
            }

            // parse
            var cmdParams = command.Parameters;
            var paramIndex = 0;
            var parseParams = new object[arguments.Count];

            foreach (var parameter in arguments)
            {
                var cmdParameter = cmdParams[paramIndex];

                parseParams[paramIndex] = ParseObject(parameter, cmdParameter.ParameterType.Name.ToLower());

                paramIndex++;
            }

            // execute!
            command.Method.Invoke(command.MethodTarget, parseParams);
        }

        private static object ParseObject(string value, string type)
        {
            switch (type)
            {
                case "string":
                    // string does not need any type check
                    return value;
                case "int32":
                    if (int.TryParse(value, out var resultInt))
                    {
                        return resultInt;
                    }
                    else
                    {
                        Debug.LogError($"invalid parameter type were given for '{value}' expected type of '{type}'.");
                        return null;
                    }

                case "single":
                    if (float.TryParse(value, out var resultSingle))
                    {
                        return resultSingle;
                    }
                    else
                    {
                        Debug.LogError($"invalid parameter type were given for '{value}' expected type of '{type}'.");
                        return null;
                    }

                case "double":
                    if (double.TryParse(value, out var resultDouble))
                    {
                        return resultDouble;
                    }
                    else
                    {
                        Debug.LogError($"invalid parameter type were given for '{value}' expected type of '{type}'.");
                        return null;
                    }

                case "boolean":
                    if (bool.TryParse(value, out var resultBoolean))
                        return resultBoolean;
                    else
                        // Additional boolean thingies
                        switch (value)
                        {
                            case "1":
                            case "yes":
                            case "on":
                                return true;
                            case "0":
                            case "no":
                            case "off":
                                return false;
                            default:
                                Debug.LogError(
                                    $"invalid parameter type were given for '{value}' expected type of '{type}'.");
                                return null;
                        }

                default:
                    Debug.LogError(
                        $"Command target method has invalid type ({type} with value of {value}) in parameters!");
                    return null;
            }
        }

        /// <summary>
        ///     Unregisters all commands that have the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        public static void UnregisterCommand(string name)
        {
            Instance._commands.RemoveAll(x => x.Name == name);
        }

        /// <summary>
        ///     Registers command with specified name and execution method in given command group.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="description">(optional)The command description.</param>
        /// <param name="action">Called when command is being executed.</param>
        public static void RegisterCommand(string name, string description, Action action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Registers command with specified name and execution method in given command group.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="description">(optional)The command description.</param>
        /// <param name="action">Called when command is being executed.</param>
        public static void RegisterCommand<T1>(string name, string description, Action<T1> action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Registers command with specified name and execution method in given command group.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="description">(optional)The command description.</param>
        /// <param name="action">Called when command is being executed.</param>
        public static void RegisterCommand<T1, T2>(string name, string description, Action<T1, T2> action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Registers command with specified name and execution method in given command group.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="description">(optional)The command description.</param>
        /// <param name="action">Called when command is being executed.</param>
        public static void RegisterCommand<T1, T2, T3>(string name, string description, Action<T1, T2, T3> action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Registers command with specified name and execution method in given command group.
        /// </summary>
        /// <param name="name">The command name.</param>
        /// <param name="description">(optional)The command description.</param>
        /// <param name="action">Called when command is being executed.</param>
        public static void RegisterCommand<T1, T2, T3, T4>(string name, string description,
            Action<T1, T2, T3, T4> action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Structure for command data.
        /// </summary>
        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public ParameterInfo[] Parameters { get; set; }
            public object MethodTarget { get; set; }
            public MethodInfo Method { get; set; }
        }
    }
}