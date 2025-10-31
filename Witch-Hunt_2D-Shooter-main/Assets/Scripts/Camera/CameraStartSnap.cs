using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraStartSnap : MonoBehaviour
{
    public CameraController controller;

    void Reset() { controller = GetComponent<CameraController>(); }

    void Awake()
    {
        if (controller != null && controller.target != null)
        {
            Vector3 p = controller.target.position;
            transform.position = new Vector3(p.x, p.y, controller.cameraZCoordinate);
        }
        else
        {
            GameObject pl = GameObject.FindGameObjectWithTag("Player");
            if (pl != null)
            {
                Vector3 p = pl.transform.position;
                transform.position = new Vector3(p.x, p.y, -10f);
            }
        }
    }
}
