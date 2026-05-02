# Location Scene Art Override Guide

`LocationSceneBuilder` generates placeholder scenery for every campus location.
You can replace those placeholders without changing code by adding assets here.

## Full prefab override

Put a prefab at:

`Assets/Resources/LocationScenes/Prefabs/<LocationId>.prefab`

Supported names:

- `Dormitory`
- `Canteen`
- `Store`
- `TeachingBuilding`
- `Library`
- `Playground`
- `ExpressStation`
- `TakeoutStation`

If this prefab exists, it replaces the generated placeholder for that location.
Place the prefab's visual root around local X = 0; it will be spawned at that
location's `worldCenterX`.

## Sprite layer override

Put sprites at:

- `Assets/Resources/LocationScenes/Backgrounds/<LocationId>.png`
- `Assets/Resources/LocationScenes/Foregrounds/<LocationId>.png`

Set Texture Type to `Sprite (2D and UI)` in Unity's Inspector. Backgrounds fill
the whole camera view behind the floating HUD. They are scaled with aspect ratio
preserved, using a cover fit: the image will not be squeezed, but a little edge
content can be cropped if the image aspect ratio differs from the game window.
Foregrounds use the same full-screen cover fit and are drawn above the
background layer.

Recommended background ratios:

- 16:9, such as `1920x1080`, for a standard full-screen scene.
- 21:9 or wider, such as `2560x1080`, when you want more side detail that can
  survive camera movement and edge cropping.

Do not leave a blank UI strip at the bottom of the image. The bottom HUD floats
over the scene.

## Sorting order convention

- 0-9: background layers
- 10-19: ground and scenery landmarks
- 20+: labels and foreground readable objects
- Player/NPC sprites should stay above scenery landmarks
