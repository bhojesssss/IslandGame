using UnityEngine;

/// <summary>
/// Top-down camera that smoothly follows the player and stays inside the map bounds.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new(0f, 0.5f);
    [SerializeField] float smoothTime = 0.12f;

    [Header("Map bounds (world units)")]
    [SerializeField] bool clampToBounds = true;
    [SerializeField] Vector2 boundsMin = new(0f, 0f);
    [SerializeField] Vector2 boundsMax = new(72f, 52f);

    static CameraFollow instance;

    Camera cam;
    Vector3 velocity;
    float shakeUntil;
    float shakeStrength;

    public static void Shake(float strength, float duration)
    {
        if (instance == null) return;
        instance.shakeStrength = strength;
        instance.shakeUntil = Time.time + duration;
    }

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target != null) transform.position = Clamp(DesiredPosition());
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
        position = Clamp(position);
        if (Time.time < shakeUntil) position += (Vector3)(Random.insideUnitCircle * shakeStrength);
        transform.position = position;
    }

    Vector3 DesiredPosition() =>
        new(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

    Vector3 Clamp(Vector3 position)
    {
        if (!clampToBounds) return position;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        position.x = ClampAxis(position.x, boundsMin.x + halfWidth, boundsMax.x - halfWidth);
        position.y = ClampAxis(position.y, boundsMin.y + halfHeight, boundsMax.y - halfHeight);
        return position;
    }

    static float ClampAxis(float value, float min, float max) =>
        min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
}
