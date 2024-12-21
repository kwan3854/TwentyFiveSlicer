# Twenty Five Slicer

**Twenty Five Slicer** is a Unity package designed for more advanced sprite slicing, enabling a "25-slice" approach. It divides a sprite into a 5x5 grid, allowing precise scaling and manipulation of individual regions while preserving key areas.

## 9-slice vs 25-slice

<p align="center">
  <img src="Documentation~/Images/9slice_vs_25slice.gif" alt="9-slice vs 25-slice" width="350" />
  <img src="Documentation~/Images/9slice_vs_25slice_2.gif" alt="9-slice vs 25-slice 2" width="350" />
</p>


## Key Concept

<p align="center">
  <img src="Documentation~/Images/25slice_debugging_view.gif" alt="25-slice Debugging View" width="700" />
</p>


- **9 slices**: Non-stretchable areas.
- **6 slices**: Stretch horizontally only.
- **6 slices**: Stretch vertically only.
- **4 slices**: Stretch in both directions.

This allows for far more detailed slicing. Where traditional 9-slice images often require stacking multiple image layers to achieve complex UI shapes (e.g., speech bubbles, boxes with icons or separators at the center), a 25-slice configuration can often handle these scenarios with just a single image.

## Installing the Package

### 1. Install via OpenUPM

#### 1.1. Install via Package Manager
Please follow the instrustions:
- open Edit/Project Settings/Package Manager 
- add a new Scoped Registry (or edit the existing OpenUPM entry)
  - Name: `package.openupm.com`
  - URL: `https://package.openupm.com`
- click `Save` or `Apply`
- open Window/Package Manager 
- click `+`
- select `Add package by name...` or `Add package from git URL...`
- paste `com.kwanjoong.twentyfiveslicer` into name 
- paste version (e.g.`1.0.0`) into version 
- click `Add`
---
#### 1.2. Alternatively, merge the snippet to Packages/manifest.json
```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": []
    }
  ],
  "dependencies": {
    "com.kwanjoong.twentyfiveslicer": "1.0.0"
  }
}
```
#### 1.3. Install via command-line interface
```sh
openupm add com.kwanjoong.twentyfiveslicer
```

### 2. Install via Git URL

1. Open the Unity **Package Manager**.
2. Select **Add package from Git URL**.
3. Enter: `https://github.com/kwan3854/twentyfiveslicer.git` 
4. To install a specific version, append a version tag: `https://github.com/kwan3854/twentyfiveslicer.git#v1.0.0`

## How to Use

### Create Slice Data Map (First-time Setup)

1. Navigate to the `Assets/Resources` folder. (Create the folder if it doesn’t exist.)
2. Right-click -> **Create -> TwentyFiveSlicer -> SliceDataMap**

<p align="center">
  <img src="Documentation~/Images/how_to_add_25slice_datamap.png" alt="How to Add 25-slice DataMap" width="550" style="display:inline-block; margin-right:20px;" />
  <img src="Documentation~/Images/sliceDataMap.png" alt="SliceDataMap Example" width="200" style="display:inline-block;" />
</p>


### Editing a Sprite

1. **Open the 25-Slice Editor**:
- **Window -> 2D -> 25-Slice Editor**

<p align="center">
  <img src="Documentation~/Images/how_to_open_editor.png" alt="How to Open 25-Slice Editor" width="700" />
</p>


2. **Load Your Sprite**:
- Drag and drop your sprite into the editor or select it via the provided field.

3. **Adjust the Slices**:
- Use the sliders to define the horizontal and vertical cut lines, dividing the sprite into 25 sections.
- Borders are displayed visually for accurate adjustments.

<p align="center">
  <img src="Documentation~/Images/editor.png" alt="25-Slice Editor" width="700" />
</p>


4. **Save the Configuration**:
- Click **Save Borders** to store the 25-slice settings.

### Using the 25-Sliced Sprite

1. **Create a TwentyFiveSliceImage GameObject** (or add the component to an existing GameObject):

<p align="center">
  <img src="Documentation~/Images/how_to_add_25slice_gameobject.png" alt="How to Add 25-Slice GameObject" width="700" />
</p>


2. **Assign the Sliced Sprite**:
- In the `TwentyFiveSliceImage` component, assign the sprite as you would with a standard UI Image.

## Key Features

- Divide sprites into a 5x5 grid for highly detailed control.
- Seamlessly scale and stretch specific sprite regions.
- Fully compatible with Unity’s UI system for dynamic layouts.
- Intuitive editor with clear visual guidance for precise adjustments.

For more information or contributions, visit the [repository](https://github.com/kwan3854/TwentyFiveSlicer).

## Delete Unused Data

**Tools -> Twenty Five Slicer Tools -> Slice Data Cleaner**