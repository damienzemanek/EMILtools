using System;
using EMILtools.Core;

/// <summary>
/// Settables manage generic state using Template Method Pattern
/// </summary>
/// <typeparam name="T1"></typeparam>
public interface ISettableTemplate<T1>
{
    public IPersistentDelegate action { get; set; }
    public Delegate TemplateCall { get; }
    public PersistentAction OnSet { get; set; }
    
    /// <summary>
    /// All SettableTMP's will have at least 1 unnamedStoredValue1, meaning it is accessible from the interface
    /// </summary>
    public T1 _unnamedStoredValue1 { get; set; }
}