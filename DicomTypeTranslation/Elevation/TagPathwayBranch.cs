using System.Collections.Generic;
using FellowOakDicom;

namespace DicomTypeTranslation.Elevation;

internal class SequenceElement
{
    public SequenceElement Parent { get; }

    public List<SequenceElement> ArraySiblings { get; }

    public DicomTag SequenceTag { get; }

    public Dictionary<DicomTag, object> Dataset { get; }
        
    public SequenceElement(DicomTag sequenceTag, Dictionary<DicomTag, object> dataset, SequenceElement parentIfAny = null)
    {
        SequenceTag = sequenceTag;
        Dataset = dataset;
        Parent = parentIfAny;
        ArraySiblings = new List<SequenceElement>();
    }
}