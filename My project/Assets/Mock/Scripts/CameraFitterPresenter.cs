using UnityEngine;

public class CameraFitterPresenter
{
    private CameraFitterView view;
    private Camera camera;
    private MapBoundary mapBoundary;

    public CameraFitterPresenter(CameraFitterView view, Camera camera, MapBoundary mapBoundary)
    {
        this.view = view;
        this.camera = camera;
        this.mapBoundary = mapBoundary;
    }

    public void Follow(Vector2 target)
    {
        Rect movableRect = mapBoundary.GetCameraMovableRect();

        float vertExtent = camera.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float minX = movableRect.xMin + horzExtent;
        float maxX = movableRect.xMax - horzExtent;
        float minY = movableRect.yMin + vertExtent;
        float maxY = movableRect.yMax - vertExtent;

        float x = Mathf.Clamp(target.x, minX, maxX);
        float y = Mathf.Clamp(target.y, minY, maxY);

        view.SetCameraPosition(new Vector3(x, y, view.transform.position.z));
    }
}