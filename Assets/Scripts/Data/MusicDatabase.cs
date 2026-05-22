using UnityEngine;
using System.Collections.Generic;





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






[CreateAssetMenu(fileName = "MusicDatabase", menuName = "Scriptable Objects/MusicDatabase")]
public class MusicDatabase : ScriptableObject
{
    [Tooltip("Liste de toutes les musiques disponibles dans le jeu")]
    public List<MusicEntry> entries = new List<MusicEntry>();

    
    
    
    public MusicEntry GetByEventPath(string path)
    {
        return entries.Find(e => e.fmodEventPath == path);
    }

    
    
    
    public MusicEntry GetByName(string displayName)
    {
        return entries.Find(e => e.displayName == displayName);
    }

    
    
    
    public MusicEntry GetByIndex(int index)
    {
        if (index >= 0 && index < entries.Count)
            return entries[index];
        return null;
    }
}
