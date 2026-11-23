# A002GutierrezAaronPlataformes2D – Ginebró Quest

Activitat 2 de Unity (DAM2): joc de plataformes 2D per a l'empresa fictícia **GINEBRÓ GAMES**.
Punt de partida: el projecte base de classe [`mphc028/UnityPlatformerExample`](https://github.com/mphc028/UnityPlatformerExample).

Unity **6000.6.0f1** (URP 2D) · Exportació: **PC (Windows x64)** · Control: **teclat**

## Controls

| Acció | Tecla |
|---|---|
| Moure's | A / D o ← / → |
| Saltar / doble salt | Espai (dues vegades) |
| Pausa (continuar, reiniciar, menú) | Esc |
| Menús | Ratolí, o fletxes + Enter |

## Escenes (Build Settings, en aquest ordre)

| # | Escena | Contingut |
|---|---|---|
| 0 | `Splash` | Logo de **GINEBRÓ GAMES** (emblema amb una branca de ginebre + nom). Passa sol al menú (qualsevol tecla el salta). |
| 1 | `MainMenu` | Botons **Play**, **Credits** i **Quit**. |
| 2 | `Credits` | Nom del creador i botó **Back to Menu** (també Esc). |
| 3 | `Level1` | Nivell senzill: **5 cargols** (enemics normals). |
| 4 | `Level2` | Nivell més complex (zona vertical, pou i illes flotants): **5 cargols + 5 abelles voladores**. |
| 5 | `LevelFinal` | Nivell més curt amb enemics lliures (cargols i abelles) i l'arena del **Boss** (*The Boar King*). |

Els nivells s'enllacen amb un **portal** al final (Level1 → Level2 → LevelFinal). En guanyar el boss → Crèdits.

## Mecàniques

- **Barra de vida** (6 punts) a dalt a l'esquerra, amb color segons la vida i el valor "5 / 6".
- **Enemics**: fan mal en tocar el jugador (empenta + uns instants d'invulnerabilitat amb parpelleig).
  El jugador els elimina **saltant-los a sobre** (només compta si ve des de dalt).
- **Monedes** amb comptador a la UI ("x 12"). També hi ha cors que recuperen vida.
- **Boss**: 3 vides (cors a la part superior de la pantalla), treu **2 punts de vida** (els normals en treuen 1),
  carrega contra el jugador després d'avisar tremolant i es torna més ràpid a cada cop.
- En eliminar el boss apareix **"YOU WON!"** i, al cap de **10 segons** (compte enrere a pantalla), s'obre l'escena de **Crèdits**.
- Caure al buit o quedar-se sense vida reinicia el nivell.
- **So i música**: música de menú, de nivell i del boss (en bucle, sense tallar-se entre escenes que comparteixen tema) i
  efectes de salt, doble salt, moneda, trepig, cop, cop al boss, boss derrotat, mort, victòria, portal i botons.

## Estructura del projecte

```
Assets/
  Art/Generated/        Logo, monedes, cors, portal i panell UI (pixel art generat per codi)
  Audio/Music, Sfx/     Música i efectes (WAV generats per codi)
  Editor/               GameBuilder, ArtGenerator, UiFactory, AssetRenamer, GameVerifier
  Levels/               Mapes de text dels nivells (Level1.txt, Level2.txt, LevelFinal.txt)
  Prefabs/Enemies       Snail, Bee, BoarKing
  Prefabs/Pickups       Coin, HeartPickup
  Prefabs/Level, UI     Portal, GinebroGamesLogo
  Resources/            AudioManager (es carrega sol i persisteix entre escenes)
  Scenes/               Les 6 escenes + Templates/LevelTemplate (escena base de classe)
  Scripts/
    Audio/              AudioManager, SceneMusic
    Core/               SceneNames, SceneLoader (canvi d'escena amb fos a negre)
    Enemies/            Enemy (base), PatrolEnemy, FlyingEnemy, BossEnemy
    Level/              Coin, HealthPickup, KillZone, BossFightController
    Player/             PlayerHealth, PlayerWallet, PlayerContactSensor
    Platformer2DSystem/ Sistema de moviment del projecte base (PlayerController, Actor, Jumper…)
    UI/                 HealthBar, CoinCounter, BossHealthBar, MainMenu, CreditsMenu, SplashScreen, PauseMenu…
    Visual/             SpriteAnimator
Tools/
  Audio/GenerateAudio.py     Genera tots els sons i la música
  Levels/GenerateLevels.py   Genera els mapes de text dels nivells
Captures/                    Captures de les escenes i resultat de la prova automàtica
```

**Nomenclatura PascalCase**: tots els assets, carpetes, escenes, prefabs i GameObjects fan servir PascalCase.
`AssetRenamer` (menú *Ginebro > 1*) va renombrar els assets del projecte base i del pack de sprites
(p. ex. `walk-Sheet.png` → `WalkSheet.png`, `tileset_118` → `Tileset118`) amb `AssetDatabase.RenameAsset`,
de manera que no s'ha trencat cap referència. Només queda fora la carpeta `TextMesh Pro`, que és del paquet de Unity.

## Com es construeix

Tot el contingut es pot regenerar des del menú **Ginebro** de l'editor:

1. `Rename Assets To PascalCase`
2. `Build Game Content` – configura sprites i sons, genera l'art i els prefabs, i crea les 6 escenes. Els nivells es
   dibuixen a partir dels mapes d'`Assets/Levels` (`#` terra, `=` plataforma, `S` cargol, `B` abella, `X` boss,
   `C` moneda, `H` cor, `P` jugador, `E` portal) i el tilemap fa servir automàticament les peces de vora, gespa i farciment.
3. `Build Windows Player` → `Builds/Windows/GinebroQuest.exe`
4. `Capture Screenshots` → `Captures/*.png`
5. `Run Smoke Test (Play Mode)` – entra en Play Mode i comprova automàticament 33 punts (mal del boss = 2, 3 trepitjades
   per guanyar, "YOU WON!", Crèdits als 10 s, monedes i UI, barra de vida, enemics per nivell, portals, menús, splash,
   música…). El resultat queda a `Captures/SmokeTest.txt`.

## Crèdits

- Projecte base i sistema de moviment: material de classe (`mphc028/UnityPlatformerExample`).
- Gràfics del nivell, personatge i enemics: pack *Legacy Fantasy – High Forest* inclòs al projecte base.
- Logo de GINEBRÓ GAMES, monedes, cors, portal, música i efectes: generats per codi per a aquest projecte.
