using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PSXCameraEffect : MonoBehaviour
{
    public int horizontalResolution = 320;
    public int verticalResolution = 240;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture buffer = RenderTexture.GetTemporary(horizontalResolution, verticalResolution, 24, source.format);

        buffer.filterMode = FilterMode.Point;

        Graphics.Blit(source, buffer);
        Graphics.Blit(buffer, destination);

        RenderTexture.ReleaseTemporary(buffer);
    }
}