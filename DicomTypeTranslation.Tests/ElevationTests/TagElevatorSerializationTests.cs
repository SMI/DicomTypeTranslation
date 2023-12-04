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

        Assert.That(collection.Requests, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(collection.Requests[0].ElevationPathway, Is.EqualTo("ContentSequence->TextValue"));
            Assert.That(collection.Requests[0].ColumnName, Is.EqualTo("ContentSequenceDescriptions"));
            Assert.That(collection.Requests[0].ConditionalPathway, Is.EqualTo(".->ConceptNameCodeSequence->CodeMeaning"));
            Assert.That(collection.Requests[0].ConditionalRegex, Is.EqualTo("Tr.*[e-a]{2}tment"));
        });
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

        Assert.That(collection.Requests, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(collection.Requests[0].ColumnName, Is.EqualTo("ContentSequenceDescriptions"));
            Assert.That(collection.Requests[0].ElevationPathway, Is.EqualTo("ContentSequence->TextValue"));
            Assert.That(collection.Requests[0].ConditionalPathway, Is.EqualTo(".->ConceptNameCodeSequence->CodeMeaning"));
            Assert.That(collection.Requests[0].ConditionalRegex, Is.EqualTo("Tr.*[e-a]{2}tment"));

            Assert.That(collection.Requests[1].ColumnName, Is.EqualTo("ContentSequenceFreeText"));
            Assert.That(collection.Requests[1].ElevationPathway, Is.EqualTo("ContentSequence->TextString"));
            Assert.That(collection.Requests[1].ConditionalPathway, Is.EqualTo(null));
            Assert.That(collection.Requests[1].ConditionalRegex, Is.EqualTo(null));
        });
    }
}