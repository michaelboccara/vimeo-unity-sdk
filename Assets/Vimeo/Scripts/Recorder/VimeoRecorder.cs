﻿using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Vimeo;
using System;
using System.Collections;

namespace Vimeo.Recorder
{
    [AddComponentMenu("Video/Vimeo Recorder")]
    [HelpURL("https://github.com/vimeo/vimeo-unity-sdk")]
    public class VimeoRecorder : RecorderSettings
    {
        public delegate void RecordAction();
        public delegate void RecordActionMsg(string msg);
        public delegate void RecordActionVal(float val);
        public event RecordAction OnReady;
        public event RecordActionVal OnUploadProgress;
        public event RecordAction OnUploadComplete;
        public event RecordActionMsg OnUploadError;

        public VimeoPublisher publisher;

        public bool isReady     = false;
        public bool isRecording = false;
        public bool isUploading = false;
        public float uploadProgress = 0;

        VimeoFetcher fetcher;

        private int m_byteChunkSize = 1024 * 1024 * 128;
        public int byteChunkSize {
            set {
                m_byteChunkSize = value;
            }
        }

        public void Start()
        {
            if (encoder == null) {
                encoder = gameObject.AddComponent<EncoderManager>();
                encoder.Init(this);
            }

            if (recordOnStart) {
                BeginRecording();
            }
        }

        public void BeginRecording()
        {
            if (!isRecording) {
                encoder.BeginRecording();
                isRecording = true;
            }
        }

        // Used if you want to script when you want to call AddFrame
        public void BeginManualRecording()
        {
            BeginRecording();
            encoder.ManualFrameCapture();
        }

        public void EndRecording()
        {
            isRecording = false;
            encoder.EndRecording();

            if (autoUpload) {
                PublishVideo();
            } else {
                Debug.Log("[VimeoRecorder] Video did not automatically upload. VimeoRecorder.autoUpload is set to false.");
            }
        }

        public void Cancel()
        {
            isRecording = false;
            isUploading = false;
            if (encoder == null || publisher == null)
                return;
            //encoder.DeleteVideoFile();
            Destroy(publisher);
            encoder.CancelRecording();
        }

        public void FetchVideos()
        {
            if (fetcher == null)
            {
                isReady = false;
                fetcher = gameObject.AddComponent<VimeoFetcher>();
                fetcher.OnFetchComplete += OnFetchComplete;
                fetcher.OnFetchError += OnFetchError;
                fetcher.GetVideosInFolder();
            }
        }

        private void OnFetchError(string response)
        {
            DestroyFetcher();
        }

        private void OnFetchComplete(string response)
        {
            isReady = true;

            if (OnReady != null)
            {
                OnReady.Invoke();
            }

            DestroyFetcher();
        }

        private void DestroyFetcher()
        {
            if (fetcher != null)
            {
                Destroy(fetcher);
                fetcher = null;
            }
        }

        //Used if you want to publish the latest recorded video
        public void PublishVideo()
        {
            Debug.Assert(isReady);
            isUploading = true;
            uploadProgress = 0;

            if (publisher == null) {
                publisher = gameObject.AddComponent<VimeoPublisher>();
                publisher.Init(this, m_byteChunkSize);

                publisher.OnUploadProgress += UploadProgress;
                publisher.OnUploadError += UploadError;
            }

            if (replaceExisting)
            {
                if (!isReady)
                {
                    // bad situation - need some waiting point
                    Debug.LogWarning("Videos fetching is not ready for publishing with replacement - need to fetch project's videos list first");
                }

                if (currentVideo.id <= 0)
                {
                    if (!string.IsNullOrEmpty(videoName))
                    {
                        SetVimeoIdFromName();
                    }
                }

                publisher.PublishVideo(encoder.GetVideoFilePath(), currentVideo.id);
            }
            else
            {
                publisher.PublishVideo(encoder.GetVideoFilePath());
            }
        }

        internal IEnumerator WaitForReady()
        {
            while (!isReady)
            {
                yield return null;
            }
        }

        private void UploadProgress(string status, float progress)
        {
            uploadProgress = progress;

            if (status == "UploadComplete") {
                publisher.OnUploadProgress -= UploadProgress;
                publisher.OnUploadError -= UploadError;

                isUploading = false;
                encoder.DeleteVideoFile();
                Destroy(publisher);

                if (OnUploadComplete != null) {
                    OnUploadComplete();
                }
            }
            else if (status == "UploadError")
            {
                publisher.OnUploadProgress -= UploadProgress;
                publisher.OnUploadError -= UploadError;

                isUploading = false;
                encoder.DeleteVideoFile();
                Destroy(publisher);

                if (OnUploadError != null)
                {
                    OnUploadError(status);
                }
            }
            else
            {
                if (OnUploadProgress != null)
                {
                    OnUploadProgress(progress);
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
                OnUploadError(status);
            }
        }

        private void Dispose()
        {
            Cancel();
            Destroy(encoder);
            Destroy(fetcher);
        }

        void OnDisable()
        {
            Dispose();
        }

        void OnDestroy()
        {
            Dispose();
        }

        void LateUpdate()
        {
            if (encoder != null) {
                // Set recording state based upon VimeoRecorder state
                if (!isRecording && encoder.isRecording) {
                    isRecording = true;
                }
            }
        }
    }
}