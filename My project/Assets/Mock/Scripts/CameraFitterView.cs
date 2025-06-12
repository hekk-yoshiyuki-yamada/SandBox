using UnityEngine;

public class CameraFitterView : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] MapBoundary mapBoundary;
    [SerializeField] float margin = 0.5f;
    [SerializeField] PlayerView playerView;
    [SerializeField ]int verticalTileCount = 6;

    private CameraFitterPresenter presenter;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        var targetOrthoSize = verticalTileCount / 2f + margin;
        targetCamera.orthographicSize = targetOrthoSize;

        presenter = new CameraFitterPresenter(this, targetCamera, mapBoundary, playerView);
    }

    void LateUpdate()
    {
        presenter.FollowPlayer();
    }

    public void SetCameraPosition(Vector3 position)
    {
        targetCamera.transform.position = position;
    }
}