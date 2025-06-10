using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    [SerializeField] CameraFitterView cameraView;
    [SerializeField] PlayerController playerController;
    [SerializeField] MapBoundary mapBoundary;

    private CameraFitterPresenter presenter;
    private Camera camera;

    void Start()
    {
        camera = cameraView.GetComponent<Camera>();
        presenter = new CameraFitterPresenter(cameraView, camera, mapBoundary);
    }

    void LateUpdate()
    {
        Vector2 playerPos = playerController.GetPlayerGridPos();
        presenter.Follow(playerPos);
    }
}