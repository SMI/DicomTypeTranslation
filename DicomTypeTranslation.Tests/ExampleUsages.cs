using FellowOakDicom;
using FAnsi.Discovery.TypeTranslation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.Oracle;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DicomTypeTranslation.Tests;

internal class ExampleUsages
{
    [Test]
    public void ExampleUsage_Simple()
    {
        //create an Fo-Dicom dataset with a single string
        var ds = new DicomDataset(new List<DicomItem>
        {
            new DicomShortString(DicomTag.PatientName,"Frank"),
            new DicomAgeString(DicomTag.PatientAge,"032Y"),
            new DicomDate(DicomTag.PatientBirthDate,new DateTime(2001,1,1))
        });

        var name = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
        Assert.AreEqual(typeof(string), name.GetType());
        Assert.AreEqual("Frank", name);

        var age = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientAge);
        Assert.AreEqual(typeof(string), age.GetType());
        Assert.AreEqual("032Y", age);

        var dob = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientBirthDate);
        Assert.AreEqual(typeof(DateTime), dob.GetType());
        Assert.AreEqual(new DateTime(2001, 01, 01), dob);

        //create an Fo-Dicom dataset with string multiplicity
        ds = new DicomDataset(new List<DicomItem>
        {
            new DicomShortString(DicomTag.PatientName,"Frank","Anderson")
        });

        //Get the C# type
        var name2 = DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.PatientName);
        Assert.AreEqual(typeof(string[]), name2.GetType());
        Assert.AreEqual(new[] { "Frank", "Anderson" }, name2);

        name2 = DicomTypeTranslater.Flatten(name2);
        Assert.AreEqual(typeof(string), name2.GetType());
        Assert.AreEqual("Frank\\Anderson", name2);

        //create an Fo-Dicom dataset with a sequence
        ds = new DicomDataset(new List<DicomItem>
        {
            new DicomUniqueIdentifier(DicomTag.SOPInstanceUID,"1.2.3"),
            new DicomSequence(DicomTag.ActualHumanPerformersSequence, new DicomDataset(new List<DicomItem>
            {
                new DicomShortString(DicomTag.PatientName,"Rabbit")
            }), new DicomDataset(new List<DicomItem>
            {
                new DicomShortString(DicomTag.PatientName,"Roger")
            }))
        });

        var seq = (Dictionary<DicomTag, object>[])DicomTypeTranslaterReader.GetCSharpValue(ds, DicomTag.ActualHumanPerformersSequence);
        Assert.AreEqual("Rabbit", seq[0][DicomTag.PatientName]);
        Assert.AreEqual("Roger", seq[1][DicomTag.PatientName]);

        var flattened = (string)DicomTypeTranslater.Flatten(seq);
        Assert.AreEqual(
            @"[0] - 
 	 (0010,0010) - 	 Rabbit

 [1] - 
 	 (0010,0010) - 	 Roger".Replace("\r", ""), flattened.Replace("\r", ""));
    }

    [Test]
    public void ExampleUsage_Types()
    {
        var tag = DicomDictionary.Default["PatientAddress"];

        var type = DicomTypeTranslater.GetNaturalTypeForVr(tag.DictionaryEntry.ValueRepresentations, tag.DictionaryEntry.ValueMultiplicity);

        Assert.AreEqual(typeof(string), type.CSharpType);
        Assert.AreEqual(64, type.Width);

        TypeTranslater tt = new MicrosoftSQLTypeTranslater();
        Assert.AreEqual("varchar(64)", tt.GetSQLDBTypeForCSharpType(type));

        tt = new OracleTypeTranslater();
        Assert.AreEqual("varchar2(64)", tt.GetSQLDBTypeForCSharpType(type));

    }
}