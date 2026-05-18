# 🎆 Guide Feedback Visuels — Setup pas à pas

Ce guide explique comment **créer et configurer** tous les effets visuels du jeu Rythm'em All dans Unity.

> [!IMPORTANT]
> Tous les effets du joueur (Module 1) sont **auto-générés par le script**. Tu n'as pas besoin de créer de prefabs manuellement ! Il suffit d'attacher le script et tout se crée au lancement.

---

## Table des Matières

1. [Module 1 — Player VFX](#module-1--player-vfx)
2. [Module 2 — Arena VFX (à venir)](#module-2--arena-vfx)
3. [Module 3 — Enemy VFX (à venir)](#module-3--enemy-vfx)
4. [Module 4 — UI In-Game Effects (à venir)](#module-4--ui-in-game-effects)

---

## Module 1 — Player VFX

### Étape 1 — Attacher le script au Player

1. Ouvrir la scène **Game**
2. Sélectionner le **Player** dans la Hierarchy
3. Cliquer `Add Component` → taper `PlayerVFX` → sélectionner
4. **C'est tout !** Le script détecte automatiquement `PlayerDash`, `PlayerCombat`, `PlayerHealth`, `PlayerMovement` et le `Renderer`

> [!TIP]
> Si ton joueur utilise un modèle 3D avec le Renderer sur un enfant, le script le trouvera automatiquement via `GetComponentInChildren<Renderer>()`.

---

### Étape 2 — Personnaliser les couleurs et durées dans l'Inspector

Sélectionne le Player et regarde le composant `PlayerVFX` dans l'Inspector. Voici les réglages par défaut et leur signification :

#### Dash — Flash

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Dash Flash Color` | Blanc `#FFFFFF` | Couleur du flash du joueur quand il dash |
| `Dash Flash Duration` | `0.06` | Durée du flash en secondes |

#### Dash — Trail

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Trail Start Color` | Blanc semi-transparent | Couleur au début de la traînée |
| `Trail End Color` | Blanc transparent | Couleur à la fin (disparition) |
| `Trail Time` | `0.2` | Durée de la traînée en secondes |
| `Trail Width` | `0.3` | Épaisseur de la traînée |

#### Dash — Dust (Poussière)

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Dust Color` | Beige `#CCB280` | Couleur des particules de poussière |
| `Dust Burst Count` | `12` | Nombre de particules émises |

#### Dash — Cooldown UI

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Cd Slider Color` | Bleu clair `#80CCFFcc` | Couleur de la barre de recharge |
| `Cd Ready Flash Color` | Blanc | Couleur du flash quand le dash est prêt |

#### Dash — Jitter (Vibration)

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Jitter Amplitude` | `0.03` | Intensité de la vibration |
| `Jitter Frequency` | `40` | Vitesse de la vibration |

#### Attack — Shockwave (Onde de choc)

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Shockwave Color` | Blanc semi-transparent | Couleur du cercle d'onde de choc |
| `Shockwave Duration` | `0.15` | Durée de l'expansion |

> [!NOTE]
> L'onde de choc utilise un **sprite cercle généré automatiquement** en forme d'anneau. Elle s'expand de 0 jusqu'à la portée d'attaque (`hitRange`) du joueur.

#### Movement — Sparkles (Paillettes)

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Sparkle Color` | Or pâle `#FFE699` | Couleur des paillettes |
| `Sparkle Rate Per Unit` | `8` | Densité de paillettes par unité de mouvement |

#### Heal — Active (on kill)

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Heal Active Color` | Vert lumineux `#4DFF80` | Couleur des particules de soin |
| `Heal Active Burst Count` | `20` | Nombre de particules |

#### Heal — Passive

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Heal Passive Color` | Bleu clair froid `#B3E6FF` | Couleur des particules de soin passif |

#### HP Full — Flash

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Hp Full Flash Color` | Vert vif `#33FF66` | Flash quand les PV sont au maximum |
| `Hp Full Flash Duration` | `0.15` | Durée du flash |

#### Damage — Flash

| Champ | Valeur par défaut | Description |
|---|---|---|
| `Damage Flash Color` | Rouge vif `#FF2626` | Flash quand le joueur subit des dégâts |
| `Damage Flash Duration` | `0.12` | Durée du flash |

---

### Étape 3 — Tester les effets

Lancer le jeu (`Play`) et vérifier chaque effet :

| Action | Effet attendu |
|---|---|
| **Dash** (touche Dash) | Flash blanc + traînée blanche + poussière au sol |
| **Dash en cooldown** | Barre bleue sous le joueur + vibration du modèle |
| **Dash prêt** | La barre flash en blanc puis disparaît |
| **Attaque** (clic gauche/droit) | Cercle d'onde de choc qui s'expand depuis le joueur |
| **Se déplacer** | Paillettes dorées émises au sol |
| **Tuer un ennemi** | Burst de particules vertes autour du joueur (soin) |
| **PV au maximum** | Flash vert du joueur |
| **Prendre des dégâts** | Flash rouge du joueur |

---

### Comment ça marche (architecture)

```
PlayerDash    ──► OnDashStarted ──┐
              ──► OnDashEnded  ──┤
              ──► OnDashReady  ──┤
                                  │
PlayerCombat  ──► OnAttackPerf ──┼──► PlayerVFX
                                  │     (souscrit à tout)
PlayerHealth  ──► OnDamageTaken──┤
              ──► OnHealActive ──┤
              ──► OnHPFull     ──┘
```

Le script `PlayerVFX` :
1. Se met automatiquement sur le même GameObject que le Player
2. Détecte les autres composants au démarrage
3. Souscrit à leurs événements
4. Crée **tous les effets procéduralement** (pas besoin de prefabs)

---

### Dépannage

| Problème | Solution |
|---|---|
| Pas de trail au dash | Vérifier que `Trail Width` > 0 et `Trail Time` > 0 |
| Pas d'onde de choc | Vérifier que `Shockwave Duration` > 0 et la couleur a de l'alpha |
| Le flash ne se voit pas | Vérifier que le `Renderer` du joueur est bien détecté (doit être sur le GO ou un enfant) |
| La barre de cooldown est invisible | C'est normal ! Elle n'apparaît que pendant le cooldown du dash |
| Le jitter est trop fort | Baisser `Jitter Amplitude` (essayer 0.01) |

---

## Module 2 — Arena VFX

*(À venir — damier pulsant, effets de bords d'écran)*

## Module 3 — Enemy VFX

*(À venir — explosions géométriques, interférence sur mort naturelle)*

## Module 4 — UI In-Game Effects

*(À venir — score fluide, couleur multiplicateur, vignette de vitesse)*
