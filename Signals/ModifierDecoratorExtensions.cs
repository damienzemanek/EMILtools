using System;
using System.Collections.Generic;
using EMILtools.Timers;
using JetBrains.Annotations;
using UnityEngine;
using static EMILtools.Signals.ModiferRouting;
using static EMILtools.Signals.ModifierStrategies;
using static EMILtools.Signals.StatTags;

namespace EMILtools.Signals
{
    public static class ModifierDecoratorExtensions
    {

        //-----------------------------------------------------------------------------------
        //            Modifier Applying Funcs / Decorator Flexible Thoroughfare
        //------------------------------------------------------------------------------------

        public static T ApplyAll<T, TTag>(this List<Stat<T, TTag>.ModifierSlot> modslots, T val)
            where T : struct
            where TTag : struct, IStatTag
        {
            // Decorator first, then modifier
            foreach (var slot in modslots) val = slot.SlotApply(val);
            return val;
        }

        public static T ApplyDecorators<T, TTag>(this List<IStatModDecorator<T, TTag>> decorators, T val)
            where T : struct
            where TTag : struct, IStatTag
        {
            foreach (var dec in decorators) val = dec.ApplyThruDecoratorFirst(val);
            return val;
        }


        //--------------------------------------------------------------------------------------
        //                  Decorator Timer Overrides (Custom Timer)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // //----------------------------------  FLOAT - MathMod  -----------------------------------------
        public static (MathMod, TTag, IStatUser) WithTimer<TMod, TTag>(this (MathMod mod, TTag tag, IStatUser user) data, CountdownTimer timer,
            Action[] OnAddDecorCBs = null,
            Action[] OnRemoveDecorCBs = null)
            where TTag: struct, IStatTag
            => data.WithTimer<float, MathMod, TTag>(timer, OnAddDecorCBs, OnRemoveDecorCBs);
        
        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data, CountdownTimer timer,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where TMod : struct, IStatModStrategy<float>
            where TTag: struct, IStatTag
                => data.WithTimer<float, TMod, TTag>(timer, OnAddDecorCBs, OnRemoveDecorCBs);
        
        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data, CountdownTimer timer,
                Action[] OnAddDecorCBs = null,
                Action[] OnRemoveDecorCBs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TTag> decor = new StatModDecTimed<T, TMod,TTag>(
                data.mod.hash,
                timer,
                OnAddDecorCBs,
                OnRemoveDecorCBs);
        
            data.user.AddDecorator<T, TMod, TTag>(decor);
        
            return data;
        }


        //--------------------------------------------------------------------------------------
        //             Decorator Timer Overrides (OUTs for debugs, and flexibility)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // //----------------------------------  FLOAT - MathMod  -----------------------------------------
        public static (MathMod, TTag, IStatUser) WithTimer<TTag>(this (MathMod mod, TTag tag, IStatUser user) data, float duration,
            out CountdownTimer timer,
            out StatModDecTimed<float, MathMod, TTag> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where TTag : struct, IStatTag
            => data.WithTimer<float, MathMod, TTag>(duration, out timer, out decor, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data, float duration,
            out CountdownTimer timer,
            out StatModDecTimed<float, TMod, TTag> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where TMod : struct, IStatModStrategy<float>
            where TTag : struct, IStatTag
        => data.WithTimer<float, TMod, TTag>(duration, out timer, out decor, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data, float duration,
            out CountdownTimer timer,
            out StatModDecTimed<T, TMod, TTag> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            decor = new StatModDecTimed<T, TMod, TTag>(
                data.mod.hash,
                timer = new CountdownTimer(duration),
                OnAddCbs,
                OnRemoveCbs);

            data.user.AddDecorator<T, TMod, TTag>(decor);

            return data;
        }

        // }

        //--------------------------------------------------------------------------------------
        //                          Decorator Timer Overrides (NO OUTS)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // //----------------------------------  FLOAT - MathMod  -----------------------------------------
        public static (MathMod, TTag, IStatUser) WithTimer<TTag>(this (MathMod mod, TTag tag, IStatUser user) data, float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where TTag : struct, IStatTag
            => data.WithTimer<float, MathMod, TTag>(duration, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data, float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where TMod : struct, IStatModStrategy<float>
            where TTag : struct, IStatTag
            => data.WithTimer<float, TMod, TTag>(duration, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag>(this (TMod mod, TTag tag, IStatUser user) data,
            float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
            where T : struct
            where TMod : struct, IStatModStrategy<T>
            where TTag : struct, IStatTag
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TTag> decor = new StatModDecTimed<T, TMod, TTag>(
                data.mod.hash,
                new CountdownTimer(duration));

            data.user.AddDecorator<T, TMod, TTag>(decor);

            return data;
        }

    }
}
