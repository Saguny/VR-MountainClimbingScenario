using UnityEngine;
using System.Collections.Generic;
using MountainRescue.Interfaces;

namespace MountainRescue.Engine
{
    /// <summary>
    /// Checks all climbing gear. If ANY tool is holding on, the player is "Anchored".
    /// </summary>
    public class SafetyGearManager : MonoBehaviour, IAnchorStateProvider
    {
        [Header("Safety Tools")]
        [Tooltip("Drag your Left Ice Pick, Right Ice Pick, and Anchor Hook here.")]
        [SerializeField] private List<GameObject> gearObjects;

        private List<IAnchorStateProvider> _providers = new List<IAnchorStateProvider>();

        private void Start()
        {
            // Find the interface on the assigned objects
            foreach (var obj in gearObjects)
            {
                var provider = obj.GetComponent<IAnchorStateProvider>();
                if (provider != null)
                {
                    _providers.Add(provider);
                }
                else
                {
                    Debug.LogWarning($"[SafetyManager] {obj.name} does not implement IAnchorStateProvider!");
                }
            }
        }

        public bool IsAnchored()
        {
            // If ANY tool is stuck, we are safe.
            foreach (var tool in _providers)
            {
                if (tool.IsAnchored()) return true;
            }
            return false;
        }
    }
}