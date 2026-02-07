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
            foreach (var dec in decorators)
            {
                var decInfoOutput = dec.TryApplyThruDecoratorFirst(val);
                if(!decInfoOutput.blocked) val = dec.TryApplyThruDecoratorFirst(val).output;
            }
            return val;
        }


        //--------------------------------------------------------------------------------------
        //                  Decorator Timer Overrides (Custom Timer)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // //----------------------------------  FLOAT - MathMod  -----------------------------------------
        public static (MathMod, TTag, IStatUser) WithTimer<TMod, TTag, TGate>(this (MathMod mod, TTag tag, IStatUser user) data, CountdownTimer timer, bool startEnabled = true,
            Action[] OnAddDecorCBs = null,
            Action[] OnRemoveDecorCBs = null)
                where TTag: struct, IStatTag
                where TGate : class, IGate
            => data.WithTimer<float, MathMod, TTag, TGate>(timer, startEnabled, OnAddDecorCBs, OnRemoveDecorCBs);
        
        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data, CountdownTimer timer, bool startEnabled = true,
            Action[] OnAddDecorCBs = null,
            Action[] OnRemoveDecorCBs = null)
                where TMod : struct, IStatModStrategy<float>
                where TTag: struct, IStatTag
                where TGate : class, IGate
                => data.WithTimer<float, TMod, TTag, TGate>(timer, startEnabled, OnAddDecorCBs, OnRemoveDecorCBs);
        
        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data, CountdownTimer timer, bool startEnabled = true,
            Action[] OnAddDecorCBs = null,
            Action[] OnRemoveDecorCBs = null)
                where T : struct
                where TMod : struct, IStatModStrategy<T>
                where TTag : struct, IStatTag
                where TGate : class, IGate
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TTag> decor = new StatModDecTimed<T, TMod,TTag, TGate>(
                data.mod.hash,
                timer,
                startEnabled,
                OnAddDecorCBs,
                OnRemoveDecorCBs);
        
            data.user.AddDecorator<T, TMod, TTag>(decor);
        
            return data;
        }


        //--------------------------------------------------------------------------------------
        //             Decorator Timer Overrides (OUTs for debugs, and flexibility)
        //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        // //----------------------------------  FLOAT - MathMod  -----------------------------------------
        public static (MathMod, TTag, IStatUser) WithTimer<TTag, TGate>(this (MathMod mod, TTag tag, IStatUser user) data, float duration, bool startEnabled,
            out CountdownTimer timer,
            out StatModDecTimed<float, MathMod, TTag, TGate> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where TTag : struct, IStatTag
                where TGate : class, IGate
            => data.WithTimer<float, MathMod, TTag, TGate>(duration, startEnabled, out timer, out decor, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data, float duration, bool startEnabled,
            out CountdownTimer timer,
            out StatModDecTimed<float, TMod, TTag, TGate> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where TMod : struct, IStatModStrategy<float>
                where TTag : struct, IStatTag
                where TGate : class, IGate
        => data.WithTimer<float, TMod, TTag, TGate>(duration, startEnabled, out timer, out decor, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data, float duration, bool startEnabled,
            out CountdownTimer timer,
            out StatModDecTimed<T, TMod, TTag, TGate> decor,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where T : struct
                where TMod : struct, IStatModStrategy<T>
                where TTag : struct, IStatTag
                where TGate : class, IGate
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            decor = new StatModDecTimed<T, TMod, TTag, TGate>(
                data.mod.hash,
                timer = new CountdownTimer(duration),
                startEnabled,
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
        public static (MathMod, TTag, IStatUser) WithTimer<TTag, TGate>(this (MathMod mod, TTag tag, IStatUser user) data, float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where TTag : struct, IStatTag
                where TGate : class, IGate
            => data.WithTimer<float, MathMod, TTag, TGate>(duration, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  FLOAT - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data, float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where TMod : struct, IStatModStrategy<float>
                where TTag : struct, IStatTag
                where TGate : class, IGate
            => data.WithTimer<float, TMod, TTag, TGate>(duration, OnAddCbs, OnRemoveCbs);

        // //----------------------------------  GENERIC T - GENERIC TMod  -----------------------------------------
        public static (TMod, TTag, IStatUser) WithTimer<T, TMod, TTag, TGate>(this (TMod mod, TTag tag, IStatUser user) data,
            float duration,
            Action[] OnAddCbs = null,
            Action[] OnRemoveCbs = null)
                where T : struct
                where TMod : struct, IStatModStrategy<T>
                where TTag : struct, IStatTag
                where TGate : class, IGate
        {
            // Not setting the ref to the modifier strategy here
            // that happens after sending the modifier to the IStatUser
            IStatModDecorator<T, TTag> decor = new StatModDecTimed<T, TMod, TTag, TGate>(
                data.mod.hash,
                new CountdownTimer(duration));

            data.user.AddDecorator<T, TMod, TTag>(decor);

            return data;
        }

    }
}
