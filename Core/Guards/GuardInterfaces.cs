using System;
using EMILtools.Core;
using UnityEngine;

namespace EMILtools.Core
{

// Guarders

    public interface IGuarder
    {
    }

    public interface IActionGuarder : IGuarder
    {
        IGuardReaction CurrentBlocker { get; }
        bool TryEarlyExit();
    }


// Conditions

    public interface IGuardCondition
    {
        string If { get; }
        bool Blocked { get; }
    }


    public interface IGuardReaction : IGuardCondition
    {
        public Action branchingAction { get; }
    }
}