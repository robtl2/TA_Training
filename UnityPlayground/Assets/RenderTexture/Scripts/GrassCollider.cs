using UnityEngine;

[ExecuteInEditMode]
public class GrassCollider : MonoBehaviour
{
    public float radius = 1.0f;
    public bool debug = false;

    void OnEnable()
    {
        GrassInteractions.instance.grassColliders.Add(this);
    }

    void OnDisable()
    {
        GrassInteractions.instance.grassColliders.Remove(this);
    }
}
