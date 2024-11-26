# Twenty Five Slicer

**Twenty Five Slicer** is a Unity package designed for advanced sprite slicing, enabling 25-slice editing. It divides sprites into a 5x5 grid, allowing precise scaling and manipulation while preserving key areas.

## Quick Start: Samples

You can download pre-configured samples directly from the **Samples** tab in the Unity Package Manager. This is the fastest way to get started with the package.

## How to Use

### Installing the Package

1. Open the Unity Package Manager.
2. Select **Add package from Git URL**.
3. Enter the following URL:

    ```
    https://github.com/kwan3854/twentyfiveslicer.git
    ```

    > To install a specific version, append the version tag:
    >
    > ```
    > https://github.com/kwan3854/twentyfiveslicer.git#v0.1.0
    > ```

### Editing a Sprite

1. **Open the 25-Slice Editor**:
    - In the Unity menu, navigate to **Window -> 2D -> 25-Slice Editor**.
2. **Load Your Sprite**:
    - Drag and drop your sprite into the editor or select it using the provided field.
3. **Adjust the Slices**:
    - Use the sliders to set the horizontal and vertical borders, dividing the sprite into 25 sections.
    - Borders are displayed visually in the editor for precise adjustment.
4. **Save the Configuration**:
    - Click the **Save Borders** button to store the 25-slice settings.

### Using the 25-Sliced Sprite

1. **Add the TwentyFiveSliceImage Component**:
    - Attach the `TwentyFiveSliceImage` component to your GameObject.
    - Replace the standard `Image` component with the `TwentyFiveSliceImage` component.
2. **Assign the Sliced Sprite**:
    - Set the sliced sprite in the `TwentyFiveSliceImage` component, just like a regular Image.
3. **Customize as Needed**:
    - The sprite will scale dynamically while respecting the 25-slice configuration, preserving key areas.

## Key Features

- Divide sprites into a 5x5 grid for fine-grained control.
- Seamlessly scale and stretch specific regions of a sprite.
- Compatible with Unity's UI system for dynamic layouts.
- Easy-to-use editor with visual feedback for precise slicing.

For more information or contributions, visit the [repository](https://github.com/kwan3854/TwentyFiveSlicer).