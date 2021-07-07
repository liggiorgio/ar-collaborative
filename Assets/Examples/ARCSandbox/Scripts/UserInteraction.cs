using ARC;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserInteraction : NetworkBehaviour
{
    public GameObject namePlate;
    public bool autograbOnSpawn;
    public Sprite grabIcon;
    public Sprite releaseIcon;
    
    ARCUser user;
    GameObject np;
    GameObject create, dropdown, move, destroy, zoomIn, zoomOut, rotL, rotR, position, look, throwObj;
    bool hasStarted = false;
    Vector3 prevTouchPosition;
    Vector3 prevTouchPosition2;
    bool isMovingSession;
    Vector3 currMovingOffset;
    float currRotatingOffset;
    
    void Start()
    {
        user = GetComponent<ARCUser>();
        np = Instantiate(namePlate);

        if (user.isLocalPlayer)
        {
            user.CmdSetUsername(FindObjectOfType<InputField>().text);

            //GameObject.Find("ButtonReady").GetComponent<Button>().onClick.AddListener(PressReadyButton);
            create = GameObject.Find("ButtonCreate");
            dropdown = GameObject.Find("DropdownEntity");
            move = GameObject.Find("ButtonMove");
            destroy = GameObject.Find("ButtonDestroy");

            zoomIn = GameObject.Find("ButtonZoomIn");
            zoomOut = GameObject.Find("ButtonZoomOut");

            rotL = GameObject.Find("ButtonRotateLeft");
            rotR = GameObject.Find("ButtonRotateRight");
            
            position = GameObject.Find("ButtonPosition");
            look = GameObject.Find("ButtonLookAt");
            throwObj = GameObject.Find("ButtonThrow");

            create.GetComponent<Button>().onClick.AddListener(SpawnEntity);
            move.GetComponent<Button>().onClick.AddListener(MoveEntity);
            destroy.GetComponent<Button>().onClick.AddListener(DestroyEntity);
            zoomIn.GetComponent<Button>().onClick.AddListener(Grow);
            zoomOut.GetComponent<Button>().onClick.AddListener(Shrink);
            rotL.GetComponent<Button>().onClick.AddListener(RotateLeft);
            rotR.GetComponent<Button>().onClick.AddListener(RotateRight);
            position.GetComponent<Button>().onClick.AddListener(NewSessionPose);
            look.GetComponent<Button>().onClick.AddListener(LookAt);
            throwObj.GetComponent<Button>().onClick.AddListener(Throw);

            ToggleUI(false);
        }
    }

    void Update()
    {
        if (np)
        {
            np.transform.position = transform.position;
            np.GetComponentInChildren<Text>().text = user.username;
        }

        if (ARCSession.sessionState != ARCSessionState.Playing) return;

        if (!isLocalPlayer) return;

        if (user.interactingEntity == null)
        {
            if (isMovingSession)
            {
                // Move session space with user
                Vector3 projNewLocalPos = ARCSession.origin.parent.InverseTransformPoint(Camera.main.transform.position);
                projNewLocalPos.y = 0f;

                ARCSession.origin.localPosition = projNewLocalPos + currMovingOffset;
                
                if (Input.touchCount == 1)
                {
                    // Rotate with fingers
                    Touch touch = Input.touches[0];

                    if (touch.phase == TouchPhase.Began)
                    {
                        prevTouchPosition = touch.position;
                    }
                    else if (touch.phase == TouchPhase.Moved)
                    {
                        Vector3 currTouchPosition = touch.position;
                        float dx = (currTouchPosition.x - prevTouchPosition.x) / 100f;
                        
                        currRotatingOffset -= dx;
                        ARCSession.origin.rotation *= Quaternion.Euler(0f, -dx, 0f);
                    }
                }
            }
            else
            {
                int layer = 1 << 3;
                if (ARCSession.RaycastEntity(user.localCameraPosition, user.localCameraForward, 3f, layer))
                {
                    move.SetActive(true);
                    move.transform.GetChild(0).GetComponent<Image>().sprite = grabIcon;
                }
                else
                {
                    move.SetActive(false);
                }
            }
        }
        else
        {
            move.SetActive(true);
            move.transform.GetChild(0).GetComponent<Image>().sprite = releaseIcon;
            
            if (Input.touchCount == 1)
            {
                // Rotate with fingers
                Touch touch = Input.touches[0];

                if (touch.phase == TouchPhase.Began)
                {
                    prevTouchPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    Vector3 currTouchPosition = touch.position;
                    float dx, dy;
                    dx = (currTouchPosition.x - prevTouchPosition.x) / 10f;
                    dy = (currTouchPosition.y - prevTouchPosition.y) / 10f;

                    if ((Mathf.Abs(dx) > 5f) || (Mathf.Abs(dy) > 5f))
                    {
                        Quaternion zRot = Quaternion.AngleAxis(-dx,
                            Quaternion.Inverse(user.interactingEntity.transform.localRotation) *
                            Quaternion.Inverse(user.localCameraRotation) *
                            Vector3.up);
                        Vector3 projLocalRight = user.localCameraRotation * Vector3.right;
                        projLocalRight.y = 0f;
                        Quaternion xRot = Quaternion.AngleAxis(dy,
                            Quaternion.Inverse(user.interactingEntity.transform.localRotation) *
                            Quaternion.Inverse(user.localCameraRotation) *
                            projLocalRight.normalized);
                        
                        CmdRotate(zRot * xRot);
                        prevTouchPosition = currTouchPosition;
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        Destroy(np);
        if (user.isLocalPlayer)
            ToggleUI(true);
    }

    public void ToggleUI(bool show)
    {
        if (user.isLocalPlayer)
        {
            create.SetActive(show);
            move.SetActive(show);
            dropdown.SetActive(show);
            position.SetActive(show);
            destroy.SetActive(show);
            zoomIn.SetActive(show);
            zoomOut.SetActive(show);
            rotL.SetActive(show);
            rotR.SetActive(show);
            look.SetActive(show);
            throwObj.SetActive(show);
        }
    }

    public void ToggleInteractionUI(bool show)
    {
        if (user.isLocalPlayer)
        {
            create.SetActive(!show);
            move.SetActive(true);
            dropdown.SetActive(!show);
            position.SetActive(!show);
            destroy.SetActive(show);
            zoomIn.SetActive(show);
            zoomOut.SetActive(show);
            rotL.SetActive(show);
            rotR.SetActive(show);
            look.SetActive(!show);
            throwObj.SetActive(show);
            if (user.interactingEntity != null)
            {
                if (user.interactingEntity.GetComponent<Rigidbody>() == null)
                    throwObj.GetComponent<Button>().interactable = false;
                else
                    throwObj.GetComponent<Button>().interactable = true;
            }
        }
    }

    void NewSessionPose()
    {
        if (isMovingSession)
        {
            // Place the session objects at the current position
            isMovingSession = false;

            Vector3 projNewLocalPos = ARCSession.origin.parent.InverseTransformPoint(Camera.main.transform.position);
            projNewLocalPos.y = 0f;
            CmdNewPose(projNewLocalPos + currMovingOffset, currRotatingOffset);
        }
        else
        {
            // Start moving the session objects around (following the user)
            isMovingSession = true;

            currMovingOffset = ARCSession.origin.parent.
                InverseTransformVector(ARCSession.origin.position - Camera.main.transform.position);
            currMovingOffset.y = 0f;
            currRotatingOffset = ARCSession.session.sessionSpaceOrientation;
        }
    }

    void SpawnEntity()
    {
        int index = 0;
        string selectionLabel = dropdown.transform.GetChild(0).GetComponent<Text>().text;
        switch (selectionLabel) {
            case "Snapping cube":
                index = 0; break;
            case "Snapping sphere":
                index = 1; break;
            case "Snapping cylinder":
                index = 2; break;
            case "Floating cube":
                index = 3; break;
            case "Floating sphere":
                index = 4; break;
            case "Floating cylinder":
                index = 5; break;
            case "Physics cube":
                index = 6; break;
            case "Physics sphere":
                index = 7; break;
            case "Physics cylinder":
                index = 8; break;
            default: return;
        }
        CmdSpawnEntity(index, user.localCameraPosition + user.localCameraForward * 3f, Quaternion.identity);
        // Autograb
        if (autograbOnSpawn)
            Invoke("MoveEntity", 0.1f);
    }

    void MoveEntity()
    {
        CmdMoveEntity(user.localCameraPosition, user.localCameraRotation);
    }

    void DestroyEntity()
    {
        CmdDestroyEntity(user.localCameraPosition, user.localCameraRotation);
    }

    void Grow()
    {
        CmdGrow();
    }

    void Shrink()
    {
        CmdShrink();
    }

    void RotateLeft()
    {
        Quaternion userRot = Quaternion.AngleAxis(15f,
            Quaternion.Inverse(user.interactingEntity.transform.localRotation) *
            Quaternion.Inverse(user.localCameraRotation) *
            Vector3.up);
        CmdRotate(userRot);
    }

    void RotateRight()
    {
        Quaternion userRot = Quaternion.AngleAxis(-15f,
            Quaternion.Inverse(user.interactingEntity.transform.localRotation) *
            Quaternion.Inverse(user.localCameraRotation) *
            Vector3.up);
        CmdRotate(userRot);
    }

    void LookAt()
    {
        CmdLookAt(user.localCameraPosition, user.localCameraRotation);
    }

    void Throw()
    {
        CmdThrow(user.localCameraPosition, user.localCameraRotation);
    }

    // Commands
    [Command]
    void CmdNewPose(Vector3 pos, float ang)
    {
        ARCSession.session.SetSessionSpacePose(pos, ang);
    }

    [Command]
    void CmdSpawnEntity(int index, Vector3 position, Quaternion rotation)
    {
        ARCSession.Spawn(ARCSession.session.sharedSessionPrefabs[index], position, rotation);
    }

    [Command]
    void CmdMoveEntity(Vector3 position, Quaternion rotation)
    {
        if (user.interactingEntity != null)
        {
            user.interactingEntity.Release(user, position, rotation);
        }
        else
        {
            int layer = 1 << 3;
            ARCEntity entity = ARCSession.RaycastEntity(position, rotation * Vector3.forward, 3f, layer);
            if (entity != null)
                entity.Grab(user, position, rotation);
        }
    }

    [Command]
    void CmdDestroyEntity(Vector3 position, Quaternion rotation)
    {
        if (user.interactingEntity != null)
        {
            ARCSession.Destroy(user.interactingEntity.gameObject);
        }
        else
        {
            int layer = 1 << 3;
            ARCEntity entity = ARCSession.RaycastEntity(position, rotation * Vector3.forward, 3f, layer);
            if (entity != null)
                ARCSession.Destroy(entity.gameObject);
        }
    }

    [Command]
    void CmdGrow()
    {
        user.interactingEntity.AddScale(0.1f);
    }

    [Command]
    void CmdShrink()
    {
        user.interactingEntity.AddScale(-0.1f);
    }

    [Command]
    void CmdRotate(Quaternion rotation)
    {
        user.interactingEntity.AddHandlingRotation(rotation);
    }

    [Command]
    void CmdLookAt(Vector3 position, Quaternion rotation)
    {
        int layer = 1 | (1 << 3);
        Vector3 location = ARCSession.RaycastPosition(position, rotation * Vector3.forward, 100f, layer);
        if (location != Vector3.negativeInfinity)
            ARCSession.Spawn(ARCSession.session.sharedSessionPrefabs[9], location, Quaternion.identity);
    }

    [Command]
    void CmdThrow(Vector3 position, Quaternion rotation) {
        ARCEntity toThrow = user.interactingEntity;
        if (toThrow == null) return;
        if (toThrow.GetComponent<Rigidbody>() == null) return;

        toThrow.Release(user, position, rotation);
        toThrow.SetMotion(rotation * (Vector3.forward + Vector3.up/2) * 5f, Vector3.zero);
    }
}
