---
name: browser
description: Contrôler un navigateur web headless via l'API browser-streamer. À utiliser dès que l'utilisateur demande de naviguer sur un site web, faire une recherche, cliquer, remplir un formulaire, prendre un screenshot, scroller, ou interagir avec une page web — même s'il ne mentionne pas explicitement "navigateur" ou "browser". Le streamer tourne sur localhost:3000 et l'utilisateur peut voir la navigation en direct sur le viewer http://localhost:3000.
---

# Browser Streamer — Navigation Web

Ce skill permet de piloter un navigateur Chromium headless via l'API HTTP du browser-streamer (localhost:3000). L'utilisateur peut observer la navigation en direct en ouvrant `http://localhost:3000` dans son navigateur.

## État du serveur

Avant toute commande, vérifier que le streamer tourne :

```bash
curl -s http://localhost:3000/state
```

Si le serveur ne répond pas, le lancer :

```bash
cd /home/freebox/browser-streamer && node streamer.js &
```

Attendre ~3 secondes que le navigateur soit prêt, puis vérifier l'état.

## Commandes disponibles

Toutes les commandes passent par l'API HTTP POST sur `/command`. Le format est :

```bash
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"<action>", ...}'
```

### Navigation

| Action | Paramètres | Description |
|--------|-----------|-------------|
| `goto` | `url` (string) | Naviguer vers une URL |
| `reload` | — | Recharger la page |
| `url` | — | Obtenir l'URL actuelle |
| `title` | — | Obtenir le titre de la page |

### Interaction

| Action | Paramètres | Description |
|--------|-----------|-------------|
| `click` | `selector` (string CSS) | Cliquer sur un élément |
| `type` | `selector` (string), `text` (string) | Saisir du texte dans un champ |
| `press` | `key` (string) | Appuyer sur une touche (Enter, Escape, Tab...) |
| `scroll` | `y` (number, défaut 300) | Scroller de N pixels |
| `scrollTo` | `x` (number), `y` (number) | Scroller à une position |
| `wait` | `ms` (number, défaut 1000) | Attendre N millisecondes |

### Extraction

| Action | Paramètres | Description |
|--------|-----------|-------------|
| `text` | — | Texte visible de la page (max 20000 chars) |
| `content` | — | HTML complet (max 50000 chars) |
| `eval` | `script` (string JS) | Exécuter du JS dans la page |
| `screenshot` | — | Screenshot PNG (retourne base64) |

### État

| Action | Paramètres | Description |
|--------|-----------|-------------|
| `status` | — | État du navigateur |
| `help` (GET) | — | Liste des commandes |

## Exemples

**Naviguer vers une page :**
```bash
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"goto","url":"https://fr.wikipedia.org"}'
```

**Remplir un champ de recherche et valider :**
```bash
# Saisir le texte
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"type","selector":"input[name=\"q\"]","text":"claude code"}'
# Appuyer sur Entrée
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"press","key":"Enter"}'
```

**Extraire le texte visible :**
```bash
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"text"}'
```

**Prendre un screenshot et le sauvegarder :**
```bash
curl -s -X POST http://localhost:3000/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"screenshot"}' | jq -r '.result' | base64 -d > /tmp/screenshot.png
```

## Workflow typique

1. **Vérifier l'état** → `GET /state`
2. **Naviguer** → `goto` vers l'URL
3. **Attendre** → `wait` si nécessaire (le goto attend déjà domcontentloaded)
4. **Interagir** → `click`, `type`, `scroll`, `press`
5. **Extraire** → `text` ou `eval` pour lire le contenu
6. **Répéter** 3-5 autant que nécessaire

## Notes importantes

- Le navigateur est headless (pas d'affichage graphique sur la VM). L'utilisateur voit le flux via `http://localhost:3000` sur son poste local (ou via tunnel/port forwarding).
- Le profil Chrome est persistant dans `browser-streamer/chrome-profile/` — les cookies, sessions et logins sont conservés.
- Les timeouts : goto=60s, waitForSelector=10s.
- Toujours vérifier que le serveur répond avant d'envoyer des commandes.
- Après chaque action de navigation/interaction, toujours faire un petit `wait` de 500ms pour laisser le viewer se mettre à jour.
- Pour les sites qui demandent un login, guider l'utilisateur à l'oral (il voit la page sur le viewer) et exécuter les actions qu'il demande.
