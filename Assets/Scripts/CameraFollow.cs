using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static GameObject TryFindPlayerGameObject()
    {
        var player = PlayerSetupFlow.TryResolveRuntimePlayer();
        if (player != null)
        {
            return player;
        }

        try
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            // Tag not defined in project — fall through to name search.
        }

        if (player != null)
        {
            return player;
        }

        return GameObject.Find("Player");
    }

    /// <summary>Picks a single main camera when duplicates exist (e.g. merged scenes).</summary>
    private static Camera GetPreferredMainCamera()
    {
        Camera fallback = null;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!cam.isActiveAndEnabled)
            {
                continue;
            }

            if (!cam.CompareTag("MainCamera"))
            {
                continue;
            }

            if (cam.gameObject.name == "Main Camera")
            {
                return cam;
            }

            fallback ??= cam;
        }

        return fallback != null ? fallback : Camera.main;
    }

    public Transform target;
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    private bool hasJumpedToTarget = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToCamera()
    {
        var mainCamera = GetPreferredMainCamera();
        if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
            Debug.Log($"CameraFollow: Attached to {mainCamera.gameObject.name}");
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            hasJumpedToTarget = false;
            var player = TryFindPlayerGameObject();
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"CameraFollow: Found player at {target.position}");
            }

            return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (!hasJumpedToTarget)
        {
            transform.position = desiredPosition;
            hasJumpedToTarget = true;
            Debug.Log($"CameraFollow: Jumped to player at {desiredPosition}");
        }
        else
        {
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
