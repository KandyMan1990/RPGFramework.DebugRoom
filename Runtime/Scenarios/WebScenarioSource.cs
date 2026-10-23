using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.DebugRoom
{
    /// <summary>
    /// A build's scenarios where its StreamingAssets is not a folder but a URL, as on Android, inside the package, and
    /// on the web: read by request, their names from the index the build wrote, since such a folder cannot be listed.
    /// </summary>
    internal sealed class WebScenarioSource : IScenarioSource
    {
        private const long HTTP_NOT_FOUND = 404L;

        private readonly string m_Url;

        internal WebScenarioSource(string url)
        {
            m_Url = url;
        }

        async Task<string[]> IScenarioSource.LoadNamesAsync()
        {
            byte[] index = await LoadAsync(m_Url + "/" + ScenarioPaths.INDEX, true);

            if (index == null)
            {
                return Array.Empty<string>();
            }

            return Encoding.UTF8.GetString(index).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        Task<byte[]> IScenarioSource.ReadAsync(string name)
        {
            return LoadAsync(m_Url + "/" + Uri.EscapeDataString(name) + ScenarioPaths.EXTENSION, false);
        }

        // A build made with no scenarios may have no index, which is none rather than a failure.
        private static async Task<byte[]> LoadAsync(string url, bool missingIsNone)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.data;
            }

            if (missingIsNone && request.responseCode == HTTP_NOT_FOUND)
            {
                return null;
            }

            throw new IOException($"{nameof(WebScenarioSource)}::{nameof(LoadAsync)} Status [{request.result}] code [{request.responseCode}] error [{request.error}] requesting [{url}]");
        }
    }
}
