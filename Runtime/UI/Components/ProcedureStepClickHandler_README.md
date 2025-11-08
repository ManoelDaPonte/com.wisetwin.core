# ProcedureStepClickHandler - Système de Distance de Clic

## Vue d'ensemble

Le composant `ProcedureStepClickHandler` gère les clics sur les objets pendant les procédures. Il inclut maintenant un **système de distance maximale** pour obliger l'utilisateur à s'approcher des objets avant de pouvoir cliquer dessus.

## Paramètres de Distance

### Mode Relatif (Recommandé) ✅

**Par défaut activé** - S'adapte automatiquement au scale de votre environnement

```
useRelativeDistance = true
relativeDistanceFactor = 3.0
```

**Comment ça marche :**
- Le système calcule la taille de l'objet (bounds)
- Distance maximale = `taille de l'objet × relativeDistanceFactor`
- Si vous scalez tout votre environnement × 0.1, les distances seront aussi × 0.1 automatiquement !

**Exemple :**
- Objet de 2m de diamètre → distance max = 2 × 3 = **6 mètres**
- Environnement scalé × 0.5 → objet 1m → distance max = 1 × 3 = **3 mètres**

### Mode Fixe

Si vous préférez une distance absolue (non recommandé pour les environnements multi-échelles) :

```
useRelativeDistance = false
maxClickDistance = 5.0  // 5 mètres Unity
```

⚠️ **Attention :** Cette distance ne s'adaptera PAS si vous scalez votre environnement !

## Configuration Recommandée par Type d'Objet

### Petits Objets (boutons, leviers, interrupteurs)
```
useRelativeDistance = true
relativeDistanceFactor = 5.0  // Peut être cliqué de 5× sa taille
```
→ Pour un bouton de 10cm, cliquable jusqu'à 50cm

### Objets Moyens (vannes, portes, panneaux)
```
useRelativeDistance = true
relativeDistanceFactor = 3.0  // Distance standard
```
→ Pour une porte de 2m, cliquable jusqu'à 6m

### Gros Objets (machines, équipements)
```
useRelativeDistance = true
relativeDistanceFactor = 2.0  // Distance plus courte
```
→ Pour une machine de 5m, cliquable jusqu'à 10m

### Objets Très Précis (manipulation fine)
```
useRelativeDistance = true
relativeDistanceFactor = 1.5  // Très proche
```
→ Oblige l'utilisateur à vraiment s'approcher

## Comment Ajuster dans le Code

Les valeurs par défaut sont définies dans le script :

```csharp
public class ProcedureStepClickHandler : MonoBehaviour
{
    [Header("Click Distance Settings")]
    public float maxClickDistance = 5f;           // Utilisé si useRelativeDistance = false
    public bool useRelativeDistance = true;        // Mode adaptatif activé par défaut
    public float relativeDistanceFactor = 3f;     // Distance = 3× la taille de l'objet
}
```

## Feedback Visuel

Le système fournit automatiquement :
- ✅ **Hover + Scale** quand vous êtes assez proche ET que la souris survole l'objet
- ❌ **Pas de hover** si vous êtes trop loin, même si la souris est sur l'objet
- 🎯 **Click autorisé** seulement si vous êtes dans la zone ET que vous survolez

## Tests avec Différents Scales

### Scénario 1 : Environnement Normal (Scale 1:1)
- Objet : Vanne de 0.5m
- Distance max : 0.5 × 3 = **1.5m**
- ✅ L'utilisateur doit s'approcher à 1.5m

### Scénario 2 : Environnement Réduit (Scale 0.1:1)
- Objet : Vanne de 0.05m (0.5m × 0.1)
- Distance max : 0.05 × 3 = **0.15m**
- ✅ L'utilisateur doit s'approcher à 0.15m
- ✅ **Proportions conservées !**

### Scénario 3 : Environnement Agrandi (Scale 10:1)
- Objet : Vanne de 5m (0.5m × 10)
- Distance max : 5 × 3 = **15m**
- ✅ L'utilisateur doit s'approcher à 15m
- ✅ **Proportions conservées !**

## Avantages du Mode Relatif

1. 🎯 **Auto-adaptatif** - Pas besoin de reconfigurer pour chaque scale
2. 🔧 **Cohérent** - Les proportions distance/objet restent constantes
3. 🌍 **Multi-échelle** - Fonctionne pour miniatures, taille réelle, et géants
4. ⚡ **Simple** - Un seul paramètre à ajuster (`relativeDistanceFactor`)

## Problèmes Potentiels

### Objets avec des Bounds Étranges
Si l'objet a des enfants très éloignés ou des colliders mal configurés :
- Le système prend la taille totale (bounds englobants)
- Solution : Nettoyer la hiérarchie ou utiliser le mode fixe pour cet objet

### Distance Trop Grande/Petite
Ajustez simplement `relativeDistanceFactor` :
- Trop facile → **Diminuer** (ex: 2.0 au lieu de 3.0)
- Trop difficile → **Augmenter** (ex: 4.0 au lieu de 3.0)

## Debug

Pour débugger les distances dans Unity :
1. Sélectionnez l'objet avec le `ProcedureStepClickHandler`
2. En mode Play, observez les valeurs dans l'Inspector
3. La distance est recalculée chaque frame

## Conclusion

Le **mode relatif** (activé par défaut) est la meilleure option pour 99% des cas car il s'adapte automatiquement au scale de votre environnement. Vous n'avez qu'à ajuster le `relativeDistanceFactor` si nécessaire ! 🎯
