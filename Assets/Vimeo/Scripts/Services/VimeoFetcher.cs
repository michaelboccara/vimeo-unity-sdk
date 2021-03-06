﻿using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Vimeo.Player;
using Vimeo.Recorder;
using Vimeo.SimpleJSON;

namespace Vimeo
{
    [RequireComponent(typeof(VimeoSettings))]
    public class VimeoFetcher : MonoBehaviour
    {
        internal VimeoSettings target { get { return GetComponent<VimeoSettings>(); } }
        VimeoApi api;

        public delegate void FetchAction(string response);
        public event FetchAction OnFetchComplete;
        public event FetchAction OnFetchError;

        private void Start()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }

        internal IEnumerator FetchFolders()
        {
            InitAPI();
            yield return null; // wait a frame to call VimeoApi.Start
            GetFolders();
            while (api != null)
                yield return null;
        }

        public void GetFolders()
        {
            var settings = target as VimeoSettings;
            if (!settings.Authenticated()) return;

            InitAPI();
            settings.vimeoFolders.Clear();
            settings.vimeoFolders.Add(
                new VimeoFolder("Loading...", null, VimeoFolder.Collection.Undefined)
            );

            api.OnRequestComplete += GetFoldersComplete;
            api.OnError += OnRequestError;
            api.GetUserFolders(settings.currentFolderType);
        }

        internal IEnumerator FetchVideosInFolder()
        {
            InitAPI();
            yield return null; // wait a frame to call VimeoApi.Start
            GetVideosInFolder();
            while (api != null)
                yield return null;
        }

        public void GetVideosInFolder()
        {
            var settings = target as VimeoSettings;
            if (!settings.Authenticated()) return;
            InitAPI();

            settings.vimeoVideos.Clear();
            settings.vimeoVideos.Add(
                new VimeoVideo("Loading...", null)
            );

            api.OnRequestComplete += GetVideosComplete;
            api.OnError += OnRequestError;

            api.GetVideosInFolder(settings.currentFolder, "name,uri,description"); // conserve description
        }

        internal IEnumerator FetchRecentVideos()
        {
            InitAPI();
            yield return null; // wait a frame to call VimeoApi.Start
            GetRecentVideos();
            while (api != null)
                yield return null;
        }

        public void GetRecentVideos()
        {
            var settings = target as VimeoSettings;
            if (!settings.Authenticated()) return;
            InitAPI();

            settings.vimeoVideos.Clear();
            settings.vimeoVideos.Add(
                new VimeoVideo("Loading...", null)
            );

            api.OnRequestComplete += GetVideosComplete;
            api.OnError += OnRequestError;

            api.GetRecentUserVideos();
        }

        private void InitAPI()
        {
            var settings = target as VimeoSettings;
            if (api == null)
            {
                if (settings.gameObject.GetComponent<VimeoApi>())
                {
                    api = settings.gameObject.GetComponent<VimeoApi>();
                }
                else
                {
                    api = settings.gameObject.AddComponent<VimeoApi>();
                }
            }

            api.token = settings.GetVimeoToken();
        }

        protected bool IsSelectExisting(VimeoSettings settings)
        {
            return (settings is VimeoPlayer) ||
                (settings is VimeoRecorder && (settings as VimeoRecorder).replaceExisting);
        }

        private void GetVideosComplete(string response)
        {
            var settings = target as VimeoSettings;
            settings.vimeoVideos.Clear();

            api.OnRequestComplete -= GetVideosComplete;
            api.OnError -= OnRequestError;

            Destroy(api);
            api = null;

            var json = JSONNode.Parse(response);
            JSONNode videoData = json["data"];

            if (videoData.Count == 0) {
                settings.vimeoVideos.Add(new VimeoVideo("(No videos found)"));
            }
            else {
                settings.vimeoVideos.Add(new VimeoVideo("---- Select a video ----", null));

                for (int i = 0; i < videoData.Count; i++) {
                    settings.vimeoVideos.Add(
                        new VimeoVideo(videoData[i])
                    );
                }
            }

            Debug.Log("[VimeoFetcher] Completed with " + (settings.vimeoVideos.Count - 1) + " existing videos");

            if (OnFetchComplete != null)
            {
                OnFetchComplete.Invoke(response);
            }
        }

        private void OnRequestError(string error)
        {
            var settings = target as VimeoSettings;

            Destroy(api);
            api = null;

            settings.signInError = true;

            if (OnFetchError != null)
            {
                OnFetchError.Invoke("");
            }

            Debug.LogError("[VimeoFetcher] Error: " + error);
        }

        private void GetFoldersComplete(string response)
        {
            var settings = target as VimeoSettings;
            settings.vimeoFolders.Clear();
            settings.currentFolder = null;

            api.OnRequestComplete -= GetFoldersComplete;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
#endif
            {
                DestroyImmediate(settings.gameObject.GetComponent<VimeoApi>());
            }

            var json = JSONNode.Parse(response);
            var folderData = json["data"];

            string folder_prefix = "";

            string currentFolderType_LowerCase = settings.currentFolderType.ToString().ToLower();

            if (IsSelectExisting(settings))
            {
                target.vimeoFolders.Add(new VimeoFolder("---- Select a folder ----", null, VimeoFolder.Collection.Undefined));
                target.vimeoFolders.Add(new VimeoFolder("Get video by ID or URL", "custom", VimeoFolder.Collection.Undefined));
                target.vimeoFolders.Add(new VimeoFolder("Most recent videos", "recent", VimeoFolder.Collection.Undefined));

                if (target.currentFolder == null || !target.currentFolder.IsValid())
                {
                    if (target.currentVideo != null && target.currentVideo.id > 0)
                    {
                        target.currentFolder = target.vimeoFolders[1];
                    }
                    else
                    {
                        target.currentFolder = target.vimeoFolders[0];
                    }
                }

                folder_prefix = target.currentFolderType.ToString() + " / ";
            }
            else if (folderData.Count == 0)
            {
                settings.vimeoFolders.Add(new VimeoFolder("No " + currentFolderType_LowerCase, null, VimeoFolder.Collection.Undefined));
            }
            else
            {
                settings.vimeoFolders.Add(new VimeoFolder("---- Select a " + currentFolderType_LowerCase + " ----", null, VimeoFolder.Collection.Undefined));
            }

            for (int i = 0; i < folderData.Count; i++)
            {
                VimeoFolder folder = new VimeoFolder(folder_prefix + folderData[i]["name"], folderData[i]["uri"], target.currentFolderType);
                settings.vimeoFolders.Add(folder);
            }

            if (OnFetchComplete != null)
            {
                OnFetchComplete.Invoke("");
            }

            Debug.Log("[VimeoFetcher] Completed with " + (settings.vimeoFolders.Count - 1) + " existing folders");
        }


    }
}
