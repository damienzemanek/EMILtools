using System;
using EMILtools.Core;
using UnityEngine;

/// <summary>
/// SettableTemplates (Setters) handle what happens when values are set
/// They solve the problem of needing to do repetitive templating and work generically
///
/// Example of Repetitive Templating:
/// ---------------------------------
/// void Set(T inputVal)
/// {
///     storedVal = inputVal;
///     OnSet();
/// }
///
/// void OnSet();
/// ---------------------------------
/// 
/// Ensure you set aliases for values they are generic storage only
/// </summary>
/// <typeparam name="T1"></typeparam>
public abstract class SettableTemplate<T1> : ISettableTemplate<T1>
{
    public T1 unnamedStoredValue1 => (this as ISettableTemplate<T1>)._unnamedStoredValue1;
    // Values
    [field: NonSerialized] T1 ISettableTemplate<T1>._unnamedStoredValue1 { get; set; }
    
    // Template Call 
    [NonSerialized] readonly Action<T1> _templateCall; 
    public Delegate TemplateCall => _templateCall;
    public PersistentAction OnSet { get; set; } = new();
    void _TemplateCall(T1 val)
    {
        ((ISettableTemplate<T1>)this)._unnamedStoredValue1 = val;
        Set(val);
        OnSet.Invoke();
    }
    
    // Ctor
    public SettableTemplate() => _templateCall = new Action<T1>(_TemplateCall);
    
    // Action
    [NonSerialized] IPersistentAction<Action<T1>> _action;
    public IPersistentDelegate action
    {
        get => _action;
        set =>  _action = (IPersistentAction<Action<T1>>)value;
    }

    // Abstract
    protected virtual void Set(T1 val) { }
}

/// <summary>
/// Ensure you set aliases for values they are for generic storage only
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public abstract class SettableTemplate<T1, T2> : ISettableTemplate<T1>
{
    public T1 unnamedStoredValue1 => (this as ISettableTemplate<T1>)._unnamedStoredValue1;
    // Values
    [field: NonSerialized] T1 ISettableTemplate<T1>._unnamedStoredValue1 { get; set; }
    public T2 unnamedStoredValue2 { get; set; }
    
    // Template Call
    readonly Action<T1, T2> _templateCall;
    public Delegate TemplateCall => _templateCall;
    void _TemplateCall(T1 val1, T2 val2)
    {
        ((ISettableTemplate<T1>)this)._unnamedStoredValue1 = val1;
        unnamedStoredValue2 = val2;
        Set(val1, val2);
        OnSet.Invoke();
    }
    
    // Ctor
    public SettableTemplate() => _templateCall = new Action<T1, T2>(_TemplateCall);
    public PersistentAction OnSet { get; set; } = new();

    // Action
    [NonSerialized] IPersistentAction<Action<T1, T2>> _action;
    public IPersistentDelegate action
    {
        get => _action; 
        set => _action = (IPersistentAction<Action<T1, T2>>)value;
    }

    protected virtual void Set(T1 val1, T2 val2) { }
}
