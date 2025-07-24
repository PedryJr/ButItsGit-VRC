# Triturbo Face Tracking Common
All notable changes to this project will be documented in this file.

# Changelog

## [1.0.0] - 2024-09-24

### Added
- FbxDataSO: managing FBX data.
- Localization: text for FBX data.
- FaceTrackingInstaller: notify user FBX file version using `FbxDataSO`.

### Fixed
- FaceTrackingInstaller
    - Fixed an issue where `DuplicateGameObject()` get the wrong scene if multiple scenes were being edited simultaneously.
    - Fixed an exception that occurred when missing components were found in the avatar during `ParameterHelper.GetAvatarParameterMemoryUsed()`.


## [Pre-1.0.0] 

Previous changes and improvements are undocumented.