using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Vimeo;
using System;
using UnityEngine.Events;

namespace Vimeo.Recorder
{
    [AddComponentMenu("Video/Vimeo Uploader")]
    [HelpURL("https://github.com/vimeo/vimeo-unity-sdk")]
    public class VimeoJustUploader : RecorderSettings
    {
        [Serializable]
        public class UnityEvent_Bool : UnityEvent<bool> { }
        public UnityEvent_Bool OnUploading;

        public delegate void UploadAction();
        public event UploadAction OnUploadComplete;
        public event UploadAction OnUploadError;

        public VimeoPublisher publisher;

        public string filePath;
        public bool isUploading = false;
        public float uploadProgress = 0;
        public string uploadStatus;
        private int m_byteChunkSize = 1024 * 1024 * 128;
        public int byteChunkSize
        {
            set
            {
                m_byteChunkSize = value;
            }
        }

        public void SetFilePath(string filePath)
        {
            this.filePath = filePath;
        }

        public void StartUploading()
        {
            isUploading = true;
            uploadProgress = 0;

            if (publisher == null) {
                publisher = gameObject.AddComponent<VimeoPublisher>();
                publisher.Init(this, m_byteChunkSize);

                publisher.OnUploadProgress += UploadProgress;
                publisher.OnUploadError += UploadError;
            }

            publisher.PublishVideo(filePath);
        }

        public void CancelUpload()
        {
            Destroy(publisher);
            uploadProgress = 0;
            uploadStatus = "Cancelled";
            isUploading = false;
        }

        private void UploadProgress(string status, float progress)
        {
            uploadProgress = progress;
            uploadStatus = status;

            if (status == "UploadComplete" || status == "UploadError") {
                publisher.OnUploadProgress -= UploadProgress;
                publisher.OnUploadError -= UploadError;

                isUploading = false;
                Destroy(publisher);

                if (status == "UploadComplete") {
                    if (OnUploadComplete != null) {
                        OnUploadComplete();
                    }
                } else if (status == "UploadError") {
                    if (OnUploadError != null) {
                        OnUploadError();
                    }
                }
            }


        }

        private void UploadError(string status)
        {
            Debug.LogError(status);
            publisher.OnUploadProgress -= UploadProgress;
            publisher.OnUploadError -= UploadError;

            isUploading = false;
            //encoder.DeleteVideoFile();
            Destroy(publisher);

            if (OnUploadError != null)
            {
                OnUploadError();
            }
        }

        private void Dispose()
        {
            Destroy(publisher);
        }

        void OnDisable()
        {
            Dispose();
        }

        void OnDestroy()
        {
            Dispose();
        }
    }
}