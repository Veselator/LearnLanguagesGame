using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LearnLanguages.Core.Input
{
    /// <summary>
    /// Thin abstraction over the Input System so gameplay code never touches devices directly.
    /// </summary>
    public interface IInputService
    {
        Vector2 PointerPosition { get; }
        Vector2 PointerDelta { get; }
        float Scroll { get; }
        Vector2 MoveAxis { get; }
        bool PrimaryClicked { get; }
        bool SecondaryClicked { get; }
        bool MiddleHeld { get; }
        bool IsPointerOverUi { get; }
        bool CancelPressed { get; }
        bool SubmitPressed { get; }

        /// <summary>Space: end the turn on the map, execute orders in battle. Ignored while typing.</summary>
        bool NextTurnPressed { get; }
    }

    public sealed class InputService : IInputService
    {
        public Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public Vector2 PointerDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        /// <summary>Wheel notches; platforms report either raw (120 per notch) or normalized values.</summary>
        public float Scroll
        {
            get
            {
                if (Mouse.current == null) return 0f;
                var raw = Mouse.current.scroll.ReadValue().y;
                return Mathf.Abs(raw) > 10f ? raw / 120f : raw;
            }
        }
        public bool PrimaryClicked => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public bool SecondaryClicked => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public bool MiddleHeld => Mouse.current != null && Mouse.current.middleButton.isPressed;
        public bool CancelPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        public bool SubmitPressed => Keyboard.current != null &&
                                     (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);

        public bool NextTurnPressed =>
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !IsTyping;

        private static bool IsTyping
        {
            get
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                return selected != null && selected.GetComponent<TMPro.TMP_InputField>() != null;
            }
        }

        public bool IsPointerOverUi => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public Vector2 MoveAxis
        {
            get
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return Vector2.zero;
                var x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                        (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                var y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                        (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                return new Vector2(x, y);
            }
        }
    }
}
