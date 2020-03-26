# Templates

- [CT](#ct-computerised-tomography)
- [MR](#mr-magnetic-resonance)
- [PT](#pt-positron-emission-tomography-PET)
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
| BodyPartExamined |  |
| DeviceSerialNumber |  |
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
| BodyPartExamined |  |
| DeviceSerialNumber |  |
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

## PT (Positron emission tomography (PET))

What is a PET scan

-  https://youtu.be/GHLBcCv4rqk

Example Dataset
- https://wiki.cancerimagingarchive.net/display/Public/Head-Neck-PET-CT

Observations

- PET is normally combined with a CT at the same time



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
| BodyPartExamined |  |
| DeviceSerialNumber |  |
| SeriesNumber |  |
| NumberOfSlices |  |
| EnergyWindowRangeSequence_EnergyWindowLowerLimit |  |
| EnergyWindowRangeSequence_EnergyWindowUpperLimit |  |
| RadiopharmaceuticalInformationSequence_Radiopharmaceutical |  |
| RadiopharmaceuticalInformationSequence_RadiopharmaceuticalVolume |  |
| RadiopharmaceuticalInformationSequence_RadionuclideCodeSequence_CodeValue |  |
| RadiopharmaceuticalInformationSequence_RadionuclideCodeSequence_CodeMeaning |  |
| RadiopharmaceuticalInformationSequence_RadionuclideTotalDose |  |
| RandomsCorrectionMethod |  |
| AttenuationCorrectionMethod |  |
| DecayCorrection |  |
| ReconstructionMethod |  |
| ScatterCorrectionMethod |  |
| DateOfLastCalibration |  |
|TimeOfLastCalibration | |

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
| ActualFrameDuration | |


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
