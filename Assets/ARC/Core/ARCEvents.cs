using System;
using UnityEngine;
using UnityEngine.Events;

namespace ARC
{
    // ARC Session Listener event
    [Serializable]
    public class ARCStartGameEvent : UnityEvent<ARCUser[]> { public ARCUser[] startingUsers; }

    // ARC Setup Manager events
    [Serializable]
    public class ARCImageEvent : UnityEvent<Texture2D> { public Texture2D texture; }

    [Serializable]
    public class ARCManifestEvent : UnityEvent<int[]> { public int[] array; }

    [Serializable]
    public class ARCChunkEvent: UnityEvent<int, float> { public int marker; public float progress; }

    [Serializable]
    public class ARCMarkerEvent : UnityEvent<int, Texture2D> { public int marker; public Texture2D texture; }

    [Serializable]
    public class ARCScanEvent : UnityEvent<int> { public int index; }

    // ARC User event
    [Serializable]
    public class ARCBoolEvent : UnityEvent<bool> { bool ready; }
}