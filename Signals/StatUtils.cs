using EMILtools.Signals;
using UnityEngine;
using static EMILtools.Signals.StatTags;

public static class StatUtils 
{
    public static Stat<float, TTag> Clamp<TTag>(Stat<float, TTag> stat, float min, float max) 
        where TTag: struct, IStatTag
    {
        stat.Value = Mathf.Clamp(stat.Value, min, max);
        return stat;
    }
}
