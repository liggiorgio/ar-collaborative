using System;

namespace ARC
{
    // ARC Session Listener enum
    [Serializable]
    public enum ARCNetworkState
    {
        Init,
        Offline,
        Connecting,
        Connected,
        Disrupted
    };

    // ARC Session enums
    [Serializable]
    public enum ARCSessionState
    {
        Unavailable,
        Offline,
        Connecting,
        Setup,
        Lobby,
        Countdown,
        Playing
    }

    // ARC Entity enums
    [Serializable]
    public enum ARCPositionMode
    {
        CenterToView,
        KeepInitialOffset
    }

    [Serializable]
    public enum ARCRotationMode
    {
        KeepInitialRotation,
        UseViewRotation,
        ForkliftMode
    }


}