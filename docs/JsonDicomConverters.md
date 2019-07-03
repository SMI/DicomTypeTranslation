
# JsonDicomConverters

Documentation for classes which convert between DICOM and JSON. Each is an implementation of the [Newtonsoft.Json.JsonConverter](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonConverter.htm) class.


## Default fo-dicom converter

The default converter as found in fo-dicom, at the current version as specified in the project [packages](../Packages.md). Aims to follow the [DICOM JSON model](http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_F.2.2.html).

It is recommended to use this converter if you wish to read JSON data which has been created to follow the DICOM JSON model.

## SmiStrictJsonDicomConverter

- [Class](../DicomTypeTranslation/Converters/SmiStrictJsonDicomConverter.cs)

Essentially the same as the fo-dicom converter, except it handles a few cases where DICOM Decimal/IntegerString elements can be 'fixed' before attempting to convert them to their respective numeric types.

This converter is marked obsolete (it will not be updated to follow changes to the default converter), and will be removed at the next major version release.

## SmiLazyJsonDicomConverter

- [Class](../DicomTypeTranslation/Converters/SmiLazyJsonDicomConverter.cs)

The default converter used by this library, unless otherwise specified. Aims to allow greater coverage when dealing with "real world" DICOM data. In particular, it does not attempt to force the DICOM numeric string types into their respective C# types before converting to JSON.

Additionally, some value representations (`OW`, `OB`, and `UN`) have their tags serialized but their data are omitted. This can be configured with the `DicomTypeTranslater.SerializeBinaryData` option. The `VR`s which are treated like this are set by the `DicomTypeTranslater.DicomBsonVrBlacklist`.

This converter does not follow the DICOM JSON model, and will not support reading JSON data which was not created with this library.