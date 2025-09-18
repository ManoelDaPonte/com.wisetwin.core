# TextDisplayer UI

## Description
Affiche du contenu texte riche avec support pour formatage markdown-like et multilingue.

## Structure de données attendue

```json
{
  "text_content": {
    "title": {
      "en": "Content Title",
      "fr": "Titre du contenu"
    },
    "subtitle": {
      "en": "Optional Subtitle",
      "fr": "Sous-titre optionnel"
    },
    "content": {
      "en": "# Markdown-formatted content\n\nParagraph text...",
      "fr": "# Contenu formaté markdown\n\nTexte du paragraphe..."
    },
    "showContinueButton": true  // Optionnel, true par défaut
  }
}
```

## Syntaxe de formatage supportée

### Titres
```
# Titre principal (H1)
## Sous-titre (H2)
### Sous-sous-titre (H3)
```

### Listes
```
- Point de liste
• Point alternatif
```

### Citations
```
> Citation ou note importante
```

### Avertissements
```
! Message d'avertissement ou note importante
```

### Paragraphes
Les lignes vides créent de nouveaux paragraphes avec espacement.

## Styles appliqués
- **H1**: 28px, cyan (#1BD9AC), gras
- **H2**: 22px, blanc, gras
- **H3**: 18px, gris clair
- **Listes**: 16px avec indentation
- **Citations**: Fond gris foncé, bordure gauche cyan
- **Avertissements**: Fond orange foncé, texte jaune

## Fonctionnalités
- Rendu markdown-like automatique
- Support multilingue (en/fr)
- ScrollView pour contenu long
- Bouton "Continuer" optionnel
- Styles prédéfinis pour différents types de contenu

## Utilisation dans Unity
1. Configurer `ContentType.Text` sur l'InteractableObject
2. Définir le contenu dans le metadata JSON
3. Utiliser la syntaxe markdown pour le formatage
4. Le contenu s'affiche avec mise en forme automatique
5. Événement `OnCompleted` déclenché à la fermeture