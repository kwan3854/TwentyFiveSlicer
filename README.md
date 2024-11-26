# Twenty Five Slicer

**Twenty Five Slicer** is a Unity package designed to enable advanced 25-slice editing for sprites. It allows you to divide your sprites into a 5x5 grid for precise scaling and manipulation while maintaining key areas intact.

## How to Use

### Installing Package

Package manager -> Add package from git url -> enter this url

```https://github.com/kwan3854/twentyfiveslicer.git```

> You can also specify version like this
>
> ```https://github.com/kwan3854/twentyfiveslicer.git#v0.1.0```

### Editing a Sprite

1. Open the **25-Slice Editor**:
   - Navigate to Window -> 2D -> 25-Slice Editor in the Unity menu.
2. Load your sprite:
   - Drag and drop your sprite into the editor or select it using the provided field.
3. Adjust the slice:
   - Use the sliders to set the horizontal and vertical borders to divide the sprite into 25 sections.
   - Borders are represented visually in the editor for precision.
4. Save the configuration:
   - Click the **Save Borders** button to store the 25-slice configuration.

### Using the 25-Sliced Sprite

To use a sprite that has been 25-sliced:

1. Add a TwentyFiveSliceImage component to your GameObject.
   - Replace the standard Image component with the TwentyFiveSliceImage component.
2. Assign the sliced sprite:
   - Set the sprite in the TwentyFiveSliceImage component just like a regular Image.
3. Customize as needed:
   - The sprite will respect the 25-slice configuration, preserving key areas while scaling the rest dynamically.