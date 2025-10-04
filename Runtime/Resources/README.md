# WiseTwin Resources Configuration

## PanelSettings Configuration

To ensure proper UI display in both Editor and WebGL builds, you need to create a `WiseTwinPanelSettings.asset` in this Resources folder.

### How to create the PanelSettings:

1. In Unity Editor, right-click in this Resources folder
2. Select Create > UI Toolkit > Panel Settings
3. Name it exactly: `WiseTwinPanelSettings`
4. Configure the following settings:
   - **Scale Mode**: Constant Pixel Size or Scale With Screen Size
   - **Reference Resolution**: 1920x1080 (recommended)
   - **Sort Order**: 100 (to appear on top)

### Font Configuration (IMPORTANT for WebGL):

1. Import a font that supports WebGL (e.g., Liberation Sans, Roboto)
2. In the PanelSettings inspector:
   - Expand "Text Settings"
   - Assign your font asset to "Font Asset" field
   - Or create a Font Asset: Window > TextMeshPro > Font Asset Creator

### Alternative: Use an existing PanelSettings

If you already have a PanelSettings in your project:
1. Copy it to this Resources folder
2. Rename it to `WiseTwinPanelSettings.asset`

This ensures the Training Completion UI displays correctly in production WebGL builds.