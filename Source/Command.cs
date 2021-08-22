using System.Reflection;

namespace AlwaysTooLate.Commands
{
    /// <summary>
    ///     Structure for command data.
    /// </summary>
    public sealed class Command
    {
        public readonly string Name;
        public readonly string Description;
        public readonly ParameterInfo[] Parameters;
        public readonly object MethodTarget;
        public readonly MethodInfo Method;

        internal Command(string name, string description, object methodTarget, MethodInfo method)
        {
            this.Name = name;
            this.Description = description;
            this.Parameters = method.GetParameters();
            this.MethodTarget = method; 
            this.Method = method;
        }
    }
}