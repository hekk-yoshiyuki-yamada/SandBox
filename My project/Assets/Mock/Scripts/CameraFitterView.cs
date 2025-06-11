using UnityEngine;

public class CameraFitterView : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] MapBoundary mapBoundary;
    [SerializeField] float margin = 0.5f;
    [SerializeField] PlayerView playerView;

    private CameraFitterPresenter presenter;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        presenter = new CameraFitterPresenter(this, targetCamera, mapBoundary, margin, playerView);
    }

    void LateUpdate()
    {
        presenter.FollowPlayer();
    }

    // View用API
    public void SetCameraSizeAndPosition(float size, Vector3 position)
    {
        targetCamera.orthographicSize = size;
        targetCamera.transform.position = position;
    }

    public void SetCameraPosition(Vector3 position)
    {
        targetCamera.transform.position = position;
    }
}