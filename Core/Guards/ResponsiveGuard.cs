using System;
using System.Collections.Generic;
using EMILtools.Core;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;



public abstract class ActionGuarder : IActionGuarder
{
    public static readonly string NONE = "CAN ACCESS";
    public static readonly IGuardReaction NoneResponsiveGuardCondition = new LazyActionGuard(NONE);
    public abstract IGuardReaction CurrentOpenBranch { get; }
    public abstract bool TryEarlyExit();

    public bool TryEarlyExit(IEnumerable<IGuardReaction> guards)
    {
        foreach (var guard in guards)
        {
            if (guard.Blocked)
            {
                guard.branchingAction?.Invoke();
                return true;
            }
        }
        return false;
    }
}



public class ActionGuarderImmutable : ActionGuarder, IActionGuarder
{
    [ShowInInspector, PropertyOrder(-1)] public override IGuardReaction CurrentOpenBranch
    {
        get
        {
            for(int i = 0; i < guards.Length; i++)
                if (guards[i].Blocked) return guards[i];
            return NoneResponsiveGuardCondition;
        }
    }
        
    [ShowInInspector] public readonly IGuardReaction[] guards;

    public ActionGuarderImmutable(params IGuardReaction[] guards) => this.guards = guards;

    public override bool TryEarlyExit() => base.TryEarlyExit(guards);

    public static implicit operator bool(ActionGuarderImmutable chain) => chain.TryEarlyExit();
}

public class ActionGuarderMutable : ActionGuarder, IActionGuarder
{
    [ShowInInspector, PropertyOrder(-1)] public override IGuardReaction CurrentOpenBranch
    {
        get
        {
            for(int i = 0; i < guards.Count; i++)
                if (guards[i].Blocked) return guards[i];
            return NoneResponsiveGuardCondition;
        }
    }
        
    [ShowInInspector] public List<IGuardReaction> guards;

    public ActionGuarderMutable()
    {
        guards = new List<IGuardReaction>();
    }
    
    
    public ActionGuarderMutable(params IGuardReaction[] links) => this.guards = new List<IGuardReaction>(links);
        
    public ActionGuarderMutable Add(IGuardReaction guard)
    {
        guards.Add(guard);
        return this;
    }

    public override bool TryEarlyExit() => base.TryEarlyExit(guards);
    public static implicit operator bool(ActionGuarderMutable chain) => chain.TryEarlyExit();

}