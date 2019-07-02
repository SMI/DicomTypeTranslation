# MongoDB Schema

This document describes how the dicom data format is serialized into MongoDB, and what other fields are included in the schema.

## Schema format

Below is the general schema format for image-level data.

Keys are actually strings, but surrounding quote marks omitted here.

```
{
	_id: ObjectId("1234567890"),  // Auto-generated ID, can be deserialized to DateTime of document creation
	header: 
	{
		NationalPACSAccessionNumber: "1234", 	// String, since technically this is just a directory name
		DicomFilePath: "Path\\To\\File.dcm", 	// Note escaped backslashes
		
		// Also present in the tags below, pulled out here for convenience
		StudyInstanceUID: "1.2.3.4", 			
		SeriesInstanceUID: "1.2.3.4",
		SOPInstanceUID: "1.2.3.4"
	},
	
	SpecificCharacterSet: "ISO_IR 100",		
	// Rest of the document contains all the Dicom Tags stripped from the file
}
```

Note: the full `(gggg,eeee)-` string will be prefixed to the dataset keys if the keyword alone does not uniquely identify the tag (should only be for private and masked tags).
## Dicom Tags

With reference to the dicom types as represented in the fo-dicom library [here](https://github.com/HicServices/SMIPlugin/blob/master/Documentation/FoDicomElementClassDiagram.png), data are serialized as follows:

 - **DicomStringElement**:
	 - Single element strings are stored as expected:
   
   `SpecificCharacterSet: "ISO_IR 100"`
   
	- Multi-string elements are stored using the `\` separator, as defined in the dicom standard:
  
  `ImagePositionPatient: "-80.30762\-111.3076\52.00000"`


- **DicomAttributeTag**:
	- This has a special representation in fo-dicom, but will be serialized as a string element with the above rules.
`OriginalImageIdentification: "(0010,0010)"`


- **DicomValueElement**:
	-  Value tags containing a single element will be stored as-is, value tags containing multiple elements will be stored as an array of their elements:
`AcquisitionMatrix: [0, 265, 256, 0]`
	- The encodings for the different value types are<sup>1</sup>  :
```
byte    -> BsonBinaryData
short   -> BsonInt32
ushort  -> BsonInt32
int     -> BsonInt32
uint    -> BsonInt64
float   -> BsonDouble
double  -> BsonDouble
decimal -> BsonDecimal128
```

- **DicomSequence**
	- Sequences are represented in fo-dicom as arrays of sub-datasets. Similarly, we serialize them in MongoDB as arrays of sub-objects, where each sub-object contains the elements of each item in the sequence:
```
	DeidentificationMethodCodeSequence:
	[
		{
			CodeValue: "113100",
			CodingSchemeDesignator: "DCM",
			CodeMeaning: "Basic Application Confidentiality Profile"
		},
		{
			CodeValue: "113101",
			CodingSchemeDesignator: "DCM",
			CodeMeaning: "Clean Pixel Data Option"
		},
		...
	]
```

- **Tags with no content**
	- Tags can be present in a dicom file with an empty data buffer. These will be present in MongoDB with `null` as their value, irrespective of their data type.

1 - Note that the available MongoDB (Bson) value types do not fully cover the possible .NET types, so some type conversions will have to be made.
