using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Text sessionStateField;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        sessionStateField.text = ARC.ARCSession.sessionState.ToString();
    }
}
