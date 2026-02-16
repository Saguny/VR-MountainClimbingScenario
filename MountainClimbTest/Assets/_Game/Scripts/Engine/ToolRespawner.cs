using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MountainRescue.Systems
{
    public class ToolRespawner : MonoBehaviour
    {
        [System.Serializable]
        public struct RespawnableTool
        {
            public string name;
            [Tooltip("The actual item GameObject (must have Rigidbody)")]
            public XRGrabInteractable item;
            [Tooltip("The socket/transform on the belt where this item belongs")]
            public Transform homeSlot;
        }

        [Header("Configuration")]
        [Tooltip("If true, tools dropped by the player (not in a socket) will automatically magnet back after a delay.")]
        public bool autoMagnetBack = true;
        public float magnetDelay = 2.0f;

        public List<RespawnableTool> toolsToRecover;

        private void OnEnable()
        {
            foreach (var tool in toolsToRecover)
            {
                if (tool.item != null)
                {
                    tool.item.selectExited.AddListener(OnToolDropped);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var tool in toolsToRecover)
            {
                if (tool.item != null)
                {
                    tool.item.selectExited.RemoveListener(OnToolDropped);
                }
            }
        }

        /// <summary>
        /// Call this when the player dies/respawns.
        /// It acts as a "Hard Reset" for inventory.
        /// </summary>
        public void RecoverDroppedTools()
        {
            StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            // Wait one frame to ensure Player Teleport logic has fully applied 
            // and transforms are valid in the new location.
            yield return null;

            foreach (var tool in toolsToRecover)
            {
                if (tool.item == null || tool.homeSlot == null) continue;

                // 1. IcePick Unstick
                if (tool.item.TryGetComponent<IcePick>(out var pick))
                {
                    pick.ForceReset();
                }

                // 2. Force Drop from Hands
                if (tool.item.isSelected)
                {
                    var interactors = tool.item.interactorsSelecting;
                    for (int i = interactors.Count - 1; i >= 0; i--)
                    {
                        var interactor = interactors[i];
                        if (tool.item.interactionManager != null)
                        {
                            tool.item.interactionManager.SelectExit(interactor, tool.item);
                        }
                    }
                }

                // 3. NUCLEAR OPTION: Toggle GameObject to wipe physics state
                // This fixes the "Flaregun destroyed" and "92m offset" issues caused by physics interpolation.
                tool.item.gameObject.SetActive(false);

                // Reset Rigidbody while disabled
                Rigidbody rb = tool.item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Teleport to socket
                tool.item.transform.position = tool.homeSlot.position;
                tool.item.transform.rotation = tool.homeSlot.rotation;

                tool.item.gameObject.SetActive(true);

                // 4. Force Socket to Grab (Hard Attach)
                // We try to find a socket on the homeSlot transform
                var socket = tool.homeSlot.GetComponent<XRSocketInteractor>();
                if (socket != null && tool.item.interactionManager != null)
                {
                    // If the socket thinks it's holding something else, force it to let go
                    if (socket.hasSelection)
                    {
                        socket.interactionManager.SelectExit(socket, socket.interactablesSelected[0]);
                    }

                    // Force the socket to grab our tool
                    // Note: We wait a tiny bit for the Enable to register with the InteractionManager
                    // but since we are in a Coroutine, we can try immediately or wait frame.
                    tool.item.interactionManager.SelectEnter((IXRSelectInteractor) socket, (IXRSelectInteractable) tool.item);
                }
            }
        }

        // --- Magnet Logic (Unchanged) ---

        private void OnToolDropped(SelectExitEventArgs args)
        {

            if (MountainRescue.Systems.Session.GameSessionManager.Instance != null)
            {
                MountainRescue.Systems.Session.GameSessionManager.Instance.RegisterSafetyViolation();
            }

            if (!autoMagnetBack) return;
            XRGrabInteractable item = args.interactableObject as XRGrabInteractable;
            if (item != null) StartCoroutine(CheckMagnetRoutine(item));
        }

        private IEnumerator CheckMagnetRoutine(XRGrabInteractable item)
        {
            yield return new WaitForSeconds(0.1f);
            if (item == null || item.isSelected) yield break;

            var toolStruct = GetToolStruct(item);
            if (toolStruct.HasValue)
            {
                float timer = 0f;
                while (timer < magnetDelay)
                {
                    if (item.isSelected) yield break;
                    timer += Time.deltaTime;
                    yield return null;
                }

                if (!item.isSelected)
                {
                    // Gentle Reset for Magnet (No SetActive toggle needed here usually)
                    Rigidbody rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    item.transform.position = toolStruct.Value.homeSlot.position;
                    item.transform.rotation = toolStruct.Value.homeSlot.rotation;

                    // Attempt forced socketing here too for reliability
                    var socket = toolStruct.Value.homeSlot.GetComponent<XRSocketInteractor>();
                    if (socket != null && item.interactionManager != null)
                    {
                        item.interactionManager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)item);
                    }
                }
            }
        }

        private RespawnableTool? GetToolStruct(XRGrabInteractable item)
        {
            foreach (var tool in toolsToRecover)
            {
                if (tool.item == item) return tool;
            }
            return null;
        }
    }
}