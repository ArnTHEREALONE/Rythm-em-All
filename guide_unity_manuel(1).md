# 🎮 Guide Unity 6 + FMOD — Actions Manuelles dans l'Éditeur

Ce document liste **tout ce qu'il faut configurer à la main dans Unity 6 et FMOD Studio** pour Rythm'em All.

---

## Table des Matières

1. [Installer FMOD](#1--installer-fmod)
2. [Créer le Projet FMOD Studio](#2--créer-le-projet-fmod-studio)
3. [Création des Scènes](#3--création-des-scènes)
4. [Configuration de l'Input System](#4--configuration-de-linput-system)
5. [Scène Game — Setup](#5--scène-game--setup)
6. [Scène MainMenu — Setup](#6--scène-mainmenu--setup)
7. [Scène Editor — Setup](#7--scène-editor--setup)
8. [Prefabs](#8--prefabs)
9. [ScriptableObject Instances](#9--scriptableobject-instances)
10. [UI Canvas et Layout](#10--ui-canvas-et-layout)
11. [Build Settings](#11--build-settings)
12. [Project Settings](#12--project-settings)

---

## 1 — Installer FMOD

### 1.1 FMOD Studio (l'outil auteur)

> [!IMPORTANT]
> FMOD est **gratuit** pour les projets avec moins de $200k de revenus.

1. Aller sur **[fmod.com/download](https://www.fmod.com/download)**
2. Créer un compte (gratuit)
3. Télécharger **FMOD Studio** (version 2.02.x recommandée — la plus stable pour Unity 6)
4. Installer et lancer FMOD Studio

### 1.2 Plugin FMOD for Unity

> [!IMPORTANT]
> Le plugin FMOD doit être installé dans le projet Unity **AVANT** d'ouvrir le projet, sinon les scripts ne compileront pas.

**Méthode 1 — Asset Store (recommandée) :**
1. Dans Unity : `Window > Asset Store` (ou aller sur [assetstore.unity.com](https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-161631))
2. Chercher **"FMOD for Unity"**
3. **Acheter** (gratuit) → **Download** → **Import**
4. Lors de l'import : cocher **tout** et cliquer **Import**

**Méthode 2 — Téléchargement direct :**
1. Sur [fmod.com/download](https://www.fmod.com/download), section **"Unity Integration"**
2. Télécharger le `.unitypackage` pour la version 2.02.x
3. Dans Unity : `Assets > Import Package > Custom Package` → sélectionner le fichier
4. Cocher **tout** → **Import**

### 1.3 Configurer le Plugin dans Unity

Après l'import, un assistant de configuration apparaît :

1. **FMOD > Edit Settings** (menu principal Unity)
2. Dans l'Inspector `FMODStudioSettings` :

| Paramètre | Valeur |
|-----------|--------|
| **Bank Path** | `Assets/StreamingAssets` (par défaut, ne pas toucher) |
| **FMOD Studio Project Path** | Pointer vers ton fichier `.fspro` (voir section 2) |
| **Loading** | `Bank Load Type` = **Load All** (pour un petit projet) |

> [!TIP]
> Si le menu `FMOD` n'apparaît pas, re-démarrer Unity. Le plugin doit compiler d'abord.

---

## 2 — Créer le Projet FMOD Studio

### 2.1 Nouveau Projet

1. **Ouvrir FMOD Studio**
2. `File > New Project`
3. **Enregistrer** dans un dossier **à côté** du projet Unity (pas dedans !) :
   ```
   E:\arn\
   ├── Rythm-em-All\           ← Projet Unity
   └── Rythm-em-All_FMOD\      ← Projet FMOD Studio
       └── RythmEmAll.fspro    ← Fichier projet FMOD
   ```
4. Retourner dans Unity → `FMOD > Edit Settings` → **FMOD Studio Project Path** : naviguer jusqu'à `RythmEmAll.fspro`

### 2.2 Créer la Structure des Bus

Les Bus contrôlent les volumes. Cela se fait dans l'onglet **Mixer** de FMOD Studio :

1. Ouvrir l'onglet **Mixer** (en haut)
2. Le **Master Bus** (`bus:/`) existe déjà
3. **Clic droit** sur Master Bus → `Add Group Bus` → nommer **"Music"** → chemin = `bus:/Music`
4. **Clic droit** sur Master Bus → `Add Group Bus` → nommer **"SFX"** → chemin = `bus:/SFX`

La hiérarchie doit être :
```
bus:/                  (Master)
├── bus:/Music          (Musiques des niveaux)
└── bus:/SFX            (Tous les effets sonores)
```

### 2.3 Importer et Créer les Events Musique

Pour **chaque musique** du jeu :

1. **Glisser le fichier audio** (.ogg, .wav, .mp3) dans l'onglet **Audio Bin** (en bas de FMOD Studio)
   - Ou `File > Import Audio Files`
2. **Créer un dossier** dans l'onglet **Events** : clic droit → `New Folder` → nommer `Music`
3. Dans le dossier `Music`, **clic droit** → `New Event` → `2D Event`
4. **Nommer l'event** : par exemple `NeonRush`
   - Le chemin sera `event:/Music/NeonRush`
5. **Glisser le fichier audio** depuis l'Audio Bin vers la **Timeline** de l'event
6. **Important** : dans les propriétés de l'event, vérifier :
   - **Output** : sélectionner **Music** (le bus créé à l'étape 2.2)
   - **Polyphony** : 1 (une seule instance)
7. **Optionnel** : ajouter un **Tempo marker** dans la timeline :
   - Clic droit sur la timeline → `Add Tempo Marker`
   - Entrer le BPM de la chanson
   - Cela permettra à FMOD de fournir des informations de beat

8. **Répéter** pour chaque musique

### 2.4 Créer les Events SFX

Pour les effets sonores du gameplay :

1. Créer un dossier `SFX` dans Events
2. Pour chaque SFX, **clic droit** → `New Event` → `2D Event` :

| Nom Event | Chemin | Bus | Description |
|-----------|--------|-----|-------------|
| `HitPerfect` | `event:/SFX/HitPerfect` | SFX | Son satisfaisant, brillant |
| `HitGood` | `event:/SFX/HitGood` | SFX | Son neutre, acceptation |
| `HitMiss` | `event:/SFX/HitMiss` | SFX | Son buzzer / erreur |
| `TooSoon` | `event:/SFX/TooSoon` | SFX | Son d'alerte (trop tôt) |
| `TooLate` | `event:/SFX/TooLate` | SFX | Son doux / raté de justesse |
| `Warning` | `event:/SFX/Warning` | SFX | Son de warning pré-spawn |
| `Dash` | `event:/SFX/Dash` | SFX | Whoosh de dash |
| `EnemyExplode` | `event:/SFX/EnemyExplode` | SFX | Explosion ennemi |
| `MetronomeClick` | `event:/SFX/MetronomeClick` | SFX | Clic de métronome |
| `MetronomeAccent` | `event:/SFX/MetronomeAccent` | SFX | Clic accentué (temps fort) |

> [!TIP]
> Pour chaque event SFX :
> - Glisser un fichier audio dans la timeline
> - Passer le **Output** sur le bus **SFX**
> - Mettre la **Polyphony** à 4+ (peuvent superposer)

### 2.5 Build les Banks

> [!IMPORTANT]
> **À faire à chaque modification dans FMOD Studio !**

1. Dans FMOD Studio : `File > Build` (ou `F7`)
2. Vérifie que les fichiers `.bank` sont générés dans :
   ```
   E:\arn\Rythm-em-All\Assets\StreamingAssets\
   ├── Master.bank
   ├── Master.strings.bank
   └── (autres banks si tu en as créé)
   ```
3. **Retourner dans Unity** — les banks se chargent automatiquement

> [!WARNING]
> Si les banks ne se retrouvent pas dans `StreamingAssets`, vérifier dans FMOD Studio :
> - `Edit > Preferences > Build` → **Built Banks Output Directory** doit pointer vers `E:\arn\Rythm-em-All\Assets\StreamingAssets`
> - Ou configurer le path dans `FMOD > Edit Settings` dans Unity

### 2.6 Vérifier dans Unity

1. Ouvrir Unity
2. `FMOD > Event Browser` — tu devrais voir tous tes events :
   ```
   event:/Music/NeonRush
   event:/SFX/HitPerfect
   event:/SFX/Dash
   ...
   ```
3. Cliquer sur un event → bouton **Play** dans le browser pour tester

---

## 3 — Création des Scènes

> [!IMPORTANT]
> Créer 3 scènes dans `Assets/Scenes/` :

| Scène | Fichier | Comment |
|-------|---------|---------|
| Main Menu | `MainMenu.unity` | `File > New Scene > Basic (URP)` puis `Save As` |
| Game | `Game.unity` | `File > New Scene > Basic (URP)` puis `Save As` |
| Editor | `Editor.unity` | `File > New Scene > Basic (URP)` puis `Save As` |

- Tu peux supprimer `SampleScene.unity` une fois les nouvelles scènes prêtes.

---

## 4 — Configuration de l'Input System

> [!IMPORTANT]
> Modifier le fichier `InputSystem_Actions.inputactions` dans Unity.

### Actions à ajouter dans la Map "Player" :

| Action | Type | Bindings Clavier | Bindings Manette |
|--------|------|-------------------|-------------------|
| **AttackLeft** | Button | `Mouse/leftButton` | `Gamepad/leftTrigger` |
| **AttackRight** | Button | `Mouse/rightButton` | `Gamepad/rightTrigger` |
| **Dash** | Button | `Keyboard/space` | `Gamepad/buttonSouth` (A) |

### Comment faire :
1. Double-cliquer sur `InputSystem_Actions.inputactions` dans le Project
2. Dans la map **Player** :
   - Cliquer **+** à droite de "Actions" → nommer `AttackLeft`
     - Type : `Button`
     - Ajouter binding : `<Mouse>/leftButton` pour Keyboard&Mouse
     - Ajouter binding : `<Gamepad>/leftTrigger` pour Gamepad
   - Cliquer **+** → nommer `AttackRight`
     - Type : `Button`
     - Ajouter binding : `<Mouse>/rightButton` pour Keyboard&Mouse
     - Ajouter binding : `<Gamepad>/rightTrigger` pour Gamepad
   - Cliquer **+** → nommer `Dash`
     - Type : `Button`
     - Ajouter binding : `<Keyboard>/space` pour Keyboard&Mouse
     - Ajouter binding : `<Gamepad>/buttonSouth` pour Gamepad
3. **Supprimer** les actions inutiles : `Jump`, `Crouch`, `Sprint`, `Interact`, `Previous`, `Next`, `Look`
4. **Save Asset** (Ctrl+S dans la fenêtre Input Actions)

### Nouvelle Map "Editor" à créer :

| Action | Type | Bindings Clavier |
|--------|------|-------------------|
| **NavigateGrid** | Value (Vector2) | ZQSD (composite Dpad) |
| **PlaceSimple** | Button | `Keyboard/space` |
| **PlaceLeft** | Button | `Keyboard/a` |
| **PlaceRight** | Button | `Keyboard/e` |
| **PlaceBoth** | Button | `Keyboard/z` |
| **PlaceSpam** | Button | `Keyboard/s` |
| **PlayPause** | Button | `Keyboard/p` |
| **Delete** | Button | `Keyboard/delete` |

---

## 5 — Scène Game — Setup

### 5.1 Caméra
1. Sélectionner la **Main Camera** existante
2. **Position** : `(0, 15, 0)` — vue du dessus
3. **Rotation** : `(90, 0, 0)` — regarde vers le bas
4. **Projection** : `Orthographic`
5. **Size** : `12` (ajuster selon la taille de l'arène)

### 5.2 Arène (terrain de jeu)
1. `GameObject > 3D Object > Plane` → nommer **"Arena"**
2. **Position** : `(0, 0, 0)`
3. **Scale** : `(2, 1, 2)` — carré de 20×20 unités
4. Matériau : Créer un matériau URP avec une couleur sombre ou une texture
5. Layer : `Default`

### 5.3 Murs invisibles (optionnel)
Créer 4 cubes fins autour de l'arène :
- Désactiver leur `MeshRenderer`, garder le `BoxCollider`

| Mur | Position | Scale |
|-----|----------|-------|
| Haut | `(0, 0.5, 10)` | `(20, 1, 0.5)` |
| Bas | `(0, 0.5, -10)` | `(20, 1, 0.5)` |
| Gauche | `(-10, 0.5, 0)` | `(0.5, 1, 20)` |
| Droite | `(10, 0.5, 0)` | `(0.5, 1, 20)` |

### 5.4 Spawners

> [!IMPORTANT]
> Placer les spawners sur les bords de l'arène (8 minimum, 2 par côté).

Pour chaque spawner :

1. `GameObject > Create Empty` → nommer `Spawner_Top_0` etc.
2. Ajouter le script `EnemySpawner.cs`
3. Positionner **juste en dehors** de l'arène :

| Côté | Positions (2 par côté) | Direction (vers le centre) |
|------|-------------------------------|----------------------|
| **Haut** | `(-5, 0, 12)`, `(5, 0, 12)` | `(0, 0, -1)` |
| **Bas** | `(-5, 0, -12)`, `(5, 0, -12)` | `(0, 0, 1)` |
| **Gauche** | `(-12, 0, 5)`, `(-12, 0, -5)` | `(1, 0, 0)` |
| **Droite** | `(12, 0, 5)`, `(12, 0, -5)` | `(-1, 0, 0)` |

4. Dans l'Inspector :
   - `Spawner Index` : 0 à 7
   - `Spawn Direction` : la direction correspondante
5. Regrouper sous un parent vide `"Spawners"`

### 5.5 Player
1. `GameObject > 3D Object > Capsule` (ou sprite 2D)
2. **Position** : `(0, 0.5, 0)` — centre de l'arène
3. **Tag** : `Player`
4. **Composants à ajouter** :
   - `Rigidbody` → cocher **Freeze Rotation** (X, Y, Z) + **Freeze Position Y**
   - `CapsuleCollider`
   - `PlayerController.cs`
   - `PlayerMovement.cs`
   - `PlayerCombat.cs`
   - `PlayerDash.cs`
   - `PlayerHealth.cs`
5. **Références Inspector** :
   - `PlayerConfig` → drag le SO `PlayerConfig.asset`
   - `HP Slider` → drag le Slider UI (voir section UI)
   - **SFX FMOD (dans PlayerCombat)** :
     - `sfxPerfect` → `event:/SFX/HitPerfect` (glisser depuis FMOD Event Browser)
     - `sfxGood` → `event:/SFX/HitGood`
     - `sfxMiss` → `event:/SFX/HitMiss`
     - `sfxTooSoon` → `event:/SFX/TooSoon`
     - `sfxTooLate` → `event:/SFX/TooLate`
   - **SFX FMOD (dans PlayerDash)** :
     - `dashSFX` → `event:/SFX/Dash`

### 5.6 Managers (GameObjects vides)

> [!IMPORTANT]
> Plus besoin d'AudioSource sur aucun manager. FMOD gère tout.

| GameObject | Scripts à attacher |
|-----------|-------------------|
| **GameManager** | `GameManager.cs` |
| **FMODAudioManager** | `FMODAudioManager.cs` |
| **SpeedMultiplier** | `SpeedMultiplier.cs` |
| **ScoreManager** | `ScoreManager.cs` |
| **BeatManager** | `BeatManager.cs` |
| **EnemySpawnManager** | `EnemySpawnManager.cs` |

**Références à assigner dans GameManager :**
- `GameConfig` → drag `GameConfig.asset`
- `PlayerConfig` → drag `PlayerConfig.asset`
- `Player` → drag le GO Player
- `MusicDatabase` → drag `MusicDatabase.asset`

### 5.7 FMOD Studio Listener

> [!IMPORTANT]
> **Remplacer** l'`AudioListener` de la caméra par un `FMODUnity.StudioListener`.

1. Sélectionner la **Main Camera**
2. **Supprimer** le composant `AudioListener` (clic droit → Remove Component)
3. **Ajouter** le composant `FMOD Studio Listener` (`Add Component > FMOD > Studio Listener`)
4. **Attenuate** : décocher (pas d'atténuation 3D pour un jeu 2D top-down)

### 5.8 UI — Game HUD (Canvas)

```
GameCanvas (Canvas Scaler: Scale With Screen Size, 1920×1080)
├── HPBar (UI > Slider)
│   ├── Position : en bas au centre
│   ├── Width: 400, Height: 30
│   └── Background Color : gris foncé
├── ScoreText (TextMeshPro)
│   └── Position : en haut à droite
├── ComboText (TextMeshPro)
│   └── Position : en haut au centre
├── MultiplierText (TextMeshPro)
│   └── Position : sous le combo
├── SpeedText (TextMeshPro)
│   └── Position : en haut à gauche
├── TimingFeedbackText (TextMeshPro)
│   ├── Position : centre de l'écran
│   ├── Font Size : 72
│   └── Alpha = 0 par défaut
└── PauseMenu (Panel, désactivé par défaut)
    ├── ResumeButton
    ├── RestartButton
    └── QuitButton
```

Attacher `GameUI.cs` sur le Canvas ou un GO vide.

---

## 6 — Scène MainMenu — Setup

### 6.1 Caméra
- Laisser la caméra par défaut
- **Remplacer** `AudioListener` par `FMOD Studio Listener`

### 6.2 FMOD Audio Manager
- Ajouter un GO vide **FMODAudioManager** avec `FMODAudioManager.cs`
  - (ou le rendre `DontDestroyOnLoad` pour qu'il persiste entre les scènes)

### 6.3 UI Canvas

```
MainMenuCanvas
├── Background (UI > Image, couleur sombre)
├── TitleText (TextMeshPro, "Rythm'em All", 96pt)
├── ButtonPanel (Panel vertical layout)
│   ├── PlayButton ("PLAY")
│   ├── EditorButton ("EDITOR")
│   ├── OptionsButton ("OPTIONS")
│   └── QuitButton ("QUIT")
├── MapSelectPanel (désactivé par défaut)
│   ├── MapListScrollView (UI > Scroll View)
│   │   └── Content → rempli par MapSelectUI.cs
│   ├── MapInfoPanel
│   │   ├── SelectedMapName (TextMeshPro)
│   │   ├── SelectedMapBPM (TextMeshPro)
│   │   ├── SelectedMapNotes (TextMeshPro)
│   │   └── PlaySelectedButton
│   └── BackButton
└── OptionsPanel (désactivé par défaut)
    ├── MasterVolumeSlider + Label
    ├── MusicVolumeSlider + Label
    ├── SFXVolumeSlider + Label
    └── BackButton
```

### 6.4 Scripts
- `MainMenuUI.cs` sur le Canvas
- `MapSelectUI.cs` sur MapSelectPanel
- `OptionsUI.cs` sur OptionsPanel

### 6.5 EventSystem
- Vérifier qu'un `EventSystem` existe avec `InputSystemUIInputModule`

---

## 7 — Scène Editor — Setup

### 7.1 Managers

| GameObject | Script |
|-----------|--------|
| **EditorManager** | `BeatMapEditor.cs` |
| **FMODAudioManager** | `FMODAudioManager.cs` |

### 7.2 UI Canvas

```
EditorCanvas
├── TopBar (Panel horizontal)
│   ├── NewMapButton
│   ├── LoadMapButton
│   ├── SaveMapButton
│   ├── MusicLibraryButton      ← Ouvre la bibliothèque
│   └── BackToMenuButton
├── MusicInfoPanel
│   ├── MusicNameText (TextMeshPro)
│   ├── BPMInputField (InputField - TMP)
│   └── MetronomeToggle (Toggle)
├── TimelinePanel
│   ├── TimelineSlider (pleine largeur)
│   ├── CurrentBeatText (TextMeshPro)
│   ├── CurrentTimeText (TextMeshPro)
│   └── TransportControls (Panel horizontal)
│       ├── PrevHalfBeatButton ("<½")
│       ├── PrevBeatButton ("<<")
│       ├── PlayPauseButton ("▶/⏸")
│       ├── NextBeatButton (">>")
│       └── NextHalfBeatButton ("½>")
├── GridPanel (zone principale)
│   └── GridArea (RectTransform)
│       → Rendu par EditorGrid.cs
│       → Colonnes = lanes, Lignes = beats
│       → Notes = blocs colorés par type d'input
├── LaneLabels (Panel horizontal)
│   └── Un TextMeshPro par lane ("Top-0", "Right-1", etc.)
├── MusicLibraryPanel (désactivé par défaut)
│   ├── MusicListScrollView
│   │   └── Content → rempli par EditorMusicLibrary.cs
│   ├── SelectedMusicText (TextMeshPro)
│   └── SelectButton
└── StatusBar (en bas)
    └── StatusText ("Map loaded", "Saved!", etc.)
```

### 7.3 Scripts à attacher

| GO / Panel | Script | Références à assigner |
|-----------|--------|----------------------|
| EditorManager | `BeatMapEditor.cs` | timeline, grid, controls, metronome, musicLibrary, musicDatabase |
| TimelinePanel | `EditorTimeline.cs` | timelineSlider, BeatText, TimeText |
| GridPanel | `EditorGrid.cs` | gridArea |
| TransportControls | `EditorControls.cs` | boutons transport, bpmInputField, metronomeToggle |
| EditorManager | `EditorMetronome.cs` | metronomeClick → `event:/SFX/MetronomeClick`, metronomeAccent → `event:/SFX/MetronomeAccent` |
| MusicLibraryPanel | `EditorMusicLibrary.cs` | listContent, selectButton, selectedMusicText, **musicDatabase → drag MusicDatabase.asset** |

> [!IMPORTANT]
> **EditorMusicLibrary** a besoin d'une référence au **MusicDatabase** ScriptableObject.
> Glisser `MusicDatabase.asset` dans le champ `musicDatabase` dans l'Inspector.

### 7.4 FMOD Studio Listener
- Sur la Main Camera : remplacer `AudioListener` par `FMOD Studio Listener`

---

## 8 — Prefabs

### 8.1 Player Prefab
1. Ouvrir le prefab `Player.prefab` (ou le créer depuis le GO de la scène Game)
2. S'assurer que tous les composants de la section [5.5](#55-player) sont présents
3. Glisser les EventReference FMOD dans PlayerCombat et PlayerDash
4. `Apply All`

### 8.2 Enemy Prefabs
Créer `Assets/Prefabs/Enemies/` avec :

| Prefab | Visuel | Input Type |
|--------|--------|------------|
| `EnemyBase.prefab` | Cube neutre | Any |
| `EnemyLeft.prefab` | Teinté bleu | LeftOnly |
| `EnemyRight.prefab` | Teinté rouge | RightOnly |
| `EnemyBoth.prefab` | Teinté violet | Both |
| `EnemySpam.prefab` | Avec icône "×5" | Spam |

Pour chaque prefab :
1. `GameObject > 3D Object > Cube`
2. Scale : `(0.8, 0.8, 0.8)`
3. Composants :
   - `Rigidbody` → `Is Kinematic = true`
   - `BoxCollider` → `Is Trigger = true`
   - `EnemyBase.cs`
   - `EnemyMovement.cs`
4. Assigner l'`EnemyData` SO correspondant
5. Créer un child `"VulnerableEffect"` (particles) désactivé par défaut

### 8.3 Warning Indicator Prefab
- `Assets/Prefabs/UI/WarningIndicator.prefab`
- `SpriteRenderer` + `WarningIndicator.cs`

### 8.4 Timing Feedback Prefab
- `Assets/Prefabs/UI/TimingFeedback.prefab`
- Canvas World Space + TextMeshPro + `TimingFeedbackUI.cs`

---

## 9 — ScriptableObject Instances

> [!IMPORTANT]
> Créer les instances SO dans `Assets/Scriptable Objects/` via clic droit → `Create > Scriptable Objects > [Type]`

### 9.1 GameConfig

`Create > Scriptable Objects > GameConfig` → nommer `GameConfig.asset`

| Section | Champ | Valeur | Description |
|---------|-------|--------|-------------|
| **Score** | Base Score Per Kill | `100` | Score base par kill |
| | Perfect Score Multiplier | `2` | ×2 pour perfect |
| | Good Score Multiplier | `1` | ×1 pour good |
| | Too Late Score Multiplier | `0.5` | ×0.5 si tard |
| | Too Soon Score Multiplier | `0` | ×0 si tôt |
| **Combo** | Score Multiplier Base | `1` | Multiplicateur initial |
| | Score Multiplier Inc Perfect | `0.2` | +0.2 par perfect |
| | Score Multiplier Inc Good | `0.1` | +0.1 par good |
| | Score Multiplier Max | `10` | Cap multiplicateur |
| | Score Multiplier On Miss | `1` | Reset à 1 |
| **Vitesse** | Base Game Speed | `1` | Vitesse de base |
| | Speed Per Score Multiplier | `0.05` | +0.05 par palier |
| | Speed Soft Cap | `1.8` | Diminishing returns |
| | Speed Hard Cap | `2.5` | Max absolu |
| | Speed On Miss | `1` | Reset vitesse |
| **Timing** | Perfect Window | `0.05` | ±50ms perfect |
| | Good Window | `0.12` | ±120ms good |
| | Too Soon Window | `0.25` | Zone trop tôt |
| | Too Late Window | `0.25` | Zone trop tard |

### 9.2 PlayerConfig

`Create > Scriptable Objects > PlayerConfig` → nommer `PlayerConfig.asset`

| Section | Champ | Valeur | Description |
|---------|-------|--------|-------------|
| **Mouvement** | Move Speed | `8` | ×gameSpeed en jeu |
| **Dash** | Dash Distance | `5` | Distance fixe |
| | Dash Cooldown | `1` | ÷gameSpeed en jeu |
| **PV** | Max HP | `100` | Points de vie max |
| | Heal On Kill | `5` | Soin par kill |
| | Passive Heal Rate | `1` | PV/sec passif |
| | Passive Heal Delay | `3` | Délai après dégât |
| | Damage On Miss | `10` | Dégâts explosion |
| | Damage On Too Soon | `5` | Dégâts si trop tôt |

### 9.3 EnemyData (×5)

`Create > Scriptable Objects > EnemyData` → créer 5 instances :

| Asset | HP | Speed | Input | Spam Clicks |
|-------|----|----|-------|-------------|
| `Enemy_Simple.asset` | 1 | 3 | Any | — |
| `Enemy_Left.asset` | 1 | 3 | LeftOnly | — |
| `Enemy_Right.asset` | 1 | 3 | RightOnly | — |
| `Enemy_Both.asset` | 1 | 3 | Both | — |
| `Enemy_Spam.asset` | 5 | 2 | Spam | 5 clics, 1.0s |

### 9.4 MusicDatabase

> [!IMPORTANT]
> C'est le pont entre FMOD Studio et l'éditeur de beatmap.

`Create > Scriptable Objects > MusicDatabase` → nommer `MusicDatabase.asset`

Pour chaque musique :
1. Ouvrir `MusicDatabase.asset` dans l'Inspector
2. Cliquer **+** sous `Entries` pour ajouter une entrée
3. Remplir :

| Champ | Valeur | Comment |
|-------|--------|---------|
| Display Name | `"Neon Rush"` | Nom affiché dans le jeu |
| Fmod Event Path | `"event:/Music/NeonRush"` | Doit correspondre EXACTEMENT à l'event FMOD |
| Fmod Event | → Glisser depuis **FMOD Event Browser** | Drag & drop de l'event dans ce champ |
| BPM | `128` | BPM de la chanson |
| Song Offset | `0` | Offset si le beat ne commence pas au début |
| Cover Art | (optionnel) | Sprite de couverture |

> [!TIP]
> **Pour glisser un event FMOD** : ouvrir `FMOD > Event Browser`, trouver l'event, et le drag & drop dans le champ `Fmod Event` de l'Inspector.

---

## 10 — UI Canvas et Layout

### 10.1 Installer TextMeshPro
1. `Window > Package Manager` → vérifier que TextMeshPro est installé
2. Si popup "Import TMP Essential Resources" → **Import**

### 10.2 Fonts
- Importer une police stylisée (ex: "Rajdhani" ou "Orbitron" depuis Google Fonts)
- `Window > TextMeshPro > Font Asset Creator` → créer un TMP Font Asset

### 10.3 Palette de couleurs

| Usage | Hex | Description |
|-------|-----|-------------|
| Background | `#1a1a2e` | Bleu très sombre |
| Primary | `#e94560` | Rouge/rose vif |
| Perfect | `#FFD700` | Or |
| Good | `#00FF88` | Vert néon |
| Too Soon | `#FF4444` | Rouge |
| Too Late | `#FF8800` | Orange |

---

## 11 — Build Settings

1. `File > Build Settings`
2. Ajouter les 3 scènes :

| Index | Scène |
|-------|-------|
| 0 | `Scenes/MainMenu` |
| 1 | `Scenes/Game` |
| 2 | `Scenes/Editor` |

3. `MainMenu` doit être en index **0** (scène de démarrage)

> [!TIP]
> Les banks FMOD dans `StreamingAssets` sont automatiquement inclues dans le build.

---

## 12 — Project Settings

### 12.1 Tags et Layers

`Edit > Project Settings > Tags and Layers`

**Tags :** `Player`, `Enemy`, `Spawner`

**Layers :**

| Layer # | Nom |
|---------|-----|
| 8 | Player |
| 9 | Enemy |
| 10 | Spawner |

### 12.2 Physics
`Edit > Project Settings > Physics`

| Collision | Player | Enemy | Spawner |
|-----------|--------|-------|---------|
| Player | — | ✅ | ❌ |
| Enemy | ✅ | ❌ | ❌ |
| Spawner | ❌ | ❌ | ❌ |

### 12.3 Input System
`Edit > Project Settings > Player > Other Settings`
- **Active Input Handling** = `Input System Package (New)` ou `Both`

---

## Checklist ✅

```
[ ] FMOD Studio installé + projet créé
[ ] Plugin FMOD for Unity importé + configuré (Bank Path ok)
[ ] Bus créés dans FMOD (bus:/Music, bus:/SFX)
[ ] Events musique créés dans FMOD Studio
[ ] Events SFX créés (10 events minimum)
[ ] Banks buildées (F7) → fichiers .bank dans StreamingAssets
[ ] 3 scènes créées (MainMenu, Game, Editor) dans Build Settings
[ ] Input System : AttackLeft, AttackRight, Dash + map Editor
[ ] Tags (Player, Enemy, Spawner) + Layers + collision matrix
[ ] AudioListener remplacé par FMOD Studio Listener sur chaque caméra
[ ] TextMeshPro importé + font custom
[ ] Caméra Game : Orthographic, (0,15,0), rotation (90,0,0)
[ ] Arène + 8 Spawners positionnés
[ ] Player avec tous les scripts + EventReference FMOD assignés
[ ] 5 Enemy prefabs créés
[ ] ScriptableObjects : GameConfig, PlayerConfig, 5×EnemyData, MusicDatabase
[ ] MusicDatabase rempli avec les entrées correspondant aux events FMOD
[ ] Canvas UI pour chaque scène
[ ] FMODAudioManager GO dans chaque scène (ou DontDestroyOnLoad)
[ ] Références croisées assignées dans les Inspectors
```
