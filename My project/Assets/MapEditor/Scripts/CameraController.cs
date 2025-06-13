using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] Vector3 initializePosition;
    [SerializeField] SpriteRenderer targetSpriteRenderer;

    private Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
        ClampCameraPosition();
    }

    void Update()
    {
        var move = Vector3.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) move += Vector3.left;
        if (Input.GetKey(KeyCode.RightArrow)) move += Vector3.right;
        if (Input.GetKey(KeyCode.UpArrow)) move += Vector3.up;
        if (Input.GetKey(KeyCode.DownArrow)) move += Vector3.down;

        if (move != Vector3.zero)
        {
            camera.transform.Translate(move * (Time.deltaTime * moveSpeed));
            ClampCameraPosition();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            camera.transform.position = initializePosition;
            ClampCameraPosition();
        }
    }

    void ClampCameraPosition()
    {
        if (targetSpriteRenderer == null) return;

        var bounds = targetSpriteRenderer.bounds;
        var pos = camera.transform.position;

        var vertExtent = camera.orthographicSize;
        var horzExtent = vertExtent * Screen.width / Screen.height;

        var minX = bounds.min.x + horzExtent;
        var maxX = bounds.max.x - horzExtent;
        var minY = bounds.min.y + vertExtent;
        var maxY = bounds.max.y - vertExtent;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        camera.transform.position = pos;
    }
}