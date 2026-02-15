using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Renderer))]
[ExecuteAlways]
public class DissolveController : MonoBehaviour
{
    [Header("Dissolve Settings")]
    public float dissolveDuration = 2f;

    public float delayBeforeDissolve = 0f;

    public bool destroyWhenDone = true;

    [Header("Animation")]
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    public float previewProgress = 0f;

    [Header("Optional Events")]
    public UnityEvent onDissolveStart;

    public UnityEvent onDissolveComplete;

    private Material[] materials;
    private int dissolveAmountID;
    private bool isDissolving = false;
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        dissolveAmountID = Shader.PropertyToID("_DissolveAmount");

        if (Application.isPlaying)
        {
            materials = targetRenderer.materials;
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            if (dissolveAmountID == 0) dissolveAmountID = Shader.PropertyToID("_DissolveAmount");

            targetRenderer.GetPropertyBlock(propertyBlock);
            float curveValue = dissolveCurve.Evaluate(previewProgress);
            propertyBlock.SetFloat(dissolveAmountID, curveValue);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void TriggerDissolve()
    {
        if (!isDissolving && Application.isPlaying)
        {
            StartCoroutine(DissolveRoutine());
        }
    }

    private IEnumerator DissolveRoutine()
    {
        isDissolving = true;

        if (delayBeforeDissolve > 0)
        {
            yield return new WaitForSeconds(delayBeforeDissolve);
        }

        onDissolveStart?.Invoke();

        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(elapsedTime / dissolveDuration);
            float curveValue = dissolveCurve.Evaluate(normalizedTime);

            foreach (Material mat in materials)
            {
                mat.SetFloat(dissolveAmountID, curveValue);
            }

            yield return null;
        }

        foreach (Material mat in materials)
        {
            mat.SetFloat(dissolveAmountID, 1f);
        }

        onDissolveComplete?.Invoke();

        if (destroyWhenDone)
        {
            Destroy(gameObject);
        }
    }
}