# WiseTwin Core Package

Unity package for creating interactive training/learning experiences. Bridges Unity content with the WiseTwin SaaS for analytics tracking, completion notification, and metadata loading.

Supports questions (single/multi-choice, sequential), step-by-step procedures (click / manual / zone / group validation), text content, branching dialogues with a visual graph editor, and 3D-object video triggers.

---

## For developers using the package

If you are integrating WiseTwin into a Unity project, here is where to find what you need:

### 1. Public API reference — [API.md](./API.md)

The complete reference for the `WiseTwinAPI` static class — every method, event, and a set of end-to-end recipes (validate a step from custom 3D logic, penalise the score, drive a custom HUD, run a fully custom scenario alongside the package). **Start here.**

### 2. Working sample script

Open Unity's **Package Manager**, select **WiseTwin Core Package**, expand **Samples**, and click **Import** next to **Custom Scripting Example**. This drops a `CustomTrainingExample.cs` MonoBehaviour into your `Assets/Samples/` folder — a fully runnable script that exercises every public API method and event with keyboard shortcuts (V, S, M, B, C).

### 3. IDE hover docs

Every public method on `WiseTwinAPI` ships with XML doc comments. In your IDE (Visual Studio, Rider, VS Code), hover over any `WiseTwinAPI.X()` call to see signature, description, parameters, and inline example.

### 4. Live debugging — Score Monitor

While developing a training, drop a `ScoreDebugMonitor` component on any GameObject (or use the menu shortcut **WiseTwin > Debug > Add Score Monitor to Scene**) to see a live overlay of the cumulative score and a rolling log of every score-affecting operation. See the *Debugging — Score Monitor* section of [API.md](./API.md) for inspector options.

---

## Editor workflow (creating training content)

The training content lives in `StreamingAssets/{sceneName}-metadata.json` and is generated from the Editor window:

- Open **Window > WiseTwin > WiseTwin Editor**
- Use the tabs (General / Metadata / Scenarios / Dialogue / Video) to author the content
- Click **Generate Metadata** to write the JSON

The visual dialogue graph editor is available via **WiseTwin > Dialogue Graph Editor** or from the Dialogue tab.

---

## Architecture (advanced)

Internal architecture, file map, JSON schemas, and conventions are documented in [CLAUDE.md](./CLAUDE.md) — read this if you need to extend the package itself rather than just use it.

---

## Versioning

See [CHANGELOG.md](./CHANGELOG.md) for release notes.
