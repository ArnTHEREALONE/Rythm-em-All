using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Entrée de musique dans la base de données.
/// Référence un Event FMOD + métadonnées pour le jeu et l'éditeur.
/// </summary>
[System.Serializable]
public class MusicEntry
{
    [Tooltip("Nom affiché de la chanson")]
    public string displayName;

    [Tooltip("Chemin de l'event FMOD (ex: event:/Music/NeonRush)")]
    public string fmodEventPath;

    [Tooltip("Référence FMOD Event — drag & drop depuis le FMOD Browser")]
    public FMODUnity.EventReference fmodEvent;

    [Tooltip("BPM de la chanson")]
    public float bpm = 120f;

    [Tooltip("Offset en secondes pour synchroniser le premier beat")]
    public float songOffset = 0f;

    [Tooltip("Image de couverture (optionnel)")]
    public Sprite coverArt;
}

/// <summary>
/// Base de données de toutes les musiques du projet.
/// ScriptableObject à remplir manuellement dans l'Inspector après avoir
/// créé les Events dans FMOD Studio.
/// </summary>
[CreateAssetMenu(fileName = "MusicDatabase", menuName = "Scriptable Objects/MusicDatabase")]
public class MusicDatabase : ScriptableObject
{
    [Tooltip("Liste de toutes les musiques disponibles dans le jeu")]
    public List<MusicEntry> entries = new List<MusicEntry>();

    /// <summary>
    /// Retourne une entrée par son chemin d'event FMOD.
    /// </summary>
    public MusicEntry GetByEventPath(string path)
    {
        return entries.Find(e => e.fmodEventPath == path);
    }

    /// <summary>
    /// Retourne une entrée par son nom d'affichage.
    /// </summary>
    public MusicEntry GetByName(string displayName)
    {
        return entries.Find(e => e.displayName == displayName);
    }

    /// <summary>
    /// Retourne une entrée par index.
    /// </summary>
    public MusicEntry GetByIndex(int index)
    {
        if (index >= 0 && index < entries.Count)
            return entries[index];
        return null;
    }
}
