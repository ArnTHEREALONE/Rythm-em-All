using UnityEngine;
using System.IO;

/// <summary>
/// Import de musique depuis le système de fichiers vers persistentDataPath/Music/.
/// </summary>
public class EditorMusicImporter : MonoBehaviour
{
    [Header("=== Formats supportés ===")]
    public string[] supportedExtensions = { ".ogg", ".wav", ".mp3" };

    /// <summary>
    /// Ouvre un dialogue de sélection de fichier et copie la musique dans le dossier du jeu.
    /// Note : Sur Windows, utilise System.Windows.Forms ou un plugin.
    /// En attendant, copie depuis un chemin donné.
    /// </summary>
    public void ImportMusic(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            Debug.LogError($"EditorMusicImporter: File not found: {sourcePath}");
            return;
        }

        // Vérifier l'extension
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

        // Créer le dossier de destination
        string destDir = Path.Combine(Application.persistentDataPath, "Music");
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Copier le fichier
        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(destDir, fileName);

        if (File.Exists(destPath))
        {
            Debug.LogWarning($"EditorMusicImporter: File already exists, overwriting: {fileName}");
        }

        File.Copy(sourcePath, destPath, true);
        Debug.Log($"EditorMusicImporter: Imported '{fileName}' to {destPath}");

        // Sélectionner la musique dans l'éditeur
        if (BeatMapEditor.Instance != null)
        {
            BeatMapEditor.Instance.SelectMusic(fileName);
        }

        // Rafraîchir la bibliothèque
        if (BeatMapEditor.Instance?.musicLibrary != null)
        {
            BeatMapEditor.Instance.musicLibrary.RefreshList();
        }
    }

    /// <summary>
    /// Ouvre le dossier musique dans l'explorateur de fichiers.
    /// </summary>
    public void OpenMusicFolder()
    {
        string musicPath = Path.Combine(Application.persistentDataPath, "Music");
        if (!Directory.Exists(musicPath))
            Directory.CreateDirectory(musicPath);

        Application.OpenURL("file:///" + musicPath.Replace("\\", "/"));
    }
}
