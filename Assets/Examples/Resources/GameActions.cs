using ARC;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameActions : NetworkBehaviour
{
    public Button readyButton;
    public Button spawnButton;
    public Button grabButton;

    ARCUser user;

    void Start()
    {
        user = GetComponent<ARCUser>();
        
        if (isLocalPlayer)
        {
            GetComponentInChildren<Canvas>().enabled = true;
            ARCSetupManager.manager.sessionEvents.sessionAnchorCreated.AddListener(EnableReadyButton);
        }
    }

    void EnableReadyButton()
    {
        readyButton.gameObject.SetActive(true);
    }

    public void SpawnCube()
    {
        CmdSpawnCube(user.localCameraPosition + 3f * user.localCameraForward, user.localCameraRotation);
        Invoke("GrabCube", 0.1f);
    }

    public void GrabCube()
    {
        CmdGrabCube(user.localCameraPosition, user.localCameraRotation);
    }

    [Command]
    void CmdSpawnCube(Vector3 position, Quaternion rotation)
    {
        ARCSession.Spawn(ARCSession.session.sharedSessionPrefabs[0], position, rotation);
    }

    [Command]
    void CmdGrabCube(Vector3 position, Quaternion rotation)
    {
        if (user.interactingEntity != null)
            user.interactingEntity.Release(user, position, rotation);
        else
        {
            int layer = 1;
            ARCEntity candidate = ARCSession.RaycastEntity(position, rotation * Vector3.forward, 5f, layer);

            if (candidate != null)
            {
                candidate.Grab(user, position, rotation);
            }
        }
    }
}
