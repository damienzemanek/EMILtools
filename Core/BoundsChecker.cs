using System;
using System.Collections.Generic;
using EMILtools.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class BoundsChecker : MonoBehaviour
{
    
    [Header("Who will receive the Message?")]
    [SerializeField] bool ThingCollidedWith;
    [SerializeField] bool SelectedReceiver;
    [SerializeField] [ShowIf("SelectedReceiver")] InterfaceReference<IBoundsCheckReceiver, MonoBehaviour> selectedReceiver;

    
    [Header("Which trigger callbacks are active?")]
    [SerializeField] private bool enter = true;
    [SerializeField] private bool exit = true;
    [SerializeField] private bool stay;

    [Header("Layer filtering")]
    [SerializeField] private LayerMask layerMask = ~0;

    HashSet<IBoundsCheckReceiver> collisions;

    void Awake()
    {
        this.Get<Collider>().isTrigger = true;
        if(ThingCollidedWith) collisions = new HashSet<IBoundsCheckReceiver>();
        if(selectedReceiver == null) Debug.LogError("No Receiver Selected");
        if(selectedReceiver.Value == null) Debug.LogError("No Receiver Selected");
    }

    private bool PassesLayerMask(GameObject go)
    {
        return (layerMask.value & (1 << go.layer)) != 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enter) return;

        if (ThingCollidedWith)
        {
            if (!PassesLayerMask(other.gameObject)) return;
            if (!other.TryGetComponent(out IBoundsCheckReceiver receiver)) return;
            if (!collisions.Add(receiver)) return;
            receiver.OnEnterBounds(other);
        }

        if (SelectedReceiver)
        {
            selectedReceiver.Value.OnEnterBounds(other);
            Debug.Log("Entered");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!exit) return;
        
        if (ThingCollidedWith)
        {
            if (!PassesLayerMask(other.gameObject)) return;
            if (!other.TryGetComponent(out IBoundsCheckReceiver receiver)) return;
            if (!collisions.Remove(receiver)) return;
            receiver.OnExitBounds(other);
        }
        if(SelectedReceiver) selectedReceiver.Value.OnExitBounds(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!stay) return;
        
        if (ThingCollidedWith)
        {
            if (!PassesLayerMask(other.gameObject)) return;
            if (!other.TryGetComponent(out IBoundsCheckReceiver receiver)) return;
            receiver.OnStayBounds(other);
        }
        if(SelectedReceiver) selectedReceiver.Value.OnStayBounds(other);
    }
}

public interface IBoundsCheckReceiver
{
    public virtual void OnEnterBounds(Collider other) { }
    public virtual void OnExitBounds(Collider other) { }
    public virtual void OnStayBounds(Collider other) { }
}