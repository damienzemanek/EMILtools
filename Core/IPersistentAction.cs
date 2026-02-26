using System;

namespace EMILtools.Core
{
    public interface IPersistentDelegate
    {
        void API_Add(Delegate cb);
        void API_Remove(Delegate cb);
    }

    public interface IPersistentAction<in TDelegate> : IPersistentAction
        where TDelegate : Delegate
    {
        void Add(TDelegate cb);
        void Remove(TDelegate cb);
        int Count { get; }
        void PrintInvokeListNames();
    }

    public interface IPersistentAction<in TDelegate, out TPersistentCRTP> : IPersistentDelegate
        where TDelegate : Delegate
        where TPersistentCRTP : IPersistentAction<TDelegate, TPersistentCRTP>
    {
        TPersistentCRTP Add(TDelegate cb);
        TPersistentCRTP Remove(TDelegate cb);
        int Count { get; }
        void PrintInvokeListNames();
    }

    public interface IPersistentAction : IPersistentDelegate
    {
        Delegate Add(Delegate cb);
        Delegate Remove(Delegate cb);
        int Count { get; }
        void PrintInvokeListNames();
    }
}