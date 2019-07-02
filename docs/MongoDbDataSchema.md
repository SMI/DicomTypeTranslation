
# MongoDB Schema

This document describes how fo-dicom `DicomDataset`s are (de)serialized into MongoDB's `BsonDocument` type. The methods which perform this are:

- [DicomTypeTranslaterReader.cs](../DicomTypeTranslation/DicomTypeTranslaterReader.cs) `static BsonDocument BuildBsonDocument(DicomDataset dataset)`
- [DicomTypeTranslaterWriter.cs](../DicomTypeTranslation/DicomTypeTranslaterWriter.cs) `static DicomDataset BuildDicomDataset(BsonDocument document)`

## Schema format

[This](../DicomTypeTranslation.Tests/BsonTranslationTests.cs#L104) test shows and example `BsonDocument` for a complex `DicomDataset` example. Below is the general format:

```javascript
{
	"_id": ObjectId("1234567890"),  // Auto-generated ID, can be deserialized to DateTime of document creation
	"AcquisitionDate":	"20000229\20180401",
	"AcquisitionDateTime":	"20141231194212\20180401235959",
	"RetrieveAETitle":	"ApplicationEntity-1\ApplicationEntity-2",
	"URNCodeValue":	"http://example.com?q=1",
	"AdmittingDiagnosesCodeSequence": [
		{
			"ImagesInAcquisition" : "1234",
			"SelectorSTValue" : "Short\\Text"
		}
	],
	"PatientAge":	"34y\32y",
	"AdditionalPatientHistory":	"This is a dicom long string. Backslashes should be ok: \\\\\\",
	"ImagesInAcquisition":	"0\-2147483648\2147483647",
	"QualityControlImage":	"FOOBAR\OOFRAB",
	"LossyImageCompressionRatio":	"0\123.456",
	"PatientState":	"This is a long string part 1\This is a long string part 2",
	"SelectorATValue":	"00080018\0020000E",
	"SelectorOBValue":	"",
	"SelectorOWValue":	"",
	"SelectorPNValue":	"Morrison-Jones^Susan^^^Ph.D.1\Morrison-Jones^Susan^^^Ph.D.2",
	"SelectorTMValue":	"123456\235959",
	"SelectorSHValue":	"ShortString-1\Short-String-2",
	"SelectorUNValue":	"",
	"SelectorSTValue":	"Short\Text\Backslashes should be ok: \\",
	"SelectorUCValue":	"UnlimitedCharacters-1",
	"SelectorUTValue":	"unlimited!",
	"SelectorODValue":	[0, -1.7976931348623157E+308, 1.7976931348623157E+308],
	"SelectorFDValue":	[0, -1.7976931348623157E+308, 1.7976931348623157E+308],
	"SelectorOLValue":	[0, 1, 4294967295],
	"SelectorFLValue":	[0, -3.4028234663852886E+38, 3.4028234663852886E+38],
	"SelectorULValue":	[0, 1, 4294967295],
	"SelectorUSValue":	[0, 1, 65535],
	"SelectorSLValue":	[0, -2147483648, 2147483647],
	"SelectorSSValue":	[0, -32768, 32767],
	"SelectorUIValue":	"1.2.3.4\5.6.7.8",
	"FloatPixelData":	[0, -3.4028234663852886E+38, 3.4028234663852886E+38],	
	"(0003,0010)-PrivateCreator": {
			"vr":	"LO",
			"val":	"TEST"
	},
	"(0003,1002:TEST)-PrivateTag02": {
			"vr":	"AE",
			"val":	"AETITLE"
	},
	"(0003,1003:TEST)-PrivateTag03": {		
			"vr":	"AS",
			"val":	"034Y"
	}		
}
```

Note: the full `(gggg,eeee)-` string will be prefixed to the dataset keys if the keyword alone does not uniquely identify the tag (should only be for private and masked tags). In addition, if the value representation for a given DicomTag cannot be uniquely specified, it will be written into the document as a sub-element. This mainly occurs for private tags.

## Dicom Tag Types

With reference to the dicom types as represented in the fo-dicom library [here](https://github.com/HicServices/SMIPlugin/blob/master/Documentation/FoDicomElementClassDiagram.png), data are serialized as follows:

- **DicomStringElement**
	- Single element strings are stored as expected: `"SpecificCharacterSet": "ISO_IR 100"`   
	- Multi-string elements are stored using the `\` separator, as defined in the dicom standard: `"ImagePositionPatient": "-80.30762\\-111.3076\\52.00000"`

- **DicomAttributeTag**
	- This has a special representation in fo-dicom, but will be serialized as a string element with the above rules: `"SelectorATValue": "00080018\\0020000E"`

- **DicomValueElement**
	- Will be stored in BsonArrays: `"AcquisitionMatrix": [0, 265, 256, 0]`. The default [BsonTypeMapper](https://github.com/mongodb/mongo-csharp-driver/blob/master/src/MongoDB.Bson/ObjectModel/BsonTypeMapper.cs) is used for conversion. - Note that the available Bson types do not fully cover the C# types. This means that some elements like single-precision floats have to be stored as BsonDoubles.

- **DicomSequence**
	- Sequences are represented in fo-dicom as arrays of sub-datasets. Similarly, we serialize them in MongoDB as arrays of sub-objects, where each sub-object contains the elements of each item in the sequence:

```javascript
	"DeidentificationMethodCodeSequence":
	[
		{
			"CodeValue": "113100",
			"CodingSchemeDesignator": "DCM",
			"CodeMeaning": "Basic Application Confidentiality Profile"
		},
		{
			"CodeValue": "113101",
			"CodingSchemeDesignator": "DCM",
			"CodeMeaning": "Clean Pixel Data Option"
		},
		...
	]
```

- **Binary and Unknown Tags**
	- Tags of VR `OB`, `OW`, and `UN` represent binary or unknown data and will have their values set to `BsonNull` when storing in the `BsonDocument` format. In addition, they will not be included in the reconstructed `DicomDataset`
	- This functionality is controlled by the `DicomBsonVrBlacklist` in the main [DicomTypeTranslater](../DicomTypeTranslation/DicomTypeTranslater.cs) class.

- **Tags with no content**
	- Tags can be present in a dicom file with an empty data buffer. These will be written with `BsonNull` as their value, irrespective of their data type
