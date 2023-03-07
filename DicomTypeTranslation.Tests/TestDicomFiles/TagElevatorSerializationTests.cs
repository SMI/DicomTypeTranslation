using DicomTypeTranslation.Elevation.Serialization;
using NUnit.Framework;

namespace DicomTypeTranslation.Tests.ElevationTests;

public class TagElevatorSerializationTests
{
    [Test]
    public void Deserialize_SingleRequest()
    {
        const string xml = @"<!DOCTYPE TagElevationRequestCollection
[
  <!ELEMENT TagElevationRequestCollection (TagElevationRequest*)>
  <!ELEMENT TagElevationRequest (ColumnName,ElevationPathway,Conditional?)>
  <!ELEMENT ColumnName (#PCDATA)>
  <!ELEMENT ElevationPathway (#PCDATA)>
  <!ELEMENT Conditional (ConditionalPathway,ConditionalRegex)>
  <!ELEMENT ConditionalPathway (#PCDATA)>
  <!ELEMENT ConditionalRegex (#PCDATA)>
]>

<TagElevationRequestCollection>
  <TagElevationRequest>
    <ColumnName>ContentSequenceDescriptions</ColumnName>
    <ElevationPathway>ContentSequence->TextValue</ElevationPathway>
    <Conditional>
      <ConditionalPathway>.->ConceptNameCodeSequence->CodeMeaning</ConditionalPathway>
      <ConditionalRegex>Tr.*[e-a]{2}tment</ConditionalRegex>
    </Conditional>
  </TagElevationRequest>
</TagElevationRequestCollection>";


        var collection = new TagElevationRequestCollection(xml);
             
        Assert.AreEqual(1,collection.Requests.Count);
            
        Assert.AreEqual("ContentSequence->TextValue", collection.Requests[0].ElevationPathway);
        Assert.AreEqual("ContentSequenceDescriptions", collection.Requests[0].ColumnName);
        Assert.AreEqual(".->ConceptNameCodeSequence->CodeMeaning", collection.Requests[0].ConditionalPathway);
        Assert.AreEqual("Tr.*[e-a]{2}tment", collection.Requests[0].ConditionalRegex);
    }

    [Test]
    public void Deserialize_TwoRequest_OneWithConditional()
    {
        const string xml = @"<!DOCTYPE TagElevationRequestCollection
[
  <!ELEMENT TagElevationRequestCollection (TagElevationRequest*)>
  <!ELEMENT TagElevationRequest (ColumnName,ElevationPathway,Conditional?)>
  <!ELEMENT ColumnName (#PCDATA)>
  <!ELEMENT ElevationPathway (#PCDATA)>
  <!ELEMENT Conditional (ConditionalPathway,ConditionalRegex)>
  <!ELEMENT ConditionalPathway (#PCDATA)>
  <!ELEMENT ConditionalRegex (#PCDATA)>
]>

<TagElevationRequestCollection>
  <TagElevationRequest>
    <ColumnName>ContentSequenceDescriptions</ColumnName>
    <ElevationPathway>ContentSequence->TextValue</ElevationPathway>
    <Conditional>
      <ConditionalPathway>.->ConceptNameCodeSequence->CodeMeaning</ConditionalPathway>
      <ConditionalRegex>Tr.*[e-a]{2}tment</ConditionalRegex>
    </Conditional>
  </TagElevationRequest>
<TagElevationRequest>
    <ColumnName>ContentSequenceFreeText</ColumnName>
    <ElevationPathway>ContentSequence->TextString</ElevationPathway>
  </TagElevationRequest>
</TagElevationRequestCollection>";
            
        var collection = new TagElevationRequestCollection(xml);

        Assert.AreEqual(2, collection.Requests.Count);

        Assert.AreEqual("ContentSequenceDescriptions", collection.Requests[0].ColumnName);
        Assert.AreEqual("ContentSequence->TextValue", collection.Requests[0].ElevationPathway);
        Assert.AreEqual(".->ConceptNameCodeSequence->CodeMeaning", collection.Requests[0].ConditionalPathway);
        Assert.AreEqual("Tr.*[e-a]{2}tment", collection.Requests[0].ConditionalRegex);

        Assert.AreEqual("ContentSequenceFreeText", collection.Requests[1].ColumnName);
        Assert.AreEqual("ContentSequence->TextString", collection.Requests[1].ElevationPathway);
        Assert.AreEqual(null, collection.Requests[1].ConditionalPathway);
        Assert.AreEqual(null, collection.Requests[1].ConditionalRegex);
    }
}