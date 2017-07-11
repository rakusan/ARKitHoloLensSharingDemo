using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking.Sockets;
#endif

public class SharingReceive : MonoBehaviour {

    [SerializeField]
    private GameObject qrcodePlane;

    [SerializeField]
    private GameObject avatarPrefab;

#if !UNITY_EDITOR
    private Hashtable devices = new Hashtable();
    private Dictionary<string, GameObject> avatars = new Dictionary<string, GameObject>();

    private DatagramSocket socket;

    private const int MAX_BUFFER_SIZE = 28;
    private byte[] buffer = new byte[MAX_BUFFER_SIZE];

    async void Start () {
        socket = new DatagramSocket();
        socket.MessageReceived += OnMessage;
        await socket.BindServiceNameAsync("3333");
    }

    void Update () {
        lock (devices.SyncRoot)
        {
            foreach (DictionaryEntry e in devices)
            {
                string address = (string)e.Key;
                object[] values = (object[])e.Value;
                Vector3 position = (Vector3)values[0];
                Quaternion rotation = (Quaternion)values[1];

                GameObject avatar;
                if (!avatars.TryGetValue(address, out avatar))
                {
                    avatar = Instantiate(avatarPrefab);
                    avatars[address] = avatar;
                }
                avatar.transform.position = qrcodePlane.transform.TransformPoint(position);
                avatar.transform.rotation = qrcodePlane.transform.rotation * rotation;
            }
        }
    }

    async void OnMessage(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        using (var stream = args.GetDataStream().AsStreamForRead())
        {
            await stream.ReadAsync(buffer, 0, MAX_BUFFER_SIZE);

            Vector3 cameraPosition = new Vector3(
                BitConverter.ToSingle(buffer, 0),
                BitConverter.ToSingle(buffer, 4),
                BitConverter.ToSingle(buffer, 8));

            Quaternion cameraRotation = new Quaternion(
                BitConverter.ToSingle(buffer, 12),
                BitConverter.ToSingle(buffer, 16),
                BitConverter.ToSingle(buffer, 20),
                BitConverter.ToSingle(buffer, 24));

            string address = args.RemoteAddress.ToString();
            object[] values = new object[] { cameraPosition, cameraRotation };
            lock (devices.SyncRoot)
            {
                devices[address] = values;
            }
        }
    }
#endif
}
