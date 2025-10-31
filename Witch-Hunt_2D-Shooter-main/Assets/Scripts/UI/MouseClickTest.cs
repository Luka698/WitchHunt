using UnityEngine;

public class MouseClickTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("✅ Chuột trái click!");
        }
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("✅ Chuột phải click!");
        }
    }
}
