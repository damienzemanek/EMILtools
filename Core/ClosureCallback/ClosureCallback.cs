using System;
using UnityEngine;

/// <summary>
/// Inteded to be a manual, Zero-Alloc Lambda alternative
/// Reason For Creation: Context dependant Closures create a Heap Allocated
///                      Temp class that GC churns, this avoids this
/// Pros:
/// - Suitable for better performance
/// - Requires slightly more boilerplate
///
/// Usage: 
/// - Pass into Action parameters using (This passes MethodGroup only) (Light)
///     - (For Actions)             myAction += cb.Invoke()
///     - (For PersistentActions)   myPersAction.Add(cb.Invoke())
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ClosureCallback<T, TActionParam1>
    where T: class
{
    [NonSerialized] public readonly T target;
    [NonSerialized] public readonly Action<TActionParam1> method;

    public ClosureCallback(T _target, Action<TActionParam1> _method)
    {
        target = _target;
        method = _method;
    }
    
    public void Invoke(TActionParam1 param1) 
        => method(param1);
}

public readonly struct ClosureCallback<T, TActionParam1, TActionParam2>
    where T: class
{
    [NonSerialized] public readonly T target;
    [NonSerialized] public readonly Action<TActionParam1, TActionParam2> method;

    public ClosureCallback(T _target, Action<TActionParam1, TActionParam2> _method)
    {
        target = _target;
        method = _method;
    }
    
    public void Invoke(TActionParam1 param1, TActionParam2 param2) 
        =>  method(param1, param2);
}

public readonly struct ClosureCallback<T, TActionParam1, TActionParam2, TActionParam3>
    where T: class
{
    [NonSerialized] public readonly T target;
    [NonSerialized] public readonly Action<TActionParam1, TActionParam2, TActionParam3> method;

    public ClosureCallback(T _target, Action<TActionParam1, TActionParam2, TActionParam3> _method)
    {
        target = _target;
        method = _method;
    }
    
    public void Invoke(TActionParam1 param1, TActionParam2 param2, TActionParam3 param3)
        => method(param1, param2, param3);
}