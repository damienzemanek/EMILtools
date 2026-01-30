using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static PlayerInputActions;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader")]
public class InputReader : ScriptableObject, IPlayerActions, IInputMouseLook
{
    public event UnityAction EnableMouseControlCamera = delegate { };
    public event UnityAction DisableMouseControlCamera = delegate { };
    public event UnityAction<bool> Jump = delegate { };

    [SerializeField] PlayerInputActions inputActions;
    public Vector3 move;
    public Vector2 mouse { get; set; }
    
    private void OnEnable()
    {
        if(inputActions == null)
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.SetCallbacks(this);
        }
    }

    public void EnablePlayerActions() => inputActions.Enable();

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();
        move = new Vector3(v.x, 0f, v.y);
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        mouse = context.ReadValue<Vector2>();
    }


    public void OnMouseControlCamera(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started: EnableMouseControlCamera.Invoke(); break;
            case InputActionPhase.Canceled: DisableMouseControlCamera.Invoke(); break;
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        //noop
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        //noop
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        switch(context.phase)
        {
            case InputActionPhase.Started: Jump.Invoke(true); break;
            case InputActionPhase.Canceled: Jump.Invoke(false); break;
        }
    }
}
