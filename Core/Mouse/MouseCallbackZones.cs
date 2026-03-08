using System;
using System.Collections.Generic;
using EMILtools.Core;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "Mouse Callback Zones", menuName = "ScriptableObjects/Mouse/Callback Zones")]
public class MouseCallbackZones
{ 
    public int w = 1920;
    public int h = 1080;
            
    [Serializable]
    public class CallbackZone
    {
        public Rect zone;
        [ShowInInspector, ReadOnly] bool wasInside;
        [NonSerialized] public PersistentAction callback;
            
        void EnsureInit() => callback ??= new PersistentAction();
            
        public void CheckZone(Vector2 mousePos)
        {
            EnsureInit();
            bool inside = zone.Contains(mousePos);
            if (inside && !wasInside) callback.Invoke();
            wasInside = inside;
        }

        public CallbackZone(Rect zone)
        {
            this.zone = zone;
            wasInside = false;
            callback = new PersistentAction();
        }
    }

    public void CheckAllZones(Vector2 mousePos)
    {
        if(callbackZones == null) Debug.LogError("No callback zones found, make sure to add some with AddInitialZones or AddZone");
        foreach (var zone in callbackZones) zone.CheckZone(mousePos);
    }
            
    [BoxGroup("References")] public List<CallbackZone> callbackZones;
}