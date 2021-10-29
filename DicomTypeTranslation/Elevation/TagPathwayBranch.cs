using System.Collections.Generic;
using FellowOakDicom;

namespace DicomTypeTranslation.Elevation
{
    class SequenceElement
    {
        public SequenceElement Parent { get; private set; }

        public List<SequenceElement> ArraySiblings { get; private set; }

        public DicomTag SequenceTag { get; private set; }

        public Dictionary<DicomTag, object> Dataset { get; private set; }
        
        public SequenceElement(DicomTag sequenceTag, Dictionary<DicomTag, object> dataset, SequenceElement parentIfAny = null)
        {
            SequenceTag = sequenceTag;
            Dataset = dataset;
            Parent = parentIfAny;
            ArraySiblings = new List<SequenceElement>();
        }
    }
}
