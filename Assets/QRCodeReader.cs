using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using HoloToolkit.Unity.InputModule;
using System;

public class QRCodeReader : MonoBehaviour, IInputClickHandler
{
    private PhotoCapture photoCaptureObject;
    private bool photoModeStarted = false;

    private GameObject qrcodePlane;
    private GameObject plane;

    void Start () {
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
        qrcodePlane = transform.Find("QRCodePlane").gameObject;
        plane = transform.Find("QRCodePlane/Plane").gameObject;
    }

    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            photoModeStarted = true;
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
#if !UNITY_EDITOR
        if (result.success)
        {
            List<byte> imageBufferList = new List<byte>();
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            int imageWidth = cameraResolution.width;
            int imageHeight = cameraResolution.height;

            ZXing.BarcodeReader qrReader = new ZXing.BarcodeReader();
            var qrResult = qrReader.Decode(imageBufferList.ToArray(), imageWidth, imageHeight, ZXing.BitmapFormat.RGBA32);

            if (qrReader == null)
            {
                Debug.Log("error: BarcodeReader.Decode");
                return;
            }

            Debug.Log(qrResult.Text);

            Matrix4x4 projectionMat;
            if (!photoCaptureFrame.TryGetProjectionMatrix(out projectionMat))
            {
                Debug.Log("error: PhotoCaptureFrame.TryGetProjectionMatrix");
                return;
            }

            Matrix4x4 cameraToWorldMat;
            if (!photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMat))
            {
                Debug.Log("error: PhotoCaptureFrame.TryGetCameraToWorldMatrix");
                return;
            }

            if (qrResult.ResultPoints.Length < 3)
            {
                Debug.Log("error: too few ResultPoints");
                return;
            }

            Vector3[] points = new Vector3[3];

            for (int i = 0; i < 3; ++i)
            {
                var pixelPos = new Vector2(qrResult.ResultPoints[i].X, qrResult.ResultPoints[i].Y);
                var imagePosZeroToOne = new Vector2(pixelPos.x / imageWidth, 1 - (pixelPos.y / imageHeight));
                var imagePosProjected = (imagePosZeroToOne * 2) - new Vector2(1, 1);    // -1 to 1 space
                var cameraSpacePos = UnProjectVector(projectionMat, new Vector3(imagePosProjected.x, imagePosProjected.y, 1));
                var worldSpaceRayPoint1 = cameraToWorldMat.MultiplyPoint(Vector3.zero);     // camera location in world space
                var worldSpaceRayPoint2 = cameraToWorldMat.MultiplyPoint(cameraSpacePos);   // ray point in world space

                RaycastHit hit;
                if (!Physics.Raycast(worldSpaceRayPoint1, worldSpaceRayPoint2 - worldSpaceRayPoint1, out hit, 5, 1 << 31))
                {
                    Debug.Log("error: Physics.Raycast failed");
                    return;
                }

                points[i] = hit.point;
            }

            var worldTopLeft = points[1];
            var worldTopRight = points[2];
            var worldBottomLeft = points[0];

            var bottomToTop = worldTopLeft - worldBottomLeft;
            var leftToRight = worldTopRight - worldTopLeft;

            qrcodePlane.transform.forward = bottomToTop;
            qrcodePlane.transform.position = worldBottomLeft + (bottomToTop + leftToRight) * 0.5f;
            plane.transform.localScale = new Vector3(leftToRight.magnitude, 1, bottomToTop.magnitude);

            //photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
#endif
    }

    private Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
    {
        Vector3 from = new Vector3(0, 0, 0);
        var axsX = proj.GetRow(0);
        var axsY = proj.GetRow(1);
        var axsZ = proj.GetRow(2);
        from.z = to.z / axsZ.z;
        from.y = (to.y - (from.z * axsY.z)) / axsY.y;
        from.x = (to.x - (from.z * axsX.z)) / axsX.x;
        return from;
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (photoModeStarted)
        {
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
    }
}
