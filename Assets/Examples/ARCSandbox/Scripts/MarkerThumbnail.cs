using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct Status
{
    public Sprite Icon;
    public Color Color;
}

public class MarkerThumbnail : MonoBehaviour
{
    [SerializeField] RawImage Thumbnail;
    [SerializeField] Image ProgressBar;
    [SerializeField] Image StatusIcon;
    [SerializeField] Status Downloading;
    [SerializeField] Status Downloaded;
    [SerializeField] Status Pending;
    [SerializeField] Status Done;

    public void SetProgress(float progress) { ProgressBar.fillAmount = progress; }

    public void SetThumbnail(Texture2D texture)
    {
        Thumbnail.texture = texture;
        Thumbnail.color = Color.white;
        ProgressBar.fillAmount = 0f;
        StatusIcon.sprite = Downloaded.Icon;
        StatusIcon.color = Downloaded.Color;
    }

    public void SetPending()
    {
        StatusIcon.sprite = Pending.Icon;
        StatusIcon.color = Pending.Color;
    }

    public void SetDone()
    {
        StatusIcon.sprite = Done.Icon;
        StatusIcon.color = Done.Color;
    }
}
