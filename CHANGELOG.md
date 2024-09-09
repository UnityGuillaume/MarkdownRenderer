# Changelog
All notable changes to this package will be documented in this file.

## [1.2.0] - XXXX-XX-XX

### Changed
- **Breaking Change** : UIMarkdown renderer is not static anymore. need to instance it to use it.

### Fixed
- Image now properly scale without extra empty space around it

## [1.1.1] - 2023-07-21

### Fixed
- Image now have attribute applied properly (check Readme for placement)
- Fixed some naming
- Fixed handling direct Assets path in images

### Added
- Now handle packages paths
- Added package: search special links

## [1.1.0] - 2023-06-30

### Added
 - Markdig GenericAttribute Extension enabled to support custom class attributes to add custom style
 - Markdig Yaml Front Matter extension enabled to support define a custom USS file for the Markdown file
 - Updated Readme with instruction how to use custom style and custom USS