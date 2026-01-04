using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EMILtools.Timers;
using UnityEngine;
using static EMILtools.Signals.StatTags;

namespace EMILtools.Signals
{
    public static class ModifierStrategies
    {
        /// <summary>
        /// Only will apply the Modifiers w/out the Decors, Resolves the correct TMod
        /// </summary>
        /// <param name="type"></param>
        /// <param name="list"></param>
        /// <param name="val"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ResolveList<T>(Type type, object list, T val)
            where T: struct
        {
            Debug.Log("[ResolveList] Started");
            /// float fval = (float)(object)val;
            /// return (T)(object)((List<MathMod>)list).ApplyTModList(fval);
            
            // This is faster than doing ( ((List<MathMod>)list).ApplyTModList(fval) as T retVal), as that has a type check, JIT will optimize using Elision
            //
            // in normal context, object obj = 5.0f definitly boxes
            // however when inside a generic method where T is known at runtime to be a float, the JIT compiler performs an operation known as Elision
            //  JIT sees the "bridge" casts and removes boxing instructions for the final machine code
            //  the result is direct assignment with zero heap allocation
            //  ex: The JIT compiler sees (T)(object)(float) and knows that T is a float. (it will remove the (object) bridge box)
            
            // Double Elision, or Double Bridge Cast
            if (type == typeof(MathMod) && typeof(T) == typeof(float)) return (T)(object)((List<MathMod>)list).ApplyTModList((float)(object)val); 

            
            return val;
        }

        public static T ApplyTModList<T, TMod>(this List<TMod> list, T val)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
        {
            for (int i = 0; i < list.Count; i++)
                val = list[i].Apply(val);

            return val;
        }
        
        public interface IStatModStrategy<T>
            where T : struct
        {
            public ulong hash { get; set; } // For removal comparisons, with two or more of the Same Modifier in the slots
            public T Apply(T input);
        }
        
        /// <summary>
        /// Simple struct wrapper for Func<T, T> to directly modify a stat user math
        /// </summary>
        [Serializable]
        public struct MathMod : IStatModStrategy<float>
        {
            public ulong hash { get; set; }
            public float Apply(float input) => func(input);
            public Func<float, float> func { get; set; }
            public MathMod(Expression<Func<float, float>> expr)
            {
                hash = Hashing.Fnv1a64(expr.ToString());
                func = expr.Compile();
            }
        }
        
    }

}
