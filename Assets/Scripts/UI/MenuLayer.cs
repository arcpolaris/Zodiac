using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Zodiac.UI
{
    public class MenuLayer : MonoBehaviour
    {
        public List<MenuLayer> children = new();

        [Serializable] public class MenuOpenedEvent : UnityEvent { }
        [Serializable] public class MenuClosedEvent : UnityEvent { }

        [Serializable] private enum StartBehavior { None, Open, Close }
        [SerializeField] private StartBehavior startBehavior = StartBehavior.None;

        [Space, Space]
        public MenuOpenedEvent onOpen = new();
        public MenuClosedEvent onClose = new();

        void Start()
        {
            switch (startBehavior) {
                case StartBehavior.Open:
                    Open();
                    break;
                case StartBehavior.Close:
                    Close();
                    break;
            }
        }

        public bool IsOpen()
        {
            return gameObject.activeSelf;
        }

        public void Open()
        {
            if (IsOpen()) return;
            gameObject.SetActive(true);
            onOpen.Invoke();
        }

        public void Close()
        {
            if (!IsOpen()) return;
            foreach (var child in children)
            {
                child.Close();
            }

            gameObject.SetActive(false);
            onClose.Invoke();
        }

        public virtual void CloseDeepest()
        {
            var openChildren = children.Where(static child => child.IsOpen()).ToList();
            if (openChildren.Count > 0)
            {
                foreach (var child in openChildren)
                {
                    child.CloseDeepest();
                }
            }
            else
            {
                Close();
            }
        }
    }
}