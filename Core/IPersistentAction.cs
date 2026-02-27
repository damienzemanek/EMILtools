using System;

namespace EMILtools.Core
{
    /// <summary>
    /// Lowest level Delegate
    /// </summary>
    public interface IPersistentDelegate
    {
        Delegate Add(Delegate cb);
        Delegate Remove(Delegate cb);
    }
    
    /// <summary>
    /// Persistent Action with no type constraints on the Delegate
    /// </summary>
    public interface IPersistentAction : IPersistentDelegate
    {
        int Count { get; }
        void PrintInvokeListNames();
    }

    /// <summary>
    /// Generic Constrained TDelegate
    /// No CRTP
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    public interface IPersistentAction<TDelegate> : IPersistentAction
        where TDelegate : Delegate
    {
        void Add(TDelegate cb);
        void Remove(TDelegate cb);
    }

    /// <summary>
    /// Generic Constrained TDelegate and TPersistentAction
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    /// <typeparam name="TPersistentCRTP"></typeparam>
    public interface IPersistentAction<in TDelegate, out TPersistentCRTP>
        where TDelegate : Delegate
        where TPersistentCRTP : IPersistentAction<TDelegate, TPersistentCRTP>
    {
        TPersistentCRTP Add(TDelegate cb);
        TPersistentCRTP Remove(TDelegate cb);
    }
    
}