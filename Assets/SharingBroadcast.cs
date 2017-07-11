using System;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class SharingBroadcast : MonoBehaviour {

    [SerializeField]
    private GameObject qrcodePlane;

#if !UNITY_EDITOR
    private DatagramSocket socket;
    private static HostName broadcastHostName = new HostName("255.255.255.255");

    void Start () {
        socket = new DatagramSocket();
    }

    async void Update () {
        Vector3 cameraPosition = qrcodePlane.transform.InverseTransformPoint(Camera.main.transform.position);
        Quaternion cameraRotation = Quaternion.Inverse(qrcodePlane.transform.rotation) * Camera.main.transform.rotation;

        byte[] udpData = new byte[sizeof(float) * 7];
        Array.Copy(BitConverter.GetBytes(cameraPosition.x), 0, udpData, 0, 4);
        Array.Copy(BitConverter.GetBytes(cameraPosition.y), 0, udpData, 4, 4);
        Array.Copy(BitConverter.GetBytes(cameraPosition.z), 0, udpData, 8, 4);
        Array.Copy(BitConverter.GetBytes(cameraRotation.x), 0, udpData, 12, 4);
        Array.Copy(BitConverter.GetBytes(cameraRotation.y), 0, udpData, 16, 4);
        Array.Copy(BitConverter.GetBytes(cameraRotation.z), 0, udpData, 20, 4);
        Array.Copy(BitConverter.GetBytes(cameraRotation.w), 0, udpData, 24, 4);

        var stream = await socket.GetOutputStreamAsync(broadcastHostName, "3333");
        await stream.WriteAsync(udpData.AsBuffer());
        await stream.FlushAsync();
    }
#endif
}
