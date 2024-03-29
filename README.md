# AR Collaborative

**AR Collaborative** is a library for developing AR multi-user experiences or multiplayer games in Unity where people share the same physical space. A shared [AR Anchor](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.0/api/UnityEngine.XR.ARFoundation.ARAnchor.html) is placed based on photos of the environment, and state synchronisation occurs over LAN.

**AR Collaborative** can be used in a variety of scenarios: as a testing environment for a multi-user app, requiring a quick setup, before implementing more complex solutions; as a complete alternative to such solutions; as a fallback solution when cross-platform support is needed or when an internet connection is not viable.

## How it works

-   The session host takes photos of flat objects on a surface and shares them with other users. These will become AR markers in the physical world.
-   Then, all users scan the markers with their devices. Each device tracks the environment independently, but the images ensure there are Trackables in common among everyone.
-   When everyone is ready, users can create, grab, manipulate and remove objects in the shared AR space.

## Dependencies

**AR Collaborative** requires:

-   [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.0/manual/index.html) package
    -   [ARCore XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arcore@4.0/manual/index.html) (Android)
    -   [ARKit XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.arkit@4.0/manual/index.html) (iOS)
-   [Multiplayer High-Level API](https://docs.unity3d.com/Manual/UNetUsingHLAPI.html)
-   [CaptainsMess](https://github.com/hengineer/CaptainsMess) (requires HLAPI)

CaptainsMess is a library built on top of the HLAPI, and manages network discovery, single-scene network sessions, and one-tap connectivity and exposes network events in a friendly way.

## Adding ARC to a Unity project

To add the **AR Collaborative** library to a new or existing AR project, download and move *Assets/**ARC*** into your own Assets folder.

You can find extensive usage information for every Component and class in the [dedicated wiki](https://github.com/liggiorgio/ar-collaborative/wiki).

## Sample projects

The *Assets/Examples* folder contains some sample scenes with setups that show the main features of this library, which you can use as a reference:

-   **Hello ARC:** setting up a minimal ARC Session and spawning a 3D object that appears in the same physical place for every user, plus create and grab specific entities
-   **ARC PileUp:** users can stack some boxes on top of each other to create the highest stack possible
-   **ARC Sandbox:** a playground where users can spawn and manipulate primitives that stay in place or behave according to the laws of physics

