using UnityEngine;

namespace EMILtools.Signals
{
    public static class StatTags
    {
        public interface IStatTag { }
        public struct Speed : IStatTag { }
        public struct Health : IStatTag { }
        public struct JumpPower : IStatTag { }
        public struct Damage : IStatTag { }
    }
}

