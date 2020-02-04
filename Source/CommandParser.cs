// AlwaysTooLate.Commands (c) 2018-2019 Always Too Late.

using System.Collections.Generic;

namespace AlwaysTooLate.Commands
{
    internal static class CommandParser
    {
        public static string ValidateCommand(string command)
        {
            // TODO: Check if command starts with name, all strings are closed, there is no any invalid characters etc.
            return "";
        }

        /// <summary>
        ///     Parse command to get name and arguments.
        ///     Strings are supported! eg.: 'print "Hello, World!"'
        /// </summary>
        /// <param name="command">The command string.</param>
        /// <param name="name">The resulting command name.</param>
        /// <returns>The list of command arguments.</returns>
        public static List<string> ParseCommand(string command, out string name)
        {
            // Trim
            command = command.Trim();

            name = "";

            var parameters = new List<string>();
            var parameter = "";
            var stringRead = false;

            foreach (var ch in command)
                if (ch == ' ' && !stringRead)
                {
                    // Next param

                    if (!string.IsNullOrEmpty(parameter))
                        parameters.Add(parameter);

                    parameter = string.Empty;
                }
                else if (ch == '\"' || ch == '\'')
                {
                    // Start or stop string param   
                    if (stringRead)
                    {
                        stringRead = false;
                        continue;
                    }

                    stringRead = true;
                }
                else
                {
                    // Add to current.
                    parameter += ch;
                }

            // Add last parameter
            if (!string.IsNullOrEmpty(parameter)) parameters.Add(parameter);

            // Set name
            if (string.IsNullOrEmpty(name) && parameters.Count > 0)
            {
                name = parameters[0];
                parameters.RemoveAt(0);
            }

            return parameters;
        }
    }
}