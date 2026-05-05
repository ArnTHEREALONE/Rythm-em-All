using UnityEngine;
using System.IO;

public class EditorMusicImporter : MonoBehaviour
{
    [Header("=== Formats supportés ===")]
    public string[] supportedExtensions = { ".ogg", ".wav", ".mp3" };

    public void ImportMusic(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            Debug.LogError($"EditorMusicImporter: File not found: {sourcePath}");
            return;
        }

        string ext = Path.GetExtension(sourcePath).ToLower();
        bool supported = false;
        foreach (string validExt in supportedExtensions)
        {
            if (ext == validExt) { supported = true; break; }
        }

        if (!supported)
        {
            Debug.LogError($"EditorMusicImporter: Unsupported format: {ext}. Use .ogg, .wav, or .mp3.");
            return;
        }

        string destDir = Path.Combine(Application.persistentDataPath, "Music");
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(destDir, fileName);

        if (File.Exists(destPath))
        {
            Debug.LogWarning($"EditorMusicImporter: File already exists, overwriting: {fileName}");
        }

        File.Copy(sourcePath, destPath, true);
        Debug.Log($"EditorMusicImporter: Imported '{fileName}' to {destPath}");

        if (BeatMapEditor.Instance != null)
        {
            // Correction : rechercher l'entrée MusicEntry correspondante dans la bibliothèque musicale
            if (BeatMapEditor.Instance != null)
            {
                Debug.Log("EditorMusicImporter: Music imported. Please assign it in the MusicDatabase.");
            }
            else
            {
                Debug.LogWarning($"EditorMusicImporter: Impossible de trouver MusicEntry pour '{fileName}'");
            }
        }

        if (BeatMapEditor.Instance?.musicLibrary != null)
        {
            BeatMapEditor.Instance.musicLibrary.RefreshList();
        }
    }

    public void OpenMusicFolder()
    {
        string musicPath = Path.Combine(Application.persistentDataPath, "Music");
        if (!Directory.Exists(musicPath))
            Directory.CreateDirectory(musicPath);

        Application.OpenURL("file:///" + musicPath.Replace("\\", "/"));
    }
}
