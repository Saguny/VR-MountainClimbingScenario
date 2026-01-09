using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Required for Unity 6 / XRI 3.3+

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
        public List<RespawnableTool> toolsToRecover;

        /// <summary>
        /// Call this when the player dies/respawns to recover their gear.
        /// </summary>
        public void RecoverDroppedTools()
        {
            foreach (var tool in toolsToRecover)
            {
                if (tool.item == null || tool.homeSlot == null) continue;

                // 1. Check if the player is currently holding the item.
                // If isSelected is true, the player (or a socket) is holding it.
                // We typically only want to respawn items that are dropped (floating in void).
                if (!tool.item.isSelected)
                {
                    ResetToolPhysics(tool);
                }
                // Optional: If you want to force items back to belt even if held, remove the 'if' check.
            }
        }

        private void ResetToolPhysics(RespawnableTool tool)
        {
            Rigidbody rb = tool.item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Kill all momentum so it doesn't shoot out of the socket
                rb.linearVelocity = Vector3.zero; // Note: 'velocity' is deprecated in Unity 6, use 'linearVelocity'
                rb.angularVelocity = Vector3.zero;
            }

            // Move to home position
            tool.item.transform.position = tool.homeSlot.position;
            tool.item.transform.rotation = tool.homeSlot.rotation;
        }
    }
}