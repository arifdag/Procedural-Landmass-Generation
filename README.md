# HDRP Procedural Landmass Generation

A procedural terrain generation system built with Unity's High Definition Render Pipeline (HDRP).


![terrain](https://github.com/user-attachments/assets/efc9a5c7-43f3-438a-b5e0-4b8004a1a334)


## Features

-   **Infinite Procedural Terrain**: Generates an endless world in chunks around the player.
-   **Dynamic Level of Detail (LOD)**: Mesh complexity decreases with distance for optimal performance.
-   **Multi-layered Noise Generation**: Uses Perlin noise with multiple octaves to create realistic landforms.
-   **ScriptableObject-based Configuration**: Easily create and swap terrain settings for different biomes.
-   **Procedural Texture Blending**: A custom shader blends textures based on terrain height and slope.
-   **GPU-Instanced Object Placement**: Places thousands of foliage and objects with minimal performance impact.
-   **In-Editor Preview**: See changes to the terrain in real-time without entering Play mode.

## Installation

1.  Clone this repository to your local machine.
2.  Open the project in Unity Hub using **Unity version 2022.3.20f1** or later.
3.  Open the main scene located at `Assets/Scenes/SampleScene` to begin.

## Usage
### Editor Preview
1.  In the `SampleScene`, select the **Map Preview** object in the Hierarchy.
2.  Adjust the generation parameters in the Inspector (see **Configuration** below).
3.  If `Auto Update` is disabled on the settings assets, click the **Generate** button in the `MapPreview` Inspector to see your changes.

## Configuration

The terrain generator offers several customizable parameters:

The terrain generator is controlled by several ScriptableObject assets located in `Assets/Terrain Assets/`. You can edit the existing assets or create new ones to define different biomes.

The primary settings are found in the **Default Height** asset:

-   **Noise Scale**: Controls the zoom level of the noise pattern. Larger values create more stretched-out terrain features.
-   **Octaves**: The number of noise layers combined to create the terrain. More octaves add more fine detail.
-   **Persistance**: Controls how much each successive octave influences the final shape (0-1). Lower values create smoother terrain.
-   **Lacunarity**: Controls how much the detail/frequency of each octave increases. Higher values add more small, high-frequency features.
-   **Seed**: The starting value for the random number generator, allowing you to reproduce the same world.
-   **Height Multiplier**: A global multiplier that controls the vertical scale of the terrain, making mountains taller or flatter.
-   **Height Curve**: An `AnimationCurve` that remaps the noise values, allowing for fine control over the terrain's final shape (e.g., creating plateaus or sharp cliffs).

Additional settings can be modified in:
-   **Default Texture**: Defines the texture layers (sand, grass, rock, snow) and the height at which they appear.
-   **Default Placement Settings**: Defines rules for placing objects like trees and rocks, including density, valid height/slope ranges, and scale.

## License
[MIT License]
