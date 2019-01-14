// AlwaysTooLate.Core (c) 2018-2019 Always Too Late. All rights reserved.

using AlwaysTooLate.Core;

namespace AlwaysTooLate.Commands
{
    /// <summary>
    /// Command manager class.
    /// Should be initialized on main (entry) scene.
    /// </summary>
    public class CommandManager : BehaviourSingleton<CommandManager>
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            // TODO: Register default commands (exec, print etc.)
        }

        public static void Execute(string command)
        {
            // Commands
            // >exec somefile.txt
            // >print "Hello, World!"
            // >somefunction 2 2.0 "test" true false on off

            // CVar integration
            // >settings.cheats.fly true
            // >settings.cheats.fly on
            // >settings.cheats.fly 1
            // >settings.cheats.fly
            // on (default: off)

            // TODO: Execute command
        }

        /*public static void Register(string name)
        {
        
        }*/
    }
}
