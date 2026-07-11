using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputListener : MonoBehaviour
{
    [Serializable] public class InputRecievedEvent : UnityEvent<InputAction.CallbackContext> { }

    [SerializeField] private InputActionReference input;

    public InputRecievedEvent onInput = new();

    void OnEnable()
    {
        input.ToInputAction().performed += onInput.Invoke;
    }

    void OnDisable()
    {
        input.ToInputAction().performed -= onInput.Invoke;
    }
}