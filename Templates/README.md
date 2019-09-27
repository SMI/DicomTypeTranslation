# Templates

- [CT](#ct-computerised-tomography)
- [MR](#mr-magnetic-resonance)
- [OTHER](#other)

## CT (Computerised Tomography)

Schema with 

### StudyTable

| Field | Description |
| ------------- | ------------- |
| PatientID |  |
| StudyInstanceUID |  |
| StudyDate |  |
| StudyTime |  |
| ModalitiesInStudy |  |
| StudyDescription |  |
| AccessionNumber |  |
| PatientSex |  |
| PatientAge |  |
| NumberOfStudyRelatedInstances |  |
| PatientBirthDate |  |

### SeriesTable

| Field | Description |
| ------------- | ------------- |
| StudyInstanceUID |  |
| SeriesInstanceUID |  |
| Modality |  |
| InstitutionName |  |
| ProtocolName |  |
| ProcedureCodeSequence_CodeValue |  |
| PerformedProcedureStepDescription |  |
| SeriesDescription |  |
| SeriesDate |  |
| SeriesTime |  |
| ImageType |  |
| BodyPartExamined |  |
| DeviceSerialNumber |  |
| NumberOfSeriesRelatedInstances |  |
| SeriesNumber |  |

### ImageTable

| Field | Description |
| ------------- | ------------- |
| SeriesInstanceUID |  |
| SOPInstanceUID |  |
| BurnedInAnnotation |  |
| RelativeFileArchiveURI |  |
| MessageGuid |  |
| SliceLocation |  |
| SliceThickness |  |
| SpacingBetweenSlices |  |
| SpiralPitchFactor |  |
| KVP |  |
| ExposureTime |  |
| Exposure |  |
| ImageType |  |
| ManufacturerModelName |  |
| Manufacturer |  |
| SoftwareVersions |  |
| XRayTubeCurrent |  |
| PhotometricInterpretation |  |
| ContrastBolusRoute |  |
| ContrastBolusAgent |  |
| AcquisitionNumber |  |
| AcquisitionDate |  |
| AcquisitionTime |  |
| ImagePositionPatient |  |
| PixelSpacing |  |
| FieldOfViewDimensions |  |
| FieldOfViewDimensionsInFloat |  |
| DerivationDescription |  |
| LossyImageCompression |  |
| LossyImageCompressionMethod |  |
| LossyImageCompressionRatio |  |
| LossyImageCompressionRetired |  |
| ScanOptions |  |
## MR (Magnetic Resonance)

### StudyTable

| Field | Description |
| ------------- | ------------- |
| PatientID |  |
| StudyInstanceUID |  |
| StudyDate |  |
| StudyTime |  |
| ModalitiesInStudy |  |
| StudyDescription |  |
| AccessionNumber |  |
| PatientSex |  |
| PatientAge |  |
| PatientBirthDate |  |
| NumberOfStudyRelatedInstances |  |

### SeriesTable

| Field | Description |
| ------------- | ------------- |
| StudyInstanceUID |  |
| SeriesInstanceUID |  |
| Modality |  |
| InstitutionName |  |
| ProtocolName |  |
| ProcedureCodeSequence_CodeValue |  |
| PerformedProcedureStepDescription |  |
| SeriesDescription |  |
| SeriesDate |  |
| SeriesTime |  |
| ImageType |  |
| BodyPartExamined |  |
| DeviceSerialNumber |  |
| NumberOfSeriesRelatedInstances |  |
| SeriesNumber |  |
| MRAcquisitionType |  |
| AngioFlag |  |
| MagneticFieldStrength |  |
| TransmitCoilName |  |
| PatientPosition |  |

### ImageTable

| Field | Description |
| ------------- | ------------- |
| SeriesInstanceUID |  |
| SOPInstanceUID |  |
| BurnedInAnnotation |  |
| RelativeFileArchiveURI |  |
| MessageGuid |  |
| SliceLocation |  |
| SliceThickness |  |
| SpacingBetweenSlices |  |
| ImageType |  |
| ManufacturerModelName |  |
| Manufacturer |  |
| SoftwareVersions |  |
| PhotometricInterpretation |  |
| ContrastBolusRoute |  |
| ContrastBolusAgent |  |
| AcquisitionNumber |  |
| AcquisitionDate |  |
| AcquisitionTime |  |
| ImagePositionPatient |  |
| ImageOrientationPatient |  |
| PixelSpacing |  |
| FieldOfViewDimensions |  |
| FieldOfViewDimensionsInFloat |  |
| DerivationDescription |  |
| LossyImageCompression |  |
| LossyImageCompressionMethod |  |
| LossyImageCompressionRatio |  |
| LossyImageCompressionRetired |  |
| ScanOptions |  |
| EchoTime |  |
| EchoNumbers |  |
| EchoTrainLength |  |
| InversionTime |  |
| RepetitionTime |  |
| NumberOfPhaseEncodingSteps |  |
| FlipAngle |  |
| VariableFlipAngleFlag |  |
| SequenceName |  |

## OTHER

### ImageTable

| Field | Description |
| ------------- | ------------- |
| PatientID |  |
| StudyInstanceUID |  |
| StudyDate |  |
| StudyTime |  |
| ModalitiesInStudy |  |
| StudyDescription |  |
| PatientBirthDate |  |
| AccessionNumber |  |
| PatientSex |  |
| PatientAge |  |
| NumberOfStudyRelatedInstances |  |
| SeriesInstanceUID |  |
| Modality |  |
| SourceApplicationEntityTitle |  |
| InstitutionName |  |
| ProcedureCodeSequence |  |
| ProtocolName |  |
| PerformedProcedureStepID |  |
| PerformedProcedureStepDescription |  |
| SeriesDescription |  |
| BodyPartExamined |  |
| DeviceSerialNumber |  |
| NumberOfSeriesRelatedInstances |  |
| SeriesNumber |  |
| SequenceName |  |
| SOPInstanceUID |  |
| SeriesDate |  |
| SeriesTime |  |
| BurnedInAnnotation |  |
| RelativeFileArchiveURI |  |
| MessageGuid |  |
| SliceLocation |  |
| SliceThickness |  |
| SpacingBetweenSlices |  |
| ImageType |  |
| ManufacturerModelName |  |
| Manufacturer |  |
| PhotometricInterpretation |  |
