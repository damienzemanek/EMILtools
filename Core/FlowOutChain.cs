using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;


[Serializable]
public abstract class FlowOutChain
{
    static readonly string NONE = "CAN ACCESS";
    static readonly Link NONE_LINK = new Link(NONE, null, NONE, null);
    
    public abstract Link CurrentOpenBranch { get; }
    
    public abstract bool TryEarlyExit();

    public bool TryEarlyExit(IEnumerable<Link> links)
    {
        foreach (var link in links)
        {
            if (link.thisIs?.Invoke() ?? false)
            {
                link.doMethodAndFlowOut?.Invoke();
                return true;
            }
        }

        return false;
    }

    public static Link BranchIf(string ifName, Func<bool> when, string thenName, Action then)
        => new Link(ifName, when, thenName, then);
    
    public static Link BranchIf(Func<bool> when, Action then)
        => new Link(when, then);

    public static Link ReturnIf(string ifName, Func<bool> when)
        => new Link(ifName, when, "return", null);
    
    public static Link ReturnIf(Func<bool> when)
        => new Link(when, null);

    [Serializable]
    public readonly struct Link
    {
        
        [HorizontalGroup("Top", 250)] [ShowInInspector, ReadOnly] public readonly string If;
        [HorizontalGroup("Top")] [ShowInInspector, ReadOnly, HideLabel] public bool V => thisIs?.Invoke() ?? false;
        [HorizontalGroup("Bottom", 250)] [ShowInInspector, ReadOnly] public readonly string Then;
        [HideInInspector] public readonly Action doMethodAndFlowOut;
        [HideInInspector] public readonly Func<bool> thisIs;


        public Link(string checkName, Func<bool> check, string methodName, Action method)
        {
            thisIs = check;
            doMethodAndFlowOut = method;

            If = string.IsNullOrWhiteSpace(checkName) ? check?.Method.Name : checkName;
            Then = string.IsNullOrWhiteSpace(methodName)
                ? (method != null ? method.Method.Name : "return")
                : methodName;

            If ??= "null-check";
            Then ??= "null-action";
        }

        public Link(Func<bool> check, Action method)
        {
            thisIs = check;
            doMethodAndFlowOut = method;

            If = check?.Method.Name ?? "null-check";
            Then = method != null ? method.Method.Name : "return";
        }
        
    }

    [Serializable]
    public class FlowImmutable : FlowOutChain
    {
        [ShowInInspector, PropertyOrder(-1)] public override Link CurrentOpenBranch
        {
            get
            {
                for(int i = 0; i < links.Length; i++)
                    if (links[i].V) return links[i];
                return NONE_LINK;
            }
        }
        
        [ShowInInspector] public readonly Link[] links;

        public FlowImmutable() => links = new Link[] { };

        public FlowImmutable(params Link[] links) => this.links = links;
        

        public override bool TryEarlyExit() => base.TryEarlyExit(links);

        public static implicit operator bool(FlowImmutable chain) => chain.TryEarlyExit();
    }

    [Serializable]
    public class FlowMutable : FlowOutChain
    {
        [ShowInInspector, PropertyOrder(-1)] public override Link CurrentOpenBranch
        {
            get
            {
                for(int i = 0; i < links.Count; i++)
                    if (links[i].V) return links[i];
                return NONE_LINK;
            }
        }
        
        [ShowInInspector]  public List<Link> links;

        public FlowMutable() => links = new List<Link>();
        public FlowMutable(params Link[] links) => this.links = new List<Link>(links);

        public FlowMutable Add(string checkName, Func<bool> check, string methodName, Action method)
        {
            links.Add(new Link(checkName, check, methodName, method));
            return this;
        }

        public override bool TryEarlyExit() => base.TryEarlyExit(links);
        public static implicit operator bool(FlowMutable chain) => chain.TryEarlyExit();

    }
}
