using UnityEngine;
using Model;

namespace BattlestarLabs.TransferTable
{
    [RequireComponent(typeof(TransferTableController))]
    public class TransferTableInput : MonoBehaviour
    {
        public TransferTableController controller;
        public TransferTableGraphAdapter adapter;

        [Header("Direct Stall Keys (Numpad 1-5)")]
        public KeyCode keyStall1 = KeyCode.Keypad1;
        public KeyCode keyStall2 = KeyCode.Keypad2;
        public KeyCode keyStall3 = KeyCode.Keypad3;
        public KeyCode keyStall4 = KeyCode.Keypad4;
        public KeyCode keyStall5 = KeyCode.Keypad5;

        [Header("Jog Stepping Keys")]
        public KeyCode keyStepLeft = KeyCode.KeypadMinus;
        public KeyCode keyStepRight = KeyCode.KeypadPlus;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<TransferTableController>();
            if (adapter == null) adapter = GetComponent<TransferTableGraphAdapter>();
        }

        private void Update()
        {
            // Only process input if this table is the active selected instance and not currently in placement mode
            if (TransferTablePlacement.ActiveInstance != null)
            {
                if (TransferTablePlacement.ActiveInstance.controller != this.controller || TransferTablePlacement.ActiveInstance.IsPlacementMode)
                    return;
            }

            // Direct selection
            if (Input.GetKeyDown(keyStall1)) TryMove(0);
            else if (Input.GetKeyDown(keyStall2)) TryMove(1);
            else if (Input.GetKeyDown(keyStall3)) TryMove(2);
            else if (Input.GetKeyDown(keyStall4)) TryMove(3);
            else if (Input.GetKeyDown(keyStall5)) TryMove(4);

            // Jog stepping
            else if (Input.GetKeyDown(keyStepLeft)) TryStep(-1);
            else if (Input.GetKeyDown(keyStepRight)) TryStep(1);
        }

        private void TryMove(int targetStall)
        {
            Debug.Log($"[{controller.tableId}] Key input received: Move to stall {targetStall + 1}");

            if (adapter != null)
            {
                try
                {
                    if (adapter.IsCarStraddlingGap(out Car blocker))
                    {
                        Debug.LogWarning($"[{controller.tableId}] Cannot move: Gap blocked by {blocker.DisplayName}!");
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{controller.tableId}] Note during gap safety check: {ex.Message}");
                }
            }

            controller.MoveToStall(targetStall);
        }

        private void TryStep(int direction)
        {
            Debug.Log($"[{controller.tableId}] Key input received: Step {(direction < 0 ? "Left" : "Right")}");

            if (adapter != null)
            {
                try
                {
                    if (adapter.IsCarStraddlingGap(out Car blocker))
                    {
                        Debug.LogWarning($"[{controller.tableId}] Cannot move: Gap blocked by {blocker.DisplayName}!");
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[{controller.tableId}] Note during gap safety check: {ex.Message}");
                }
            }

            if (direction < 0) controller.StepLeft();
            else controller.StepRight();
        }
    }
}