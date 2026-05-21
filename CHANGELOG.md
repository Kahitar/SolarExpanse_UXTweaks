# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [1.2.0] - 2026-05-21
### Added
- Made notification history scrollable and collapsed repeated identical notifications into one row with a count badge.

### Changed
- Suppressed automatic camera zoom/follow when changing the origin or destination in the mission planning window.

## [1.1.0] - 2026-05-16
### Changed
- Extended single-click camera retarget suppression and double-click zoom/follow behavior to map mission labels.

## [1.0.0] - 2026-05-16
### Added
- Added UXTweaks as a BepInEx/Harmony mod for Solar Expanse UI quality-of-life patches.
- Added object info list expansion, moved from FleetTracker's former `ObjectInfoListExpansion.dll`.
- Added body-click camera retarget suppression so single-clicking a map body opens object info without forcing the camera to zoom to and follow that body.
- Added double-click support for the original map body zoom-and-follow behavior.
