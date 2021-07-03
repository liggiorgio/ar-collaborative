using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DestroyAfterTime : NetworkBehaviour
{
    public float countdown;

    void Start()
    {
        StartCoroutine(DestroyCoroutine());
    }

    IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(countdown);
        NetworkServer.Destroy(gameObject);
    }
}
