using ARC;
using UnityEngine;

public class DestroyOnFall : MonoBehaviour
{
    ARCEntity entity;

    void Start()
    {
        entity = GetComponent<ARCEntity>();
        
        if (!entity.isServer)
            enabled = false;
    }

    void Update()
    {
        if (entity.handler == null && transform.localPosition.y < 0f)
            ARCSession.Destroy(entity.gameObject);            
    }
}
