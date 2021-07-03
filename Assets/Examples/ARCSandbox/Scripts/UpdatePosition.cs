using UnityEngine;
using UnityEngine.UI;

public class UpdatePosition : MonoBehaviour
{
    public Text trackedImageName;
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        
        if (trackedImageName != null)
        {
            trackedImageName.text = transform.root.name;
        }
    }

    void Update()
    {
        transform.position = cam.WorldToScreenPoint(transform.root.position);
    }
}
