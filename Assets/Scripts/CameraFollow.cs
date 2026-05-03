using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    /// <summary>
    /// Resolves the player even if the "Player" tag is missing from TagManager
    /// (FindGameObjectWithTag throws in that case).
    /// </summary>
    public static GameObject TryFindPlayerGameObject()
    {
        GameObject player = null;
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

    public Transform target;
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    private bool hasJumpedToTarget = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachToCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CameraFollow>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraFollow>();
            Debug.Log("CameraFollow: Attached to Main Camera");
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
