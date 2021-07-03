using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera cam;

    void Start() { cam = Camera.main; }

    void Update() { transform.rotation = cam.transform.rotation; }
}