
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.2.1] - 2020-04-08

- Update MongoDB.Driver to 2.10.3
- First release built and deployed via Travis rather than Jenkins

## [2.2.1] - 2020-04-07

- Update HIC.FAnsiSql to 0.11.1
  - This updates the MySQL client to MySQLConnector
  - Any connection string containing 'ssl-mode' must be updated to 'sslmode'
- Update MongoDB.Driver to 2.10.2
- Update Newtonsoft.Json to 12.0.3
- Update NLog to 4.7.0
- Update YamlDotNet to 8.1.0

## [2.2.0] - 2020-03-26

- Upgrade fo-dicom to 4.0.4
  - Disable DicomValidation.AutoValidation in JSON converters
  - Disable compiler warning for validation
  - Remove the unused "SmiStrictJsonDicomConverter"
  - Remove the "lazy" qualifier from our JsonDicomConverter
  - Add support for three new DICOM VRs: OV, SV, and UV

- Updated PT modality schema (#e608c)
- Removed `NumberOfSeriesRelatedInstances` from all schema
- Templates now show last modified date
- Removed `ImageType` from Series

## [2.1.2] - 2019-11-20

### Changed

- Updated to latest version of FAnsiSql (0.10.12)

## [2.1.1] - 2019-09-13

### Changed

- Updated to latest version of FAnsiSql (0.10.4)

## [2.1.0] - 2019-08-30

### Added

- Support for arbitrary (not based on a specific DicomTag) columns in image table templates

### Changed

- Updated to latest version of FAnsiSql (0.10.0)

### Fixed

- DicomTypeTranslaterWriter: Fixed parsing of private sequence elements when the private creator is unknown


## [2.0.0] - 2019-07-08

### Added

- Simple worked usage example
- Add handling for string encoding in JSON conversion
- Added list of nuget package inventory
- Dependency test to ensure nuspec and csproj are correct

### Changed

- Improved README
- Update and refactor of DICOM-JSON converter
- Set default converter to SmiLazyJsonDicomConverter
- Updated to latest version of FAnsiSql (0.9.1.10)
- Updated to latest version of MongoDB.Driver (2.8.1)
- Renamed DicomTypeTranslaterWriter.BuildDatasetFromBsonDocument -> BuildDicomDataset
- Renamed DicomTypeTranslaterReader.BuildDatasetDocument -> BuildBsonDocument

### Removed

- DicomTypeTranslater.SetLazyConversion
- DicomTypeTranslaterReader.GetBsonKeyForTag from public API
- DicomTypeTranslaterReader.CreateBsonValue from public API
- DicomTypeTranslaterReader.StripLargeArrays
- DicomTypeTranslaterWriter.MaxVrsToExpect

### Fixed

- Fixed Travis dotnet test
- Fixed TagElevation tests to handle different environments

## [1.0.4] - 2019-06-25

### Added

- Added Travis config
- Added CHANGELOG file

### Changed

- Changed rake to use msbuild directly without albacore

## [1.0.0.3] - 2019-05-24

### Fixed

- Fixed .NET Core version of fo-dicom in nuspec


## [1.0.0.2] - 2019-05-24

### Changed

- Switched to .NET Core version of fo-dicom


## [1.0.0.1] - 2019-05-14

### Added

- TableCreation namespace from SMIPlugin
- Comments for public members
- Enabled XML docs
- Helper extension methods, documentation, and test cases
- Nuspec file

### Fixed

- NuGet packet naming


## [1.0.0.0] - 2019-05-13

Initial commit of code from old SMIPlugin repo

### Added

- Example usage case
- Rake build scripts for CI


[Unreleased]: https://github.com/HicServices/DicomTypeTranslation/compare/2.2.0...develop
[2.2.0]: https://github.com/HicServices/DicomTypeTranslation/compare/2.1.2..2.2.0
[2.1.2]: https://github.com/HicServices/DicomTypeTranslation/compare/2.1.1..2.1.2
[2.1.1]: https://github.com/HicServices/DicomTypeTranslation/compare/2.1.0..2.1.1
[2.1.0]: https://github.com/HicServices/DicomTypeTranslation/compare/2.0.0..2.1.0
[2.0.0]: https://github.com/HicServices/DicomTypeTranslation/compare/1.0.4...2.0.0
[1.0.4]: https://github.com/HicServices/DicomTypeTranslation/compare/1.0.0.3...1.0.4
[1.0.0.3]: https://github.com/HicServices/DicomTypeTranslation/compare/1.0.0.2...1.0.0.3
[1.0.0.2]: https://github.com/HicServices/DicomTypeTranslation/compare/1.0.0.1...1.0.0.2
[1.0.0.1]: https://github.com/HicServices/DicomTypeTranslation/compare/1.0.0.0...1.0.0.1
[1.0.0.0]: https://github.com/HicServices/DicomTypeTranslation/releases/tag/1.0.0.0
