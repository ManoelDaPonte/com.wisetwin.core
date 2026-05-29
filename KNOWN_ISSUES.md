# Known issues & architectural debt

Running list of things we know are wrong but can't fix in a single PR. Add to
the top of the relevant section when you find a new one. When you ship a real
fix, remove the entry from here.

---

## 🔴 Runtime identity is baked at WebGL build time

**Status:** worked around on the SaaS side (Referer-based override in
`/api/unity/metadata` — see `KNOWN_ISSUES.md` in `wisetwin-saas-refacto`)

### What's wrong

Three values are `[SerializeField]` on `MetadataLoader.cs` and configured in
the Unity Editor, baked into the bundle at `Build > WebGL`:

- `apiBaseUrl` — locks the bundle to one SaaS deployment (typically prod)
- `containerId` — locks the bundle to one organization
- `buildName` derived from `SceneManager.GetActiveScene().name` — locks the
  bundle to one SaaS folder name (the Unity scene name)

`MetadataLoader.Start()` calls `LoadMetadata()` immediately, with no hook for
the host page to set these values first. `WiseTwinWebGL.jslib` only contains
`SendTrainingCompleted` — there's no `GetUrlParameter` and no other inbound
JS→Unity bridge wired into the loader.

This breaks several SaaS flows:

1. The same bundle uploaded to two orgs both call the original org's metadata.
2. A SaaS-side `duplicate` (used for translation) renames the Azure folder
   but the bundle keeps asking for the source's `buildName`.
3. Local SaaS development can never test changes that flow through the
   runtime metadata, because the bundle ignores the host origin.

### Proper fix

Move runtime identity from build-time fields to **runtime configuration**.
Two complementary changes:

1. **Add a JS bridge for runtime config.** Extend `WiseTwinWebGL.jslib` with
   either `GetUrlParameter(name)` or a dedicated `WiseTwinReceiveConfig(json)`
   that the host page calls before `MetadataLoader.Start()` fires. Easiest
   shape: read `containerId`, `buildName`, `apiBaseUrl`, and `lang` from the
   URL query of the hosting page.

2. **Defer the auto-load.** `MetadataLoader.Start()` should not call
   `LoadMetadata()` unconditionally. Either wait for an explicit
   `Initialize(...)` call from the host, or check for URL params and only
   fall back to baked fields if none were provided. The host page can then
   `SendMessage("MetadataLoader", "Initialize", json)` once Unity is loaded.

After both land:
- The same WebGL bundle becomes reusable across orgs without rebuild.
- SaaS duplication for translation works end-to-end.
- Localhost SaaS dev can point bundles at the local API for true end-to-end
  testing.
- All existing formations need to be rebuilt **once** with the new package
  version, and the SaaS `Referer`-based override workaround can be removed.

### Editor-side touch-up to ship alongside

`WiseTwinBuildProcessor.cs` currently only warns when `apiBaseUrl` and
`containerId` are empty in Production mode. Once those become runtime params,
the warning is wrong — empty is the correct state. Either remove the warning
or flip it to fail when those fields are **non-empty** (catches stale
configuration left over from the old workflow).

Likewise `WiseTwinEditorMetadataTab` (the Editor window tab "Metadata Config")
exposes UI for setting those fields. Remove those inputs or mark them as
legacy-only after the migration.
