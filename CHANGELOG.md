## [Unreleased]
### Changed
- New maps are created in `Assets/GleyData/DrivingNavigationSystem/Maps/<scene>` by default, and a new `NavigationSettings` asset in `Assets/GleyData/DrivingNavigationSystem`, instead of `Assets/NavigationData`. An existing `NavigationSettings` asset is still found wherever it is.
- Road layers, capture settings and the selected brush road type are remembered per project, in `UserSettings/Gley/DrivingNavigationSystem.asset`. The other editor preferences stay in EditorPrefs under the new `Gley.DrivingNavigationSystem.` prefix, so they reset once.

## [0.1.7] - 2026-10-29
### Fixed
- The setup window now finds the default prefabs, formatter and input actions wherever the package folder is placed in the project, instead of falling back to `Assets/Gley/DrivingNavigationSystem`.

## [0.1.0] - 2026-09-23
### First Release
