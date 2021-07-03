using UnityEngine;
using UnityEngine.UI;

public class Navpoint : MonoBehaviour
{
    public Transform reference;
    public Image arrow;

    float width;
    float height;
    Vector3 centre;
    Camera cam;
    
    void Start()
    {
        cam = Camera.main;

        RectTransform rt = GetComponent<RectTransform>();
        width = cam.pixelWidth / 2 - rt.sizeDelta.x;
        height = cam.pixelHeight / 2 - rt.sizeDelta.y;
        centre = new Vector3(cam.pixelWidth / 2, cam.pixelHeight / 2);

        UpdateIndicator();
    }

    void Update()
    {
        UpdateIndicator();
    }

    void UpdateIndicator()
    {
        arrow.enabled = false;
        Vector3 proj = cam.WorldToScreenPoint(reference.position);
        proj -= centre;

        if ( (proj.x < -width) || (proj.x > width) || (proj.y < -height) || (proj.y > height) || (proj.z < 0) )
        {
            arrow.enabled = true;
            proj *= Mathf.Sign(proj.z);
            
            float angle = Mathf.Atan2(proj.y, proj.x);

            Vector3 candidateW = new Vector3(
                width * Mathf.Sign(proj.x),
                width * Mathf.Tan(angle) * Mathf.Sign(Mathf.Cos(angle))
            );
            Vector3 candidateH = new Vector3(
                height * Mathf.Tan(Mathf.PI / 2 - angle) * Mathf.Sign(Mathf.Cos(Mathf.PI / 2 - angle)),
                height * Mathf.Sign(proj.y)
            );

            proj = (candidateW.sqrMagnitude < candidateH.sqrMagnitude) ? candidateW : candidateH;
            arrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
        }

        transform.position = proj + centre;
    }
}
