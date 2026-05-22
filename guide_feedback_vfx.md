# 🎆 Guide Feedback Visuels — Setup pas à pas

Ce guide explique comment **créer et configurer** tous les effets visuels du jeu Rythm'em All dans Unity.

---

## Table des Matières

1. [Module 1 — Player VFX](#module-1--player-vfx)
2. [Module 2 — Arena VFX (Damier Disco)](#module-2--arena-vfx-damier-disco)
3. [Module 3 — Enemy VFX](#module-3--enemy-vfx)

---

## Module 1 — Player VFX

### Étape 1 — Attacher le script au Player

1. Ouvrir la scène **Game**
2. Sélectionner le **Player** dans la Hierarchy
3. `Add Component` → `PlayerVFX`
4. Le script détecte automatiquement `PlayerDash`, `PlayerCombat`, `PlayerHealth`, `PlayerMovement` et le `SpriteRenderer`

> [!IMPORTANT]
> Le joueur doit avoir un **SpriteRenderer** sur un **objet enfant**. C'est ce sprite qui sera flashé et déformé.

---

### Étape 2 — Créer le Particle System de déplacement (loop)

Ce ParticleSystem tourne en boucle et émet des paillettes quand le joueur se déplace.

1. Sélectionner le **Player** dans la Hierarchy
2. Clic droit → `Effects > Particle System` → renommer `MoveSparklePS`
3. Configurer le composant **Particle System** :

| Module | Paramètre | Valeur |
|---|---|---|
| **Main** | Start Lifetime | `0.4` |
| | Start Speed | `0.5` |
| | Start Size | `0.08` |
| | Start Color | Or pâle `#FFE699` alpha 50% |
| | Gravity Modifier | `-0.2` (flotte vers le haut) |
| | Simulation Space | `World` |
| | Max Particles | `100` |
| | Looping | ✅ |
| | Play On Awake | ❌ (le script gère l'activation) |
| **Emission** | Rate over Time | `30` |
| **Shape** | Shape | `Circle` |
| | Radius | `0.4` |
| **Color over Lifetime** | Enabled | ✅ |
| | Gradient | Alpha: 0% → 100% → 0% |
| **Renderer** | Render Mode | `Billboard` |
| | Material | `Particles/Standard Unlit` (créer un nouveau) |

4. Dans l'Inspector du **PlayerVFX**, glisser `MoveSparklePS` dans le champ **Move Sparkle Particles**

---

### Étape 3 — Créer le Particle System de dash (one-shot)

Ce ParticleSystem émet un burst de poussière au déclenchement du dash.

1. Sélectionner le **Player** dans la Hierarchy
2. Clic droit → `Effects > Particle System` → renommer `DashDustPS`
3. Configurer :

| Module | Paramètre | Valeur |
|---|---|---|
| **Main** | Start Lifetime | `0.5` |
| | Start Speed | `2` |
| | Start Size | `0.15` |
| | Start Color | Beige `#CCB280` alpha 60% |
| | Gravity Modifier | `0.3` |
| | Simulation Space | `World` |
| | Looping | ❌ |
| | Play On Awake | ❌ |
| **Emission** | Rate over Time | `0` |
| | Bursts → Add | Count: `12`, Time: `0` |
| **Shape** | Shape | `Sphere` |
| | Radius | `0.3` |
| **Renderer** | Render Mode | `Billboard` |
| | Material | `Particles/Standard Unlit` |

4. Dans l'Inspector du **PlayerVFX**, glisser `DashDustPS` dans le champ **Dash Dust Particles**

---

### Étape 4 — Créer les VFX Graph pour le heal

#### Heal Actif (on kill) — one-shot

1. `Assets > Create > Visual Effects > Visual Effect Graph` → nommer `HealActiveVFX`
2. Double-cliquer pour ouvrir le VFX Graph Editor
3. Créer un système simple :
   - **Initialize** : Capacity `30`, Lifetime `0.8`, Set Color `(0.3, 1, 0.5, 0.7)` vert lumineux
   - **Output** : Billboard, Set Size `0.12`
   - **Update** : Add Gravity `(0, 2, 0)` (flotte vers le haut)
   - **Spawn** : Single Burst, Count `20`
4. Dans la scène, créer un enfant du Player : `Effects > Visual Effect` → glisser le graph
5. Renommer l'objet `HealActiveVFX`
6. Cocher **Play On Awake** = ❌
7. Glisser le composant `Visual Effect` dans le champ **Heal Active VFX** du `PlayerVFX`

#### Heal Passif — loop

1. `Assets > Create > Visual Effects > Visual Effect Graph` → nommer `HealPassiveVFX`
2. Système subtil :
   - **Initialize** : Capacity `20`, Lifetime `1.0`, Set Color bleu clair `(0.7, 0.9, 1, 0.4)`
   - **Output** : Billboard, Set Size `0.06`
   - **Update** : Add Gravity `(0, 1, 0)` (flotte doucement)
   - **Spawn** : Constant Rate `8` per second
3. Même procédure : enfant du Player, renommer `HealPassiveVFX`, Play On Awake = ❌
4. Glisser dans le champ **Heal Passive VFX**

> [!TIP]
> Le VFX Graph est activé/désactivé automatiquement par le script selon que le joueur est en train de se soigner passivement.

---

### Étape 5 — Configurer les paramètres dans l'Inspector

Sélectionner le Player → composant `PlayerVFX` :

#### Flash

| Champ | Valeur | Description |
|---|---|---|
| `Dash Flash Color` | Blanc `#FFFFFF` | Flash au déclenchement du dash |
| `Attack Darken Color` | Gris foncé `#4D4D4D` | Le sprite s'assombrit brièvement à l'attaque |
| `Damage Flash Color` | Rouge `#FF2626` | Flash rouge quand le joueur prend des dégâts |
| `Hp Full Flash Color` | Vert `#33FF66` | Flash quand les PV atteignent le max |

#### Cercle de portée

| Champ | Valeur | Description |
|---|---|---|
| `Show Attack Range` | ✅ | Affiche le cercle de portée d'attaque |
| `Attack Range Color` | Rouge transparent `#FF4D4D26` | Couleur du cercle |
| `Attack Range Line Width` | `0.05` | Épaisseur |

#### Déformation du sprite

| Champ | Valeur | Description |
|---|---|---|
| `Max Stretch Z` | `1.4` | Le sprite s'allonge de 40% max dans la direction du mouvement |
| `Min Squash X` | `0.7` | Le sprite se tasse de 30% max latéralement |
| `Deform Max Speed` | `15` | Vitesse à laquelle la déformation atteint le max |
| `Sprite Rotation Speed` | `15` | Vitesse de rotation du sprite vers la direction de déplacement |

> [!NOTE]
> La **rotation du sprite** est purement cosmétique. Les contrôles du joueur restent identiques et instantanés.

---

### Étape 6 — Tester

| Action | Effet attendu |
|---|---|
| **Dash** | Flash blanc + traînée + poussière |
| **Dash en cooldown** | Barre bleue + vibration du sprite |
| **Dash prêt** | Barre flash blanc puis disparaît |
| **Attaque** | Sprite s'assombrit + onde de choc |
| **Se déplacer** | Paillettes + sprite s'allonge et tourne |
| **Tuer un ennemi** | Burst de VFX vert (heal actif) |
| **PV au max** | Flash vert |
| **Dégâts** | Flash rouge |

---

## Module 2 — Arena VFX (Damier Disco)

### Étape 1 — Créer le sol (Plane)

1. Dans la scène **Game**, clic droit → `3D Object > Plane`
2. Renommer `ArenaFloor`
3. Position : `(0, 0, 0)`
4. Scale : ajuster selon la taille de ton arène (ex: `(3, 1, 3)` pour une arène 30×30)

---

### Étape 2 — Créer le Shader Graph (Damier Pulsant)

1. `Assets > Create > Shader Graph > URP > Lit Shader Graph` → nommer `DamierDiscoShader`
2. Double-cliquer pour ouvrir le Shader Graph
3. Créer les propriétés suivantes (clique `+` dans le Blackboard) :

| Nom | Type | Default |
|---|---|---|
| `_BeatPhase` | Float | `0` |
| `_Color1` | Color | `#1A1A2E` (bleu foncé) |
| `_Color2` | Color | `#16213E` (bleu marine) |
| `_PulseColor` | Color | `#E94560` (rose vif) |
| `_PulseIntensity` | Float | `0.3` |
| `_TileSize` | Float | `4` |
| `_FlashColor` | Color | `#FF0000` (rouge) |
| `_FlashIntensity` | Float | `0` |

4. **Construire le graph** :

```
[Position (Object)] → [Split XZ]
    ↓ X                    ↓ Z
[Multiply (_TileSize)] [Multiply (_TileSize)]
    ↓                      ↓
[Floor]                [Floor]
    ↓                      ↓
[Add] ─────────────────────┘
    ↓
[Modulo 2]
    ↓
[Step 1] → checkerValue (0 ou 1)

[_BeatPhase] → [Frac] → beatFrac (0-1 chaque beat)
    ↓
[One Minus] → pulseFactor (1→0 chaque beat)
    ↓
[Multiply (_PulseIntensity)]
    ↓
[Multiply (checkerValue)] → pulse (cases en quinconce)

[Lerp (_Color1, _Color2, checkerValue)] → baseColor
[Lerp (baseColor, _PulseColor, pulse)] → pulsedColor
[Lerp (pulsedColor, _FlashColor, _FlashIntensity)] → finalColor

finalColor → Base Color (Fragment)
```

> [!IMPORTANT]
> Le `Frac` de `_BeatPhase` donne la fraction du beat (0→1 chaque beat). Multiplié par `checkerValue` (0 ou 1), seule une case sur deux pulse. Le `One Minus` fait que le pulse est fort au début du beat et s'éteint progressivement.

5. Sauvegarder le Shader Graph

---

### Étape 3 — Créer le Material

1. `Assets > Create > Material` → nommer `DamierDiscoMat`
2. Shader : sélectionner `Shader Graphs/DamierDiscoShader`
3. Configurer :
   - `_Color1` : bleu foncé `#1A1A2E`
   - `_Color2` : bleu marine `#16213E`
   - `_PulseColor` : rose vif `#E94560`
   - `_PulseIntensity` : `0.3`
   - `_TileSize` : `4` (taille des cases)
4. Glisser le material sur le `ArenaFloor`

---

### Étape 4 — Configurer ArenaVFX

1. Créer un **GameObject vide** dans la scène → renommer `ArenaVFXManager`
2. `Add Component` → `ArenaVFX`
3. Glisser les références :

| Champ | Objet à glisser |
|---|---|
| `Floor Renderer` | `ArenaFloor` (le Plane) |
| `Screen Flash Image` | Image UI fullscreen (voir ci-dessous) |
| `Target Camera` | La caméra principale |

4. **Créer l'image de flash** :
   - Dans le `GameCanvas`, clic droit → `UI > Image` → renommer `ScreenFlashOverlay`
   - Anchor : `Stretch` (ctrl+alt+shift sur le preset en bas à droite)
   - Color : rouge `#FF0000` avec alpha `0`
   - **Raycast Target** : ❌ (très important !)
   - Glisser dans le champ `Screen Flash Image`

---

### Étape 5 — Tester

- Le damier doit **pulser** une case sur deux au rythme du BPM
- Quand un ennemi s'auto-détruit → **flash rouge** des bords de l'écran + du damier + **secousse** caméra

---

## Module 3 — Enemy VFX

### Étape 1 — Timing Indicator (Fill)

Le fill est un **deuxième sprite** qui grandit depuis le centre de l'ennemi pour indiquer le moment d'attaquer.

#### Préparer le prefab ennemi

Pour **chaque prefab d'ennemi** :

1. Ouvrir le prefab (double-clic)
2. Clic droit sur le root → `2D Object > Sprite` → renommer `FillSprite`
3. Configurer :
   - **Sprite** : le même sprite que le fond (ex: carré blanc `UISprite`) ou un sprite custom
   - **Color** : rouge `#FF0000`
   - **Sorting Order** : `1` (au-dessus du fond)
   - **Position** : `(0, 0, 0)` centré
4. `Add Component` → `EnemyTimingIndicator` sur le **root** du prefab
5. Glisser `FillSprite` dans le champ **Fill Sprite**
6. Configurer :

| Champ | Valeur | Description |
|---|---|---|
| `Fill Beats Before Target` | `4` | Le fill commence 4 beats avant le timing |
| `Fill Color` | Rouge `#FF0000` | Couleur du remplissage |
| `Flash Color` | Blanc `#FFFFFF` | Flash au moment du beat parfait |
| `Flash Duration` | `0.1` | Durée du flash blanc |

> [!IMPORTANT]
> Le **Fill Sprite doit être un sprite personnalisable**. Tu peux utiliser n'importe quel sprite : carré, losange, étoile... Le script va le faire grandir de 0 à 1 en scale uniforme depuis le centre.

#### Comportement attendu

```
[4 beats avant]     [3 beats]     [2 beats]     [1 beat]     [TIMING !]
    ⬜               🟥 petit     🟥 moyen     🟥 grand      🟥⬜ FLASH!
    fond seul        fill 25%     fill 50%     fill 75%      fill 100%
```

Le joueur voit le fill rouge grandir → quand il couvre tout le fond vert = moment d'attaquer !

---

### Étape 2 — Particules de mort (Death by Player)

1. `Assets > Create > Prefab` (GameObject vide) → renommer `EnemyDeathParticlesPrefab`
2. Ajouter un composant `Particle System`
3. Configurer pour une **explosion géométrique** :

| Module | Paramètre | Valeur |
|---|---|---|
| **Main** | Start Lifetime | `0.6` |
| | Start Speed | `5` |
| | Start Size | `0.2` |
| | Start Color | Blanc (sera remplacé par la couleur de l'ennemi via script) |
| | Gravity Modifier | `0.5` |
| | Simulation Space | `World` |
| | Looping | ❌ |
| | Play On Awake | ✅ |
| **Emission** | Rate over Time | `0` |
| | Bursts | Count `15`, Time `0` |
| **Shape** | Shape | `Sphere` |
| | Radius | `0.1` |
| **Renderer** | Render Mode | `Mesh` |
| | Mesh | Cube ou autre forme géométrique |
| | Material | Un matériau unlit de couleur blanche |
| **Size over Lifetime** | Enabled | ✅ |
| | Curve | 100% → 0% (rétrécit) |

4. Ajouter un script `AutoDestroy` (petit script qui détruit l'objet après X secondes) ou utiliser `Stop Action > Destroy`
5. Sauvegarder le prefab

> [!TIP]
> Pour des formes **géométriques variées**, dans Renderer > Mesh, tu peux choisir Cube, Sphere, ou même un mesh custom. Le script colore automatiquement les particules avec la couleur de l'ennemi tué.

---

### Étape 3 — Particules d'auto-destruct

1. Dupliquer `EnemyDeathParticlesPrefab` → renommer `EnemyAutoDestructPrefab`
2. Modifier :
   - Start Color : Rouge
   - Start Speed : `3`
   - Burst Count : `20`
   - Ajouter un module **Noise** (intensité `1`, fréquence `5`) pour un look "glitch"

---

### Étape 4 — Configurer EnemyDeathVFX sur les prefabs

Pour **chaque prefab d'ennemi** :

1. `Add Component` → `EnemyDeathVFX`
2. Glisser les prefabs :

| Champ | Prefab à glisser |
|---|---|
| `Death By Player Particles Prefab` | `EnemyDeathParticlesPrefab` |
| `Auto Destruct Particles Prefab` | `EnemyAutoDestructPrefab` |

---

### Étape 5 — Tester les effets ennemis

| Action | Effet attendu |
|---|---|
| Ennemi approche du timing | Fill rouge grandit depuis le centre |
| Timing atteint | Flash blanc sur l'ennemi |
| Joueur tue l'ennemi | Explosion de particules de la couleur de l'ennemi + texte "Perfect"/"Good" |
| Ennemi s'auto-détruit | Explosion + flash rouge écran + flash rouge damier + secousse caméra + texte "Too Late" |

---

## Dépannage

| Problème | Solution |
|---|---|
| Le sprite ne flash pas | Vérifier que `playerSprite` est assigné (SpriteRenderer sur un enfant, pas Renderer) |
| L'onde de choc est invisible | Vérifier que `shockwaveDuration > 0` et que la couleur a de l'alpha |
| Le damier ne pulse pas | Vérifier que le shader a bien les propriétés `_BeatPhase`, `_FlashColor`, `_FlashIntensity` |
| Le fill indicator ne grandit pas | Vérifier que `fillSprite` est assigné et que `fillBeatsBeforeTarget > 0` |
| Les particules de mort n'apparaissent pas | Vérifier que les prefabs sont assignés sur `EnemyDeathVFX` et que `Play On Awake` est activé |
| Le screen flash ne s'affiche pas | Vérifier que `screenFlashImage` est assigné et que **Raycast Target** est ❌ |
| Le sprite ne se déforme pas | Vérifier qu'il y a un `Rigidbody` sur le Player et que `deformMaxSpeed > 0` |
