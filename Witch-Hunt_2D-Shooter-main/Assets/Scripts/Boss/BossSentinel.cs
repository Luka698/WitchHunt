using UnityEngine;
public class BossSentinel : MonoBehaviour
{
    void OnDisable() { Debug.LogWarning("[Boss] OnDisable @ " + Time.time); }
    void OnDestroy() { Debug.LogError("[Boss] OnDestroy @ " + Time.time); }
}
