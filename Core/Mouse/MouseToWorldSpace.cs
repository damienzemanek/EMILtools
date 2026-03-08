using System;
using EMILtools.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "MouseToWorldSpace", menuName = "EMILtools/ScriptableObjects/Mouse/MouseToWorldSpace")]
public class MouseToWorldSpace : ScriptableObject
{
    [Serializable]
    public struct OffsetRelative
    {
        public float scalar;
        public bool x, y, z;
    }
    
    
    [BoxGroup("Settings")] [SerializeField] public float maximumLength;
    [BoxGroup("Settings")] [SerializeField] public bool lookAtCollisions = false;
    [BoxGroup("Settings")] [SerializeField] public bool lookAtPlane = false;
    [BoxGroup("Settings")] [SerializeField] [ShowIf("lookAtPlane")]  public LayerMask lookAtPlaneMask;
        
    [BoxGroup("ReadOnly")] [ReadOnly] [ShowIf("lookAtPlane")] public Vector3 contactPoint; 
    [BoxGroup("ReadOnly")] [ShowInInspector, ReadOnly] public Vector3 direction;
    public Ray rayMouse;
    public Vector3 regularOffset;
    public bool offsetRelative = false;
    [ShowIf("offsetRelative")] public OffsetRelative offsetRelativeTo;
    
    public Vector3 GetHitPoint(Camera cam, Transform center = null)
    {
        RaycastHit hit;
        var mousePos = Input.mousePosition + regularOffset;
        rayMouse = cam.ScreenPointToRay(mousePos);
        Debug.DrawRay(rayMouse.origin, rayMouse.direction * maximumLength, Color.red);
        int layermask = 0;
        if(lookAtPlane) layermask = lookAtPlaneMask.value;
        if(Physics.Raycast (rayMouse.origin, rayMouse.direction, out hit, maximumLength, layermask))
        {

            if (lookAtCollisions || lookAtPlane)
            {
                if (offsetRelative)
                {
                    float distToCenter = Vector3.Distance(center.position, hit.point);
                    distToCenter *= offsetRelativeTo.scalar;
                    if (offsetRelativeTo.x) hit.point = hit.point.With(x: hit.point.x + distToCenter);
                    if (offsetRelativeTo.y) hit.point = hit.point.With(y: hit.point.y + distToCenter);
                    if (offsetRelativeTo.z) hit.point = hit.point.With(z: hit.point.z + distToCenter);
                }
                return contactPoint = hit.point;
            }
        }
        
        // Looks at the direction max length point by default
        var pos = rayMouse.GetPoint(maximumLength);
        contactPoint = Vector3.zero;
        return pos;
    }
}

