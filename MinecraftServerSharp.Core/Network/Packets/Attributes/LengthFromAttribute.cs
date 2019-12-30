using System;

namespace MinecraftServerSharp.Network.Packets
{
    /// <summary>
    /// Used to dictate the length of the marked parameter by looking at a previous parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class LengthFromAttribute : Attribute
    {
        public int RelativeIndex { get; }

        public LengthFromAttribute(int relativeIndex)
        {
            if (relativeIndex >= 0)
                throw new ArgumentOutOfRangeException(nameof(relativeIndex), "Value must be below zero.");
            RelativeIndex = relativeIndex;
        }
    }
}