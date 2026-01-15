using UnityEngine.XR.OpenXR.Features;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;

[OpenXRFeature(
    TargetOpenXRApiVersion = "1.1.53", // This forces the correct loader version
    UiName = "Quest 3 Rotation Fix (PCVR & Android)",
    BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android }, // Standalone = PCVR
    DocumentationLink = "https://issuetracker.unity3d.com/issues/xr-interaction-toolkit-xr-controllers-are-inverted-by-y-axis-when-using-meta-quest-3-with-openxr-plugin"
)]
public class FixQuest3Rotation : OpenXRFeature { }