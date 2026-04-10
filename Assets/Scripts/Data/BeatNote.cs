using UnityEngine;

public enum EnemyInputType
{
    Any,        // Space — ennemi simple, n'importe quel input
    LeftOnly,   // Action gauche uniquement
    RightOnly,  // Action droite uniquement
    Both,       // Gauche + droite en même temps
    Spam        // Clics multiples rapides
}

[System.Serializable]
public class BeatNote
{
    public float beatTime;

    public int spawnerIndex;

    public EnemyInputType inputType;

    public BeatNote() { }

    public BeatNote(float beatTime, int spawnerIndex, EnemyInputType inputType)
    {
        this.beatTime = beatTime;
        this.spawnerIndex = spawnerIndex;
        this.inputType = inputType;
    }
}
