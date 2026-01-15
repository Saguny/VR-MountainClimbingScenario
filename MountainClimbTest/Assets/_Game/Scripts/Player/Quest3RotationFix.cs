using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

// The configuration attribute is only needed in the Editor to set up the build.
// The runtime player doesn't need it.
#if UNITY_EDITOR
[OpenXRFeature(
    TargetOpenXRApiVersion = "1.1.53",
    UiName = "Quest 3 Rotation Fix",
    BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
    DocumentationLink = "https://issuetracker.unity3d.com/issues/xr-interaction-toolkit-xr-controllers-are-inverted-by-y-axis-when-using-meta-quest-3-with-openxr-plugin"
)]
#endif
public class Quest3RotationFix : OpenXRFeature
{
    // This class remains empty.
    // It acts as a bridge to force the specific OpenXR version during the build process.
}