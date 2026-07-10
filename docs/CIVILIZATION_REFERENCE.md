# Civilization — Référence des Mécaniques Originales

> Document de référence pour le projet Civ VS Civ.  
> Basé sur les standards Civilization V / VI et Civilization Revolution 2.

---

## 1. Architecture de la Carte (World Board)

| Aspect | Mécanique Civ | Notre projet |
|--------|---------------|--------------|
| **Grille** | Hexagonale (6 directions uniformes) | ✅ Hexagonale |
| **Topologie** | Cylindre (boucle horizontale), limites Nord/Sud (glace) | ✅ Glace aux pôles, wrap à faire |
| **Terrain** | Plaine, prairie, désert, toundra, neige, océan, côte | ✅ 9 types (Sea, Ocean, Mountain, Hill, Forest, Plain, Desert, Marsh, Ice) |
| **Relief** | Plat, colline, montagne (bloquante sauf cols) | ✅ Mountain avec `IsMountainPass` |
| **Features** | Forêt, jungle, marais, oasis, fleuve (sur les arêtes) | ✅ Forêt, marais, rivière |
| **Ressources** | Stratégiques (fer, pétrole), luxe (soie), bonus (blé) | ✅ `LuxuryResourceId`, `StrategicResourceId` |
| **Fog of War** | 3 états : Inexploré / Exploré (figé) / Visible (temps réel) | ❌ Désactivé temporairement |

### Structure d'une tuile

```
HexCell {
    Coordinates       → q, r, s (cubiques)
    TileType          → enum 9 valeurs
    OwnerIndex        → -1 = neutre
    IsVisible         → fog actuel
    HasBeenExplored   → mémoire fog
    HasRiver          → bool
    IsMountainPass    → col franchissable
    LuxuryResourceId  → -1 = aucune
    StrategicResourceId → -1 = aucune
    MovementCost      → -1 = infranchissable
    DefenseBonus      → modificateur combat
}
```

---

## 2. Caméra et Rendu

### Civ Standard
- **Projection** : Perspective 3D avec FOV bas (15°-30°) simulant vue axonométrique
- **Inclinaison** : 45°-60° par défaut
- **Rotation** : Verrouillée horizontalement (alignement Nord/Sud)
- **Zoom** : Modifie l'inclinaison (zoomé = plus horizontal pour effet 3D)

### Notre projet
- **Projection** : Orthographique (simplifié)
- **Inclinaison** : 53° (réglable)
- **Contrôles** : Clic droit glissé = pan, Molette = zoom
- **Bornes** : Clamp aux limites de la carte

### Comportement CivRev2
- Au zoom max : vue globe, rotation gauche/droite uniquement
- Au zoom moyen : pan libre
- La caméra suit l'unité sélectionnée

---

## 3. Boucle de Gameplay (Core 4X)

### Tour par Tour
- **Solo** : Séquentiel (Joueur 1 → Joueur 2 → ...)
- **Multi** : Simultané (tous jouent en même temps)

### Phases d'un tour (notre implémentation)

| Phase | Description | Automatique ? |
|-------|-------------|---------------|
| **NarrativeEvent** | Événement narratif si déclenché | Auto si pas d'event |
| **Movement** | Déplacer les unités | Manuel |
| **CityManagement** | Gérer les villes (production, citoyens) | Auto (0.5s) |
| **Diplomacy** | Interactions diplomatiques | Auto (0.5s) |
| **Research** | Choix de technologie | Popup pour joueur humain |
| **EndOfTurn** | Résolution IA, événements système | Auto |

---

## 4. Gestion des Villes

### Civ Standard
- **Expansion** : Achat ou croissance organique des tuiles (score culture)
- **Citoyens** : Assignés aux tuiles pour générer ressources
- **Quartiers** : Districts spécialisés (occupent un hexagone complet)
- **Merveilles** : Bâtiments uniques mondiaux

### Notre implémentation

```csharp
City {
    CityName, OwnerIndex, Population
    Location (HexCoordinates)
    CurrentProduction, ProductionStored, ProductionCost
    FoodStored, FoodThreshold    // Croissance
    IsCapital
    Buildings                     // Liste des bâtiments construits
}
```

**Yields par tour :**
- Nourriture : Pop × 2
- Production : Pop × 1
- Or : Pop × 1
- Science : Pop × 1
- Culture : max(1, Pop / 2)

**Croissance :** FoodStored >= 10 + Pop × 5 → Population++

---

## 5. Système d'Unités

### Civ Standard
- **Règle 1UPT** : 1 unité militaire + 1 civile par tuile max
- **Points de mouvement** : Coût variable selon terrain
- **Armées** : 3 unités identiques → 1 Armée (stats combinées)

### Notre implémentation

| Catégorie | Exemples | Rôle |
|-----------|----------|------|
| Recon | Éclaireur | Vision, rapide, fragile |
| Infantry | Guerrier, Phalange | Polyvalent, bonus défensif |
| Cavalry | Char, Cavalerie | Rapide, bonus terrain plat |
| Siege | Bélier, Catapulte | Anti-ville |
| Naval | Trière, Quinquérème | Contrôle maritime |
| Support | Médecin, Ingénieur | Soin, construction |
| Civil | Colon | Fondation de ville |

**Combat :** Facteurs terrain, moral, ravitaillement, vétérance, doctrines, généraux.

---

## 6. Arbres de Progression

### Civ Standard
- **Arbre des Technologies** (Science) : DAG orienté acyclique
- **Arbre des Dogmes** (Culture) : Politiques gouvernementales

### Notre implémentation

**3 ères :** Antiquité → Classique → Médiévale  
**17 technologies** dans l'arbre

**Changement d'ère :** Toutes les techs de l'ère OU 3 techs de l'ère suivante

---

## 7. Conditions de Victoire

| Type | Civ Standard | Notre MVP |
|------|-------------|-----------|
| **Domination** | Conquérir toutes les capitales | ✅ Éliminer toutes les villes ennemies |
| **Scientifique** | Course spatiale | ❌ Pas encore |
| **Culturelle** | Tourisme | ❌ Pas encore |
| **Diplomatique** | Votes mondiaux | ❌ Pas encore |
| **Économique** | Accumuler de l'or | ❌ Pas encore |

---

## 8. Équilibrage (Pacing)

- **Snowball** : Plus de villes = plus de ressources
- **Freins** : Coût des technologies augmente avec le nombre de villes, pénalités de maintenance

---

## 9. État du projet — Gap Analysis

### ✅ Fonctionnel
- Grille hexagonale + génération procédurale
- Tuiles KayKit (alignement corrigé)
- Unités procédurales (Guerrier, Colon, Cavalier, Bateau, Éclaireur)
- Villes avec KayKit buildings
- Système de tour (6 phases)
- Arbre technologique (3 ères)
- Combat (terrain, doctrines)
- Diplomatie basique
- Événements narratifs
- Audio procédural
- Sauvegarde/Chargement
- HUD + barre de ressources
- Caméra isométrique 53°

### ❌ À implémenter
- Fog of War (3 états)
- 1UPT (one unit per tile)
- Armées combinées (3 unités → 1)
- Conditions de victoire multiples
- IA qui joue vraiment
- Wrap cylindrique de la carte
- Effet globe visuel
- Quartiers / Merveilles
- Diplomatie avancée (congrès mondial)
- Ouvriers / améliorations de terrain

---

*Document généré le 2026-07-10 — Projet Civ VS Civ*
