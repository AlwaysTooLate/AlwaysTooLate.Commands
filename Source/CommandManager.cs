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
        public bool ColoredFindOutput = true;
        private readonly List<Command> _commands = new List<Command>();

        /// <summary>
        ///     Gets all commands.
        /// </summary>
        /// <returns>The command list.</returns>
        public static IReadOnlyList<Command> Commands => Instance._commands;

        protected override void OnAwake()
        {
            base.OnAwake();
            RegisterCommand("help", "Prints all registered commands", Commands.ForEach(command => Debug.Log($"{command.Name}: {command.Description}")));
            RegisterCommand("find", "Looks for commands that have the given string in its name or description.", HelpCommand);
            RegisterCommand("print", "Prints given string to the log.", Debug.Log);
            RegisterCommand("warning", "Prints given warning string to the log.", Debug.LogWarning);
            RegisterCommand("error", "Prints given error string to the log.", Debug.LogError);
            RegisterCommand("quit", "Exits the game.", () =>
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        internal void RegisterFunction(string functionName, string functionDescription, object instance, MethodInfo method)
        {
            functionName.Replace(" ", "_");
            if (_commands.Any(x => x.Name == functionName && x.Parameters.Length == parameters.Length))
                Debug.LogWarning($"Command with this name({functionName}) and the same parameters count already exists.");
            else
                _commands.Add(new Command(functionName, functionDescription, instance, method));
        }

        private void HelpCommand(string str)
        {
            var sb = new StringBuilder();
            var list = new List<string>();

            // Find commands
            var commands = Commands.Where(x => x.Name.Contains(str) || x.Description.Contains(str));

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
            list.ForEach(Debug.Log);
        }

        private static object ParseObject(string value, TypeCode type)
        {
            value.ToLower();
            if (type == TypeCode.DBNull || type == TypeCode.Empty)
                return null;
            if (type == TypeCode.String || type == TypeCode.Object)
                return value;
            try
            {
                if (type == TypeCode.Boolean)
                {
                    if (value == "yes" || value == "1" || value == "on")
                        return true;
                    if (value == "no" || value == "0" || value == "off")
                        return false;
                }
                return Convert.ChangeType(value, type);
            }
            catch (Exception)
            {
                Debug.LogError($"Invalid parameter type were given for '{value}' expected type of '{type}'.");
            }
            return null;
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
        public static bool Execute(string commandString)
        {

            var arguments = CommandParser.ParseCommand(commandString, out var commandName);

            var error = CommandParser.ValidateCommand(commandName);

            if (error.Length > 0)
            {
                Debug.Log($"Invalid command syntax. {error}");
                return false;
            }

            

            // Find proper command
            var commands = Instance._commands.Where(x => x.Name == commandName).ToArray();

            if(command.Length == 0)
            {
                //Syntax check
                var error = CommandParser.ValidateCommand(commandName);

                if (error.Length > 0)
                {
                    Debug.Log($"Invalid command syntax. {error}");
                    return false;
                }
                // CVar integration (read value)
                if (arguments.Count == 0)
                {
                    var variable = CVarManager.GetVariable(commandName);
                    if (variable != null)
                    {
                        Debug.Log(
                            $"{commandName} {variable.GetValue().ToString().ToLower()} (default: {variable.Attribute.DefaultValue.ToString().ToLower()})");
                        return false;
                    }

                    Debug.LogError($"Unknown command '{commandName}'");
                    return false;
                }

                // CVar integration (write value)
                if (arguments.Count == 1)
                {
                    var variable = CVarManager.GetVariable(commandName);
                    if (variable != null)
                    {
                        var value = ParseObject(arguments.First(), variable.Field.FieldType.GetTypeCode());

                        if (value != null)
                            // Set variable data
                            variable.SetValue(value);
                        return;
                    }

                    Debug.LogError($"Unknown command '{commandName}'");
                    return;
                }
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
                Debug.LogError("'" + commandName + "' command exists, but invalid arguments were given." + arguments.Count);
                return;
            }

            // parse
            var cmdParams = command.Parameters;
            var paramIndex = -1;
            var parseParams = cmdParams.Select(parameter => ParseObject(parameter, cmdParams[paramIndex++].ParameterType.Name.GetTypeCode()));

            // execute!
            command.Method.Invoke(command.MethodTarget, parseParams.ToArray());
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
        ///     Unregisters command.
        /// </summary>
        /// <param command="command">Command instance</param>
        public static void UnregisterCommand(Command command)
        {
            Instance._commands.Remove(command);
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
        public static void RegisterCommand<T1, T2, T3, T4>(string name, string description, Action<T1, T2, T3, T4> action)
        {
            Instance.RegisterFunction(name, description, action.Target, action.Method);
        }

        /// <summary>
        ///     Gets all commands.
        /// </summary>
        /// <returns>The command list.</returns>
        [Obsolete("'GetCommands' method is deprecated, please use 'Command' property instead.")]
        public static IReadOnlyList<Command> GetCommands() => Instance._commands;
    }
}