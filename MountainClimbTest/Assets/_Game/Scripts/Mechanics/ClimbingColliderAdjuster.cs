using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;

[RequireComponent(typeof(CharacterController))]
public class ClimbingColliderAdjuster : MonoBehaviour
{
    [Header("Climbing Shape")]
    public float climbingRadius = 0.1f;
    public float climbingHeight = 1.0f;
    public Vector3 climbingCenter = new Vector3(0f, 0.5f, 0f);

    private CharacterController _cc;
    private float _defaultRadius;
    private float _defaultHeight;
    private Vector3 _defaultCenter;
    private int _climbSources = 0;

    private ClimbProvider _climbProvider;

    public static ClimbingColliderAdjuster Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _cc = GetComponent<CharacterController>();
        _defaultRadius = _cc.radius;
        _defaultHeight = _cc.height;
        _defaultCenter = _cc.center;
    }

    void OnEnable()
    {
        _climbProvider = FindFirstObjectByType<ClimbProvider>();
        if (_climbProvider != null)
        {
            _climbProvider.locomotionStarted += OnXRIClimbStarted;
            _climbProvider.locomotionEnded += OnXRIClimbEnded;
        }
    }

    void OnDisable()
    {
        if (_climbProvider != null)
        {
            _climbProvider.locomotionStarted -= OnXRIClimbStarted;
            _climbProvider.locomotionEnded -= OnXRIClimbEnded;
        }
    }

    private void OnXRIClimbStarted(LocomotionProvider provider) => AddClimbSource();
    private void OnXRIClimbEnded(LocomotionProvider provider) => RemoveClimbSource();

    // Called by IcePick
    public void AddClimbSource()
    {
        _climbSources++;
        if (_climbSources == 1) ApplyClimbingShape();
    }

    public void RemoveClimbSource()
    {
        _climbSources = Mathf.Max(0, _climbSources - 1);
        if (_climbSources == 0) RestoreDefaultShape();
    }

    private void ApplyClimbingShape()
    {
        _cc.radius = climbingRadius;
        _cc.height = climbingHeight;
        _cc.center = climbingCenter;
    }

    private void RestoreDefaultShape()
    {
        _cc.radius = _defaultRadius;
        _cc.height = _defaultHeight;
        _cc.center = _defaultCenter;
    }
}