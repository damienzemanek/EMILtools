using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public interface IInputMouseLook
{
    public Vector2 mouse { get; set; }
}