// Assets/Scripts/Interactions/DragObject.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DragObject : MonoBehaviour
{
    Camera cam;
    Vector3 offset;
    float zPos;
    public bool IsDragging { get; private set; }

    void Awake()
    {
        cam = Camera.main;
        zPos = Mathf.Abs(cam.transform.position.z - transform.position.z);
    }

    void OnMouseDown()
    {
        IsDragging = true;
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(world.x, world.y, transform.position.z);
    }

    void OnMouseDrag()
    {
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(world.x, world.y, transform.position.z) + offset;
    }

    void OnMouseUp()
    {
        IsDragging = false;
    }
}
