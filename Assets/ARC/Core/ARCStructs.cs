using System;
using UnityEngine.Events;

namespace ARC
{
    // ARC Setup Manager structs
    [Serializable]
    public struct ARCCapturePhaseEvents
    {
        public ARCImageEvent imageCaptured;
        public UnityEvent captureCompleted;
    }

    [Serializable]
    public struct ARCReceivingPhaseEvents
    {
        public ARCManifestEvent manifestReceived;
        public ARCChunkEvent chunkReceived;
        public ARCMarkerEvent markerReceived;
        public UnityEvent receivingCompleted;
    }

    [Serializable]
    public struct ARCScanningPhaseEvents
    {
        public ARCScanEvent markerScanned;
        public UnityEvent scanningComplete;
    }

    [Serializable]
    public struct ARCSessionEvents
    {
        public UnityEvent sessionAnchorCreated;
        public UnityEvent sessionAnchorMoved;
    }
}