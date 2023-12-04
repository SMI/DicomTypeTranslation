using FellowOakDicom;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.Oracle;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DicomTypeTranslation.Tests;

class ExampleUsages
{
    [Test]
    public void ExampleUsage_Simple()
    {
        //create an Fo-Dicom dataset with a single string
        var ds = new DicomDataset(new List<DicomItem>()
        {
            new DicomShortString(DicomTag.PatientName,"Frank"),
            new DicomAgeString(DicomTag.PatientAge,"032Y"),
            new DicomDate(DicomTag.PatientBirthDate,new DateTime(2001,1,1))
        });

        var name = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
        Assert.Multiple(() =>
        {
            Assert.That(name.GetType(), Is.EqualTo(typeof(string)));
            Assert.That(name, Is.EqualTo("Frank"));
        });

        var age = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientAge);
        Assert.Multiple(() =>
        {
            Assert.That(age.GetType(), Is.EqualTo(typeof(string)));
            Assert.That(age, Is.EqualTo("032Y"));
        });

        var dob = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientBirthDate);
        Assert.Multiple(() =>
        {
            Assert.That(dob.GetType(), Is.EqualTo(typeof(DateTime)));
            Assert.That(dob, Is.EqualTo(new DateTime(2001, 01, 01)));
        });

        //create an Fo-Dicom dataset with string multiplicity. Cannot use name any more since fo-dicom enforces VM=1 for that.
        ds = new DicomDataset(new List<DicomItem>()
        {
            new DicomShortString(DicomTag.ReferringPhysicianTelephoneNumbers,"Frank","Anderson")
        });

        //Get the C# type
        var name2 = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.ReferringPhysicianTelephoneNumbers);
        Assert.Multiple(() =>
        {
            Assert.That(name2.GetType(), Is.EqualTo(typeof(string[])));
            Assert.That(name2, Is.EqualTo(new string[] { "Frank", "Anderson" }));
        });

        var name3 = DicomTypeTranslater.Flatten(name2);
        Assert.Multiple(() =>
        {
            Assert.That(name3.GetType(), Is.EqualTo(typeof(string)));
            Assert.That(name3, Is.EqualTo("Frank\\Anderson"));
        });

        //create an Fo-Dicom dataset with a sequence
        ds = new DicomDataset(new List<DicomItem>()
        {
            new DicomUniqueIdentifier(DicomTag.SOPInstanceUID,"1.2.3"),
            new DicomSequence(DicomTag.ActualHumanPerformersSequence,new []
            {
                new DicomDataset(new List<DicomItem>()
                {
                    new DicomShortString(DicomTag.PatientName,"Rabbit")
                }),
                new DicomDataset(new List<DicomItem>()
                {
                    new DicomShortString(DicomTag.PatientName,"Roger")
                })
            })
        });

        var seq = (Dictionary<DicomTag, object>[])DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.ActualHumanPerformersSequence);
        Assert.Multiple(() =>
        {
            Assert.That(seq[0][DicomTag.PatientName], Is.EqualTo("Rabbit"));
            Assert.That(seq[1][DicomTag.PatientName], Is.EqualTo("Roger"));
        });

        var flattened = (string)DicomTypeTranslater.Flatten(seq);
        Assert.That(
            flattened.Replace("\r", ""), Is.EqualTo(@"[0] - 
 	 (0010,0010) - 	 Rabbit

 [1] - 
 	 (0010,0010) - 	 Roger".Replace("\r", "")));
    }

    [Test]
    public void ExampleUsage_Types()
    {
        var tag = DicomDictionary.Default["PatientAddress"];

        var type = DicomTypeTranslater.GetNaturalTypeForVr(tag.DictionaryEntry.ValueRepresentations, tag.DictionaryEntry.ValueMultiplicity);

        Assert.Multiple(() =>
        {
            Assert.That(type.CSharpType, Is.EqualTo(typeof(string)));
            Assert.That(type.Width, Is.EqualTo(64));

            Assert.That(MicrosoftSQLTypeTranslater.Instance.GetSQLDBTypeForCSharpType(type), Is.EqualTo("varchar(64)"));
            Assert.That(OracleTypeTranslater.Instance.GetSQLDBTypeForCSharpType(type), Is.EqualTo("varchar2(64)"));
        });

    }
}