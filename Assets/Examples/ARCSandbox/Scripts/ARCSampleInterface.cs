using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ARCSampleInterface : MonoBehaviour
{
    [Header("Navigation UI")]
    public CanvasGroup mainScreen;
    public CanvasGroup hostScreen;
    public CanvasGroup joinScreen;
    public CanvasGroup playScreen;

    [Header("Host UI")]
    public LayoutGroup thumbnailContainer;
    public LayoutGroup thumbnailContainer2;

    [Header("Prefabs")]
    public GameObject thumbnailPrefab;
    public GameObject markerPrefab;

    GameObject buttonExit, buttonExit2;
    GameObject buttonRetry, buttonRetry2;
    GameObject buttonReady;

    void Start()
    {
        buttonExit = GameObject.Find("ButtonExit");
        buttonExit2 = GameObject.Find("ButtonExit2");
        buttonRetry = GameObject.Find("ButtonRetry");
        buttonRetry2 = GameObject.Find("ButtonRetry2");
        buttonReady = GameObject.Find("ButtonReady");
        buttonRetry.SetActive(false);
        buttonRetry2.SetActive(false);
        buttonReady.SetActive(false);
        ShowScreen(-1);
        StartCoroutine(InvokeShowScreen(0, 3f));
        //GameObject.Find("ButtonContinue").GetComponent<Button>().interactable = false;
    }

    public void AddThumbnail(Texture2D image)
    {
        GameObject thumbnail = Instantiate(thumbnailPrefab) as GameObject;
        thumbnail.GetComponent<RawImage>().texture = image;
        thumbnail.transform.SetParent(thumbnailContainer.transform);
        buttonExit.SetActive(false);
        buttonRetry.SetActive(true);
    }

    public void OnManifestReceived(int[] manifest)
    {
        for (int i = 0; i < manifest.Length; ++i)
        {
            GameObject thumbnail = Instantiate(markerPrefab) as GameObject;
            thumbnail.transform.SetParent(thumbnailContainer2.transform);
        }
    }

    public void OnChunkReceived(int marker, float progress)
    {
        GameObject thumbnail = thumbnailContainer2.transform.GetChild(marker).gameObject;
        thumbnail.GetComponent<MarkerThumbnail>().SetProgress(progress);
    }

    public void OnMarkerReceived(int marker, Texture2D texture)
    {
        GameObject thumbnail = thumbnailContainer2.transform.GetChild(marker).gameObject;
        thumbnail.GetComponent<MarkerThumbnail>().SetThumbnail(texture);
    }

    public void OnReceivingCompleted()
    {
        foreach (Transform t in thumbnailContainer2.transform)
        {
            t.GetComponent<MarkerThumbnail>().SetPending();
        }
    }

    public void OnMarkerScanned(int marker)
    {
        GameObject thumbnail = thumbnailContainer2.transform.GetChild(marker).gameObject;
        thumbnail.GetComponent<MarkerThumbnail>().SetDone();
    }

    public void ClearThumbnails()
    {
        foreach (Transform child in thumbnailContainer.transform) { Destroy(child.gameObject); }
        buttonExit.SetActive(true);
        buttonRetry.SetActive(false);
    }

    public void ClearThumbnails2()
    {
        foreach (Transform child in thumbnailContainer2.transform) { Destroy(child.gameObject); }
    }

    public void ResetAnchorConfirm()
    {
        buttonExit2.SetActive(true);
        buttonRetry2.SetActive(false);
        buttonReady.SetActive(false);
    }

    public void ToggleAnchorConfirm(bool show)
    {
        buttonExit2.SetActive(!show);
        buttonRetry2.SetActive(show);
        buttonReady.SetActive(show);
    }

    public void ShowScreen(int screen) {
        mainScreen.alpha = screen == 0 ? 1 : 0;
        hostScreen.alpha = screen == 1 ? 1 : 0;
        joinScreen.alpha = screen == 2 ? 1 : 0;
        playScreen.alpha = screen == 3 ? 1 : 0;
        mainScreen.interactable = screen == 0;
        hostScreen.interactable = screen == 1;
        joinScreen.interactable = screen == 2;
        playScreen.interactable = screen == 3;
        mainScreen.blocksRaycasts = screen == 0;
        hostScreen.blocksRaycasts = screen == 1;
        joinScreen.blocksRaycasts = screen == 2;
        playScreen.blocksRaycasts = screen == 3;
    }

    public IEnumerator InvokeShowScreen(int screen, float time)
    {
        yield return new WaitForSeconds(Mathf.Abs(time));
        ShowScreen(screen);
    }

    public void SetReady()
    {
        FindObjectOfType<CaptainsMess>().LocalPlayer().SendReadyToBeginMessage();
    }
}
