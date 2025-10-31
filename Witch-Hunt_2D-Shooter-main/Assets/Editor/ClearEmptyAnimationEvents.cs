#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ClearEmptyAnimationEvents
{
    // Xoá event rỗng trên các AnimationClip đang chọn trong Project
    [MenuItem("Tools/Animation/Clear Empty Events On Selected Clips")]
    static void ClearOnSelectedClips()
    {
        int removed = 0;
        foreach (var obj in Selection.objects)
        {
            var clip = obj as AnimationClip;
            if (!clip) continue;

            var events = AnimationUtility.GetAnimationEvents(clip);
            var kept = events.Where(e => !string.IsNullOrEmpty(e.functionName)).ToArray();
            if (kept.Length != events.Length)
            {
                AnimationUtility.SetAnimationEvents(clip, kept);
                EditorUtility.SetDirty(clip);
                removed += (events.Length - kept.Length);
                Debug.Log($"[Clean] {clip.name}: removed {events.Length - kept.Length} empty event(s).");
            }
        }
        AssetDatabase.SaveAssets();
        if (removed == 0) Debug.Log("[Clean] No empty events found on selected clips.");
    }

    // Xoá event rỗng cho toàn bộ clip nằm trong Animator của GameObject đang chọn (VD: Doraemon)
    [MenuItem("Tools/Animation/Clear Empty Events On Selected Animator")]
    static void ClearOnSelectedAnimator()
    {
        var go = Selection.activeGameObject;
        if (!go) { Debug.LogWarning("Select a GameObject with Animator in Hierarchy."); return; }

        var animator = go.GetComponent<Animator>();
        if (!animator || animator.runtimeAnimatorController == null)
        { Debug.LogWarning("Selected object has no Animator/Controller."); return; }

        var clips = animator.runtimeAnimatorController.animationClips.Distinct();
        int removed = 0;
        foreach (var clip in clips)
        {
            var events = AnimationUtility.GetAnimationEvents(clip);
            var kept = events.Where(e => !string.IsNullOrEmpty(e.functionName)).ToArray();
            if (kept.Length != events.Length)
            {
                AnimationUtility.SetAnimationEvents(clip, kept);
                EditorUtility.SetDirty(clip);
                removed += (events.Length - kept.Length);
                Debug.Log($"[Clean] {clip.name}: removed {events.Length - kept.Length} empty event(s).");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[Clean] Done. Removed total {removed} empty event(s).");
    }
}
#endif
