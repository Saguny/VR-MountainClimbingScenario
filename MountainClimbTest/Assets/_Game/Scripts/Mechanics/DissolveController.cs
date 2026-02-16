using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Renderer))]
[ExecuteAlways]
public class DissolveController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float maxInteractionDistance = 5.0f;
    public Transform playerCamera;

    [Range(0.1f, 0.9f)]
    public float textDistanceFromCamera = 0.3f;

    [Header("Dissolve Settings")]
    public float dissolveDuration = 2f;
    public bool destroyWhenDone = true;

    [Header("TMP Text Settings")]
    public TextMeshProUGUI textMesh;
    public string messageToShow = "That's odd...";
    public float textDisplayDuration = 2.0f;

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    public float previewProgress = 0f;

    private int dissolveAmountID;
    private bool isDissolving = false;
    private Renderer targetRenderer;
    private Collider targetCollider;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        targetCollider = GetComponent<Collider>();
        dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            if (textMesh != null) textMesh.gameObject.SetActive(false);
            if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

            // Ensure car starts visible at runtime
            UpdateDissolve(0);
        }
    }

    void OnValidate()
    {
        // This makes the slider work in the Inspector
        UpdateDissolve(previewProgress);
    }

    private void UpdateDissolve(float value)
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        if (dissolveAmountID == 0) dissolveAmountID = Shader.PropertyToID("_DissolveAmount");

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(dissolveAmountID, value);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    public void TriggerDissolve()
    {
        if (isDissolving || !Application.isPlaying) return;

        float distance = Vector3.Distance(playerCamera.position, transform.position);

        if (distance <= maxInteractionDistance)
        {
            PositionTextInFrontOfPlayer();
            StartCoroutine(DissolveRoutine());
        }
    }

    private void PositionTextInFrontOfPlayer()
    {
        if (textMesh == null || playerCamera == null) return;

        Vector3 targetPos = Vector3.Lerp(playerCamera.position, transform.position, textDistanceFromCamera);
        textMesh.transform.position = targetPos;
        textMesh.transform.LookAt(playerCamera);
        textMesh.transform.Rotate(0, 180, 0);
    }

    private IEnumerator DissolveRoutine()
    {
        isDissolving = true;

        if (textMesh != null)
        {
            textMesh.text = messageToShow;
            textMesh.gameObject.SetActive(true);
            StartCoroutine(HandleTextLifeCycle());
        }

        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / dissolveDuration);

            // Use the same helper function for runtime animation
            UpdateDissolve(progress);

            yield return null;
        }

        targetRenderer.enabled = false;
        if (targetCollider != null) targetCollider.enabled = false;

        if (destroyWhenDone)
        {
            yield return new WaitForSeconds(textDisplayDuration);
            Destroy(gameObject);
        }
    }

    private IEnumerator HandleTextLifeCycle()
    {
        float elapsed = 0f;
        Color startColor = textMesh.color;
        while (elapsed < textDisplayDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / textDisplayDuration;
            if (normalizedTime > 0.5f)
            {
                float fadeAlpha = Mathf.InverseLerp(1.0f, 0.5f, normalizedTime);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, fadeAlpha);
            }
            yield return null;
        }
        textMesh.gameObject.SetActive(false);
    }
}