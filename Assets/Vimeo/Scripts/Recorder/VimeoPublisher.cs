using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Vimeo;
using Vimeo.SimpleJSON;

namespace Vimeo.Recorder
{
    public class VimeoPublisher : MonoBehaviour
    {
        public delegate void UploadAction(string status, float progress);
        public event UploadAction OnUploadProgress;

        public delegate void RequestAction(string error_message);
        public event RequestAction OnUploadError;

        [HideInInspector] public RecorderSettings settings; // settings contains all the settings

        private VimeoUploader m_vimeoUploader;
        public VimeoUploader vimeoUploader
        {
            get
            {
                return m_vimeoUploader;
            }
        }
        public VimeoVideo video;

        private Coroutine saveCoroutine;
        private void Start()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }

        public void Init(RecorderSettings _settings, int _chunkSize = 1024 * 1024 * 128)
        {
            settings = _settings;

            if (m_vimeoUploader == null) {
                m_vimeoUploader = gameObject.AddComponent<VimeoUploader>();
                m_vimeoUploader.Init(settings.GetVimeoToken(), _chunkSize);

                m_vimeoUploader.OnUploadProgress += UploadProgress;
                m_vimeoUploader.OnUploadComplete += UploadComplete;
                m_vimeoUploader.OnUploadError += UploadError;
                m_vimeoUploader.OnNetworkError += NetworkError;
                m_vimeoUploader.OnRequestComplete += OnUploadInit;
                m_vimeoUploader.OnError += ApiError;
            }
        }

        public void OnUploadInit(string response)
        {
            m_vimeoUploader.OnRequestComplete -= OnUploadInit;
            m_vimeoUploader.OnRequestComplete += OnVideoUpdated;

            JSONNode jsonResponse = JSONNode.Parse(response);
            video = new VimeoVideo(jsonResponse);

#if UNITY_2018_1_OR_NEWER
            if (settings.defaultVideoInput == VideoInputType.Camera360) {
                m_vimeoUploader.SetVideoSpatialMode("equirectangular", settings.defaultRenderMode360 == RenderMode360.Stereo ? "top-bottom" : "mono");
            }
#endif

            if (string.IsNullOrEmpty(settings.description))
            {
                m_vimeoUploader.SetVideoDescription("Recorded and uploaded with the Vimeo Unity SDK: https://github.com/vimeo/vimeo-unity-sdk");
            }
            else
            {
            m_vimeoUploader.SetVideoDescription(settings.description);
            }

            if (settings.enableDownloads == false) {
                m_vimeoUploader.SetVideoDownload(settings.enableDownloads);
            }
            m_vimeoUploader.SetVideoComments(settings.commentMode);
            m_vimeoUploader.SetVideoReviewPage(settings.enableReviewPage);
            SetVideoName(settings.GetVideoName());

            if (settings.privacyMode == VimeoApi.PrivacyModeDisplay.OnlyPeopleWithAPassword) {
                m_vimeoUploader.SetVideoPassword(settings.videoPassword);
            }
            SetVideoPrivacyMode(settings.privacyMode);
        }

        public string GetVimeoPermalink()
        {
            if (settings.videoPermalink != null) {
                if (settings.defaultShareLink == LinkType.ReviewPage) {
                    return settings.videoReviewPermalink;
                } else {
                    return settings.videoPermalink;
                }
            }

            if (video != null && video.id != 0) {
                return "https://vimeo.com/" + video.id;
            }

            Debug.LogError("No vimeo video link found, try recording again");
            return null;
        }

        public void PublishVideo(string filename, int vimeoId = 0)
        {
            if (System.IO.File.Exists(filename)) {
                Debug.Log("[VimeoPublisher] Uploading to Vimeo");
                m_vimeoUploader.Upload(filename, vimeoId);
            } else {
                Debug.LogError("File doesn't exist, try recording it again");
            }
        }

        void UploadProgress(string status, float progress)
        {
            if (OnUploadProgress != null) {
                OnUploadProgress(status, progress);
            }
        }

        private void UploadComplete(string video_url)
        {
            if (settings.openInBrowser == true)
            {
                OpenVideo();
            }
            if (OnUploadProgress != null)
            {
                OnUploadProgress("UploadComplete", 1f);
            }

            Debug.Log("[VimeoPublisher] Uploaded video to " + video_url);
        }

        private void UploadError(string err)
        {
            if (OnUploadProgress != null) {
                OnUploadProgress("UploadError", 0f);
            }

            Debug.Log("[VimeoPublisher] Upload error: " + err);
        }

        private void OnVideoUpdated(string response)
        {
            m_vimeoUploader.OnRequestComplete -= OnVideoUpdated;

            JSONNode json = JSONNode.Parse(response);
            settings.videoPermalink = json["link"];
            settings.videoReviewPermalink = json["review_link"];

            if (settings.currentFolder != null && settings.currentFolder.uri != null) {
                m_vimeoUploader.AddVideoToFolder(video, settings.currentFolder);
            }
        }

        private void NetworkError(string error_message)
        {
            if (OnUploadError != null) {
                OnUploadError("It seems like you are not connected to the internet or are having connection problems.");
            }
        }

        private void ApiError(string response)
        {
            JSONNode json = JSONNode.Parse(response);

            if (!string.IsNullOrEmpty(json["error"]))
            {
                Debug.LogError("Vimeo Upload Error: " + json["error"]);
                Debug.LogError("Vimeo Upload Error: " + json["developer_message"]);
                if (OnUploadError != null)
                {
                    OnUploadError("Vimeo Upload Error " + json["error_code"] + ": " + json["error"]);
                }
            }
            else if (json["invalid_parameters"] != null) {
                for (int i = 0; i < json["invalid_parameters"].Count; i++) {
                    // TODO use .Value
                    if (json["invalid_parameters"][i]["field"].ToString() == "\"privacy.download\"") {
                        if (OnUploadError != null) {
                            OnUploadError("You must upgrade your Vimeo account in order to access this privacy feature. https://vimeo.com/upgrade");
                        }
                    } else if (json["invalid_parameters"][i]["field"].ToString() == "\"privacy.view\"") {
                        if (OnUploadError != null) {
                            OnUploadError("You must upgrade your Vimeo account in order to access this privacy feature. https://vimeo.com/upgrade");
                        }
                    } else {
                        if (OnUploadError != null) {
                            OnUploadError(json["invalid_parameters"][i]["field"] + ": " + json["invalid_parameters"][i]["error"]);
                        }
                    }
                }
            }
        }

        public void SetVideoName(string title)
        {
            if (title != null && title != "") {
                if (saveCoroutine != null) { StopCoroutine(saveCoroutine); } // DRY
                m_vimeoUploader.SetVideoName(title);
                saveCoroutine = StartCoroutine("SaveVideo");
            }
        }

        public void SetVideoPrivacyMode(VimeoApi.PrivacyModeDisplay mode)
        {
            if (saveCoroutine != null) { StopCoroutine(saveCoroutine); }
            m_vimeoUploader.SetVideoViewPrivacy(mode);
            saveCoroutine = StartCoroutine("SaveVideo");
        }

        private IEnumerator SaveVideo()
        {
            yield return new WaitForSeconds(1f);

            if (video != null) {
                m_vimeoUploader.SaveVideo(video);
            }
        }

        public void OpenVideo()
        {
            Application.OpenURL(GetVimeoPermalink());
        }

        public void OpenSettings()
        {
            Application.OpenURL("https://vimeo.com/" + video.id + "/settings");
        }

        void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(m_vimeoUploader);
        }
    }
}