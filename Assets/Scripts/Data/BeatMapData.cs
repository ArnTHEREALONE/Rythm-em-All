using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Structure sérialisable JSON pour une beatmap complète.
/// Référence un Event FMOD au lieu d'un fichier musique.
/// </summary>
[System.Serializable]
public class BeatMapData
{
    [Tooltip("Nom de la map (affiché dans le panneau de sélection)")]
    public string mapName;

    [Tooltip("Nom de la chanson (extrait du MusicDatabase)")]
    public string songName;

    [Tooltip("Chemin de l'event FMOD (ex: event:/Music/NeonRush)")]
    public string fmodEventPath;

    [Tooltip("Beats par minute de la chanson")]
    public float bpm;

    [Tooltip("Offset de démarrage en secondes (pour sync)")]
    public float songOffset;

    [Tooltip("Liste de toutes les notes/events de la beatmap")]
    public List<BeatNote> notes = new List<BeatNote>();

    /// <summary>
    /// Durée d'un beat en secondes pour ce BPM.
    /// </summary>
    public float SecondsPerBeat => 60f / bpm;

    /// <summary>
    /// Convertit un temps en beats vers un temps en secondes.
    /// </summary>
    public float BeatToSeconds(float beat)
    {
        return songOffset + beat * SecondsPerBeat;
    }

    /// <summary>
    /// Convertit un temps en secondes vers un temps en beats.
    /// </summary>
    public float SecondsToBeat(float seconds)
    {
        return (seconds - songOffset) / SecondsPerBeat;
    }

    /// <summary>
    /// Sérialise la beatmap en JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    /// <summary>
    /// Désérialise une beatmap depuis un JSON.
    /// </summary>
    public static BeatMapData FromJson(string json)
    {
        return JsonUtility.FromJson<BeatMapData>(json);
    }
}
