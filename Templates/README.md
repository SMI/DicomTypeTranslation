# Templates

- [CT](#ct-computerised-tomography)
- [MR](#mr-magnetic-resonance)
- [PT](#pt-positron-emission-tomography-PET)
- [NM](#nm-nuclear-medicine)
- [DX](#dx-digital-radiography)
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
| EnergyWindowRange_EnergyWindowLowerLimit |  |
| EnergyWindowRange_EnergyWindowUpperLimit |  |
| Radiopharmaceutical_Radiopharmaceutical |  |
| Radiopharmaceutical_RadiopharmaceuticalVolume |  |
| Radiopharmaceutical_RadionuclideCode_CodeValue |  |
| Radiopharmaceutical_RadionuclideCode_CodeMeaning |  |
| Radiopharmaceutical_RadionuclideTotalDose |  |
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


## NM (Nuclear Medicine)

This modality contains both SPECT and other forms of nuclear medicine.

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
| EnergyWindowRange_EnergyWindowLowerLimit |  |
| EnergyWindowRange_EnergyWindowUpperLimit |  |
| Radiopharmaceutical_Radiopharmaceutical |  |
| Radiopharmaceutical_RadiopharmaceuticalVolume |  |
| Radiopharmaceutical_RadionuclideCode_CodeValue |  |
| Radiopharmaceutical_RadionuclideCode_CodeMeaning |  |
| Radiopharmaceutical_RadionuclideTotalDose |  |
|NumberOfDetectors                                         | |
|DetectorInformation_CollimatorGridName            | |
|DetectorInformation_CollimatorType                | |
|DetectorInformation_FieldOfViewShape              | |
|DetectorInformation_FieldOfViewDimensions         | |
|DetectorInformation_ZoomFactor                    | |
|DetectorInformation_RadialPosition                | |
|RotationInformation_ScanArc                       | |
|RotationInformation_AngularStep                   | |
|RotationInformation_NumberOfFramesInRotation      | |
|TypeOfDetectorMotion                                      | |
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

## DX Digital Radiography

For a great introduction to medical imaging and specifically X-Rays check out:

https://www.youtube.com/watch?v=cTwbSDcCN4E

### StudyTable


| Field | Description |
| ------------- | ------------- |
|PatientID| |
|StudyInstanceUID| |
|StudyDate| |
|StudyTime| |
|ModalitiesInStudy| |
|StudyDescription| |
|AccessionNumber| |
|PatientSex| |
|PatientAge| |
|NumberOfStudyRelatedInstances| |
|PatientBirthDate| |

### SeriesTable


| Field | Description |
| ------------- | ------------- |
|StudyInstanceUID| |
|SeriesInstanceUID| |
|Modality| |
|InstitutionName| |
|ProtocolName| |
|ProcedureCodeSequence_CodeValue| |
|PerformedProcedureStepDescription| |
|SeriesDescription| |
|SeriesDate| |
|SeriesTime| |
|BodyPartExamined| |
|DeviceSerialNumber| |
|SeriesNumber| |

### ImageTable

| Field | Description |
| ------------- | ------------- |
|SeriesInstanceUID| |
|SOPInstanceUID| |
|BurnedInAnnotation| |
|RelativeFileArchiveURI| |
|MessageGuid| |
|KVP| |
|ExposureTime| |
|Exposure| |
|ImageType| |
|ManufacturerModelName| |
|Manufacturer| |
|SoftwareVersions| |
|XRayTubeCurrent| |
|PhotometricInterpretation| |
|AcquisitionNumber| |
|AcquisitionDate| |
|AcquisitionTime| |
|ImagePositionPatient| |
|PixelSpacing| |
|DerivationDescription| |
|LossyImageCompression| |
|LossyImageCompressionMethod| |
|LossyImageCompressionRatio| |
|LossyImageCompressionRetired| |
|ScanOptions| |
|SpatialResolution| |
|DistanceSourceToDetector| |
|ExposureInuAs| |
|DistanceSourceToPatient| |
|EstimatedRadiographicMagnificationFactor| |
|ImageAndFluoroscopyAreaDoseProduct| |
|Grid| |
|Rows| |
|Columns| |
|LongitudinalTemporalInformationModified| |
|PixelIntensityRelationshipSign| |
|WindowCenter| |
|WindowWidth| |
|RescaleIntercept| |
|RescaleSlope| |
|RescaleType| |
|CollimatorShape| |
|CollimatorLeftVerticalEdge| |
|CollimatorRightVerticalEdge| |
|CollimatorUpperHorizontalEdge| |
|CollimatorLowerHorizontalEdge| |
|ViewPosition| |
|DetectorTemperature| |
|DetectorType| |
|DetectorMode| |
|DetectorID| |
|PositionerType| |
|RelativeXRayExposure| |
|AcquisitionDeviceProcessingDescription| |

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
