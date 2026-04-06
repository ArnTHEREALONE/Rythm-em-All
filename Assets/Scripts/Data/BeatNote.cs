using UnityEngine;

/// <summary>
/// Types d'input requis pour frapper un ennemi.
/// Détermine quel bouton le joueur doit utiliser.
/// </summary>
public enum EnemyInputType
{
    Any,        // Space — ennemi simple, n'importe quel input
    LeftOnly,   // Action gauche uniquement
    RightOnly,  // Action droite uniquement
    Both,       // Gauche + droite en même temps
    Spam        // Clics multiples rapides
}

/// <summary>
/// Représente une note/event dans la beatmap.
/// Chaque BeatNote correspond à un ennemi qui va spawn.
/// </summary>
[System.Serializable]
public class BeatNote
{
    [Tooltip("Temps en beats (ex: beat 4.5 = 4ème temps et demi)")]
    public float beatTime;

    [Tooltip("Index du spawner (= quelle lane)")]
    public int spawnerIndex;

    [Tooltip("Type d'input requis pour tuer cet ennemi")]
    public EnemyInputType inputType;

    public BeatNote() { }

    public BeatNote(float beatTime, int spawnerIndex, EnemyInputType inputType)
    {
        this.beatTime = beatTime;
        this.spawnerIndex = spawnerIndex;
        this.inputType = inputType;
    }
}
