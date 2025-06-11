using UnityEngine;

public class CameraFitterPresenter
{
    private CameraFitterView view;
    private Camera camera;
    private MapBoundary mapBoundary;
    private float margin;
    private PlayerView playerView;

    public CameraFitterPresenter(CameraFitterView view, Camera camera, MapBoundary mapBoundary, float margin, PlayerView playerView)
    {
        this.view = view;
        this.camera = camera;
        this.mapBoundary = mapBoundary;
        this.margin = margin;
        this.playerView = playerView;
    }

    public void FollowPlayer()
    {
        if (playerView == null) return;
        var playerPos = playerView.transform.position;
        var camPos = camera.transform.position;

        // カメラ移動可能範囲で制限
        var movableRect = mapBoundary.GetCameraMovableRect();
        var halfHeight = camera.orthographicSize;
        var halfWidth = halfHeight * ((float)Screen.width / Screen.height);

        camPos.x = Mathf.Clamp(playerPos.x, movableRect.xMin + halfWidth, movableRect.xMax - halfWidth);
        camPos.y = Mathf.Clamp(playerPos.y, movableRect.yMin + halfHeight, movableRect.yMax - halfHeight);

        view.SetCameraPosition(camPos);
    }
}