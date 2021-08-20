﻿// AlwaysTooLate.Commands (c) 2018-2019 Always Too Late.
using System.Linq;
using System.Collections.Generic;

namespace AlwaysTooLate.Commands
{
    internal static class CommandParser
    {
        private static float _validationLevel = 0.7f;

        /// <summary>
        ///     The percentage of command validation detection.
        ///     This value is between 0f (0%) and 1f (100%)
        /// </summary>
        public static float ValidationLevel 
        {
            get => _validationLevel;
            set => _validationLevel = value > 1f ? 1f : value < 0f ? 0f : value;
        }

        /// <summary>
        ///     Searches for similiar commands and if not found, returns the false
        /// </summary>
        /// <param name="command">The command string.</param>
        /// <param name="correction">The resulting correction of command.</param>
        /// <returns>Returns true if mistake is found.</returns>
        public static bool ValidateCommand(string command, out string correction)
        {  
            correction = string.Empty;
            if(_validationLevel == 0f)
                return false;
            command.Trim().ToLower();

            //Misspell check
            var names = CommandManager.Commands.Select(x => x.Name).ToArray();
            char[] command_chars = command.ToCharArray();
            int bestIndex;
            int bestPoints;
            for (int i = 0; i < names.Length; i++)
            {
                int points;
                char[] name_chars = names[i].ToLower().ToCharArray();
                for (int c = 0; c < command_chars.Length; c++)
                    if(name_chars[c] == command_chars[c])
                        points++;
                if(points < bestPoints)
                    continue;
                bestPoints = points;
                bestIndex = i;
            }
            if((float)bestPoints / command_chars.Length  <= _validationLevel)
            {
                correction = names[bestIndex];
                return true;
            }
            return false;
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
 
            //NOTE: Regex can be used to short this, but its slower ofc.
            foreach (var ch in command)
                if (ch == ' ' && !stringRead)
                {
                    // Next param
                    if (!string.IsNullOrEmpty(parameter))
                        parameters.Add(parameter);
                    parameter = string.Empty;
                }
                else if (ch == '\"' || ch == '\'')
                    stringRead = !stringRead;
                else
                    parameter += ch;

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