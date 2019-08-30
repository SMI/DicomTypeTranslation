
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dicom;
using DicomTypeTranslation.Elevation;
using DicomTypeTranslation.Elevation.Exceptions;
using DicomTypeTranslation.Helpers;
using FAnsi.Discovery;
using NUnit.Framework;

namespace DicomTypeTranslation.Tests.ElevationTests
{
    //Includes sample dicom files taken from http://www.dclunie.com/medical-image-faq/html/index.html
    public class TagElevatorTests
    {
        private static readonly string _dcmDir = Path.Combine(TestContext.CurrentContext.TestDirectory ,"TestDicomFiles");

        private readonly string _srDcmPath = Path.Combine(_dcmDir, "report01.dcm");
        private readonly string _imDcmPath = Path.Combine(_dcmDir, "image11.dcm");


        /// <summary>
        /// Tests that it is illegal to elevate a top level tag
        /// </summary>
        [Test]
        public void TagElevation_SingleNode_Throws()
        {
            Assert.Throws<InvalidTagElevatorPathException>(() => new TagElevator("LUTExplanation"));
        }

        /// <summary>
        /// Tests that the pathway finishing with an SQ element is illegal
        /// </summary>
        [Test]
        public void TagElevation_SQTerminator_Throws()
        {
            Assert.Throws<TagNavigationException>(() => new TagElevator("ModalityLUTSequence->ModalityLUTSequence"));
        }

        [Test]
        public void AreSequenceTagsAmbiguous()
        {
            //For all tags
            foreach (DicomDictionaryEntry entry in DicomDictionary.Default)
            {
                //that can be Sequences
                if (entry.ValueRepresentations.Any(v => v == DicomVR.SQ))
                    Assert.AreEqual(entry.ValueRepresentations.Length, 1, "Failed on " + entry.Name); //can they ONLY be sequences (pretty please!)
            }
        }
        [Test]
        public void DoWeNeedToMakeTagsSane()
        {
            Dictionary<string, string> dodgy = new Dictionary<string, string>();

            //For all tags
            foreach (DicomDictionaryEntry entry in DicomDictionary.Default)
            {
                //that can be Sequences
                if (entry.Keyword != QuerySyntaxHelper.MakeHeaderNameSensible(entry.Keyword))
                    dodgy.Add(entry.Keyword, QuerySyntaxHelper.MakeHeaderNameSensible(entry.Keyword));
            }

            foreach (var kvp in dodgy)
                Console.WriteLine(kvp.Key + "|" + kvp.Value);

            //no we don't
            Assert.AreEqual(0, dodgy.Count);
        }


        //tests reading a SEQuence
        [Test]
        public void TagElevation_TwoDeep_FindValue()
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.image11);

            var file = DicomFile.Open(_imDcmPath);

            var elevator = new TagElevator("ModalityLUTSequence->LUTExplanation");

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNotNull(file);
            Assert.AreEqual(value, "KESPR LUT");
        }

        //tests reading a non existant tag
        [TestCase("ContentSequence->TextValue")]
        [TestCase("ModalityLUTSequence->TextValue+")]
        public void TagElevation_TagNotFound_ReturnsNull(string path)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.image11);

            var file = DicomFile.Open(_imDcmPath);

            var elevator = new TagElevator(path);

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNotNull(file);
            Assert.IsNull(value);
        }

        //tests reading a non existant tag
        [TestCase("ReferencedPerformedProcedureStepSequence->TextValue+")]
        public void TagElevation_TagNotFoundComplex_ReturnsNull(string path)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator(path);

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNotNull(file);
            Assert.IsNull(value);
        }

        [Test]
        public void TagElevation_ThreeDeep_FindValue()
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            //for debugging purposes
            ShowContentSequence(file.Dataset);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning+");

            object value = elevator.GetValue(file.Dataset);

            string expected =
                $"Observer Name{Environment.NewLine}Observer Organization Name{Environment.NewLine}Description{Environment.NewLine}Diagnosis{Environment.NewLine}Treatment";
            Assert.AreEqual(expected, value);
        }

        //tests reading a multiple matching leaf nodes (TextValue) from a top level tag (ContentSequence)
        [Test]
        public void TagElevation_TwoDeepReport_FindValue()
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->TextValue+");

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNotNull(file);
            Assert.IsTrue(value.ToString().Contains("Redlands Clinic"));
            Assert.IsTrue(value.ToString().Contains("This 78-year-old gentleman referred by Dr"));
            Assert.IsTrue(value.ToString().Contains(" involving the skin of the left external ear, "));

            Assert.IsTrue(value.ToString().Contains("possibility of complication was discussed with this patient at some length, and he accepted therapy as outlined."));
        }

        //tests reading a multiple matching leaf nodes (TextValue) from a top level tag (ContentSequence)
        [Test]
        public void TagElevation_TwoDeepWithConditional_FindValue()
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->TextValue", ".->ConceptNameCodeSequence->CodeMeaning", "Observer Organization Name");

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNotNull(file);
            Assert.AreEqual("Redlands Clinic", value);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TagElevation_Multiplicity(bool specifyConcatenateMultiplicity)
        {
            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "abcd", "efgh"),
                    })
            };

            var searchString = "PseudoColorPaletteInstanceReferenceSequence->TextString";

            if (specifyConcatenateMultiplicity)
                searchString += "&";

            var elevator = new TagElevator(searchString, null, null);

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            if (specifyConcatenateMultiplicity)
            {
                //should pass when you do
                Assert.AreEqual("abcd\\efgh", elevator.GetValue(ds));
            }
            else
            {
                //should fail when you don't specify multiplicity concatenation
                var ex = Assert.Throws<TagNavigationException>(() => elevator.GetValue(ds));
                Assert.IsTrue(ex.Message.Contains("Multiplicity"));
            }
        }

        //tests the conditional path '[]' which specifies that the toFind regex should be applied directly to the array elements in the matching leaves multiplicities
        [TestCase("[hm]", "efgh\r\nhij\\klm")]
        [TestCase("a", "abcd")]
        [TestCase("avmamaged", null)]
        [TestCase("h", "efgh\r\nhij")]
        public void TagElevation_Multiplicity_Conditional(string toFind, string expectedResults)
        {
            if (!string.IsNullOrWhiteSpace(expectedResults))
                expectedResults = expectedResults.Replace("\r\n", Environment.NewLine);

            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "abcd", "efgh"),
                    },
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "hij", "klm"),
                    })
            };

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            var elevator = new TagElevator("PseudoColorPaletteInstanceReferenceSequence->TextString&+", "[]", toFind);
            Assert.AreEqual(expectedResults, elevator.GetValue(ds));
        }

        [Test]
        public void TagElevation_MultiplicityOperatorWhenNoMultiplicity()
        {
            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // No multiplicity!
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "abcd"),
                    })
            };

            var elevator = new TagElevator("PseudoColorPaletteInstanceReferenceSequence->TextString", "[]", "a");

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            //should pass when you do
            Assert.AreEqual("abcd", elevator.GetValue(ds));

            elevator = new TagElevator("PseudoColorPaletteInstanceReferenceSequence->TextString", "[]", "happy fun times");
            Assert.IsNull(elevator.GetValue(ds));
        }

        [Test]
        public void TagElevation_MultipleLeavesAndMultiplicity_FindValues()
        {
            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "abcd", "efgh"),
                    },
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "hij", "klm"),
                    })
            };

            const string searchString = "PseudoColorPaletteInstanceReferenceSequence->TextString&+";

            var elevator = new TagElevator(searchString, null, null);

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            //should pass when you do
            Assert.AreEqual($"abcd\\efgh{Environment.NewLine}hij\\klm", elevator.GetValue(ds));
        }


        [Test]
        public void TagElevation_MultipleLeavesAndMultiplicity_WithArraySiblingConditional_FindValues()
        {
            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->CodeValue
                        new DicomShortString(DicomTag.CodeValue, "CODE_01"),
                    },
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->CodeMeaning
                        new DicomShortString(DicomTag.CodeMeaning , "Description"),
                    })
            };

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            //this won't match because CodeValue is an array sibling of the Sequence PseudoColorPaletteInstanceReferenceSequence
            var elevator = new TagElevator("PseudoColorPaletteInstanceReferenceSequence->CodeMeaning", ".->CodeValue", "^CODE"); //match starting with code

            //should pass when you do
            Assert.IsNull(elevator.GetValue(ds));

            //use the Array sibling starter instead of the sequence sibling starter (i.e. [..] instead of .)
            elevator = new TagElevator("PseudoColorPaletteInstanceReferenceSequence->CodeMeaning", "[..]->CodeValue", "^CODE"); //match starting with code

            //should pass when you do
            Assert.AreEqual("Description", elevator.GetValue(ds));
        }

        //match current node as regex
        [TestCase(".", ".*ment$", true)]
        [TestCase(".", "^Treatment$", true)]
        [TestCase(".", "coconuts", false)]
        public void TagElevation_ConditionalCurrentNode(string conditional, string conditionalMatch, bool expectedToFind)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            if (expectedToFind)
                Assert.AreEqual("Treatment", value);
            else
                Assert.IsNull(value);
        }

        //match other tags that sit side by side the final leaf tag (i.e. other tags in "ContentSequence->ConceptNameCodeSequence")
        [TestCase(".->CodeValue", "CODE_03", true, false)]
        [TestCase(".->CodeMeaning", "Treatment", true, false)] //you can go back into the tag you just came out of in which case it's the same as . (kinda pointless though)
        [TestCase(".->CodingSchemeDesignator", "99_OFFIS_DCMTK", false, true)] //throws because all ContentSequence->ConceptNameCodeSequence->CodeMeaning are designated by the same value Offis...
        public void TagElevation_ConditionalCurrentNode_OtherTags(string conditional, string conditionalMatch, bool expectedToFind, bool expectMultipleMatchesException)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            if (expectMultipleMatchesException)
                Assert.Throws<TagNavigationException>(() => elevator.GetValue(file.Dataset));
            else
            {
                object value = elevator.GetValue(file.Dataset);

                if (expectedToFind)
                    Assert.AreEqual("Treatment", value);
                else
                    Assert.IsNull(value);
            }
        }

        [TestCase("..->TextValue", "The plan of treatment is as follows")] //parent SQ (i.e. ContentSequence->TextValue)
        [TestCase("..->ConceptNameCodeSequence->CodeMeaning", "Treatment")] //parent SQ (i.e. ContentSequence) but we go back into our own tag anyway! (note that if there were multiple ConceptNameCodeSequence in side by side datasets we would go down into all of them)
        public void TagElevation_DoubleDotConditional_FindValue(string conditional, string conditionalMatch)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.AreEqual("Treatment", value);
        }

        [TestCase("..->[..]->TextValue", "The plan of treatment is as follows")] //actually results in matching all elements of ContentSequence (not just [4]) so will match everyone
        public void TagElevation_DoubleDotConditional_ThenArrayElementBuddies_FindValue(string conditional, string conditionalMatch)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning+", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.AreEqual($"Observer Name{Environment.NewLine}Observer Organization Name{Environment.NewLine}Description{Environment.NewLine}Diagnosis{Environment.NewLine}Treatment", value);
        }


        [TestCase("..->[..]->[..]->..->TextValue", "The plan of treatment is as follows")]
        [TestCase("..->..->..->[..]->[..]->TextValue", "The plan of treatment is as follows")]
        [TestCase("[..]->[..]->..->..->TextValue", "The plan of treatment is as follows")]
        public void TagElevation_DoubleDotConditionalSpam_NeverMatch(string conditional, string conditionalMatch)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->ConceptNameCodeSequence->CodeMeaning&", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.IsNull(value);
        }


        [TestCase("[]", ".*Clinic$")]
        [TestCase(".", ".*Clinic$")]
        [TestCase(".->ConceptNameCodeSequence->CodeValue", "IHE.05")]
        public void TagElevation_TextValueConditionals_FindValue(string conditional, string conditionalMatch)
        {
            //File.WriteAllBytes(srDcmPath, TestStructuredReports.report01);

            var file = DicomFile.Open(_srDcmPath);

            var elevator = new TagElevator("ContentSequence->TextValue", conditional, conditionalMatch);

            ShowContentSequence(file.Dataset);

            object value = elevator.GetValue(file.Dataset);

            Assert.AreEqual("Redlands Clinic", value);
        }


        //complex sibling tests
        //no constraints PatientInsurancePlanCodeSequence->SpecimenShortDescription matches all 3 tags
        [TestCase(null, null, "2_0.1\r\n2_1.1\r\n2_2.1")]

        //look in current sequence array element for ProbeDriveEquipmentSequence->PatientID
        [TestCase(".->ProbeDriveEquipmentSequence->PatientID", "3_1.2", "2_1.1")]

        //look in all sibling sequences for ProbeDriveEquipmentSequence->PatientID
        [TestCase("[..]->ProbeDriveEquipmentSequence->PatientID", "3_1.2", "2_0.1\r\n2_1.1\r\n2_2.1")]

        //look in current sequence array element for SpecimenShortDescription
        [TestCase(".->SpecimenShortDescription", "2_1.1", "2_1.1")]

        //look in all sibling sequences for SpecimenShortDescription 2_1.1 (matches everything since they are all adjacent to one another)
        [TestCase("[..]->SpecimenShortDescription", "2_1.1", "2_0.1\r\n2_1.1\r\n2_2.1")]

        public void TagElevation_SiblingConditionals(string conditional, string conditionalMatch, string expectedResults)
        {
            expectedResults = expectedResults.Replace("\r\n", Environment.NewLine);

            var ds = new DicomDataset
                (
                        //root->PatientInsurancePlanCodeSequence [array of 3 sibling sequences]
                        new DicomSequence
                            (DicomTag.PatientInsurancePlanCodeSequence,
                                new DicomDataset
                                {
                                    //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence
                                    new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                    {
                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                        new DicomShortText(DicomTag.SpecimenShortDescription, "3_0.1"),

                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                        new DicomShortString(DicomTag.PatientID,"3_0.2")
                                    }),
                                
                                    //root->PatientInsurancePlanCodeSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "2_0.1")
                                },

                                new DicomDataset
                                {
                                    //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence
                                    new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                    {
                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                        new DicomShortText(DicomTag.SpecimenShortDescription, "3_1.1"),

                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                        new DicomShortString(DicomTag.PatientID,"3_1.2")
                                    }),
                                
                                    //root->PatientInsurancePlanCodeSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "2_1.1")
                                },


                                new DicomDataset
                                {
                                    //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence
                                    new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                    {
                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                        new DicomShortText(DicomTag.SpecimenShortDescription, "3_2.1"),

                                        //root->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                        new DicomShortString(DicomTag.PatientID,"3_2.2")
                                    }),
                                
                                    //root->PatientInsurancePlanCodeSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "2_2.1")
                                }
                            )
                    );

            var elevator = new TagElevator("PatientInsurancePlanCodeSequence->SpecimenShortDescription+", conditional, conditionalMatch);

            ShowContentSequence(ds, DicomTag.PatientInsurancePlanCodeSequence);

            object value = elevator.GetValue(ds);

            Assert.AreEqual(expectedResults, value);

        }


        //route tests
        [TestCase("ContentSequence->TextValue", null, null, null, TestName = "Complex_1")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->SpecimenShortDescription", null, null, "1.1", TestName = "Complex_2")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription+", null, null, "2.1\r\n2.1", TestName = "Complex_3")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence->PatientID", null, null, "2.2", TestName = "Complex_4")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription", null, null, "3.1", TestName = "Complex_5")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence->PatientID", null, null, "3.2", TestName = "Complex_6")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->SpecimenShortDescription", null, null, "2.1", TestName = "Complex_7")]
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->PatientID", null, null, "1.2", TestName = "Complex_8")]

        //multiplicity tests
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->TextString&", null, null, "abcd\\efgh", TestName = "Complex_Multiplicity_1")]

        //conditional tests
        [TestCase("PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence->PatientID", "..->SpecimenShortDescription", "2.1", "3.2", TestName = "Complex_Conditional_1")]
        public void ComplexTagNestingTests(string pathway, string conditional, string conditionalMatch, object expectedResults)
        {
            if (!string.IsNullOrWhiteSpace((string)expectedResults))
                expectedResults = ((string)expectedResults).Replace("\r\n", Environment.NewLine);

            // Arrange
            var ds = new DicomDataset
            {
                //root->SpecimenShortDescription
                new DicomShortText(DicomTag.SpecimenShortDescription, "Root"),

                //root->PseudoColorPaletteInstanceReferenceSequence
                new DicomSequence(DicomTag.PseudoColorPaletteInstanceReferenceSequence,
                    new DicomDataset()
                    {
                        // Text multiplicity
                        //root->PseudoColorPaletteInstanceReferenceSequence->TextString
                        new DicomShortString(DicomTag.TextString, "abcd", "efgh"),
                        
                        //root->PseudoColorPaletteInstanceReferenceSequence->SpecimenShortDescription
                        new DicomShortText(DicomTag.SpecimenShortDescription, "1.1"),

                        //root->PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence
                        new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,
                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2.1"),

                                //root->PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence->PatientID
                                new DicomShortString(DicomTag.PatientID,"2.2")
                            },

                            //root->PseudoColorPaletteInstanceReferenceSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                            new DicomDataset
                            {
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2.1")
                            }),

                        //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence
                        new DicomSequence(DicomTag.AbstractPriorCodeSequence,
                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence
                                new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                {
                                    //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "3.1"),

                                    //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                    new DicomShortString(DicomTag.PatientID,"3.2")
                                }),

                                //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence
                                new DicomSequence(DicomTag.AbstractPriorCodeSequence,
                                    new DicomDataset()
                                    {

                                    }),

                                //root->PseudoColorPaletteInstanceReferenceSequence->AbstractPriorCodeSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2.1")
                            }),


                        //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence
                        new DicomSequence(DicomTag.PatientGantryRelationshipCodeSequence,
                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence
                                new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                {
                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "3.1"),

                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                    new DicomShortString(DicomTag.PatientID,"3.2")
                                }),
                                
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2.1")
                            }),

                            
                        //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence [array of 3 sequences]
                        new DicomSequence(DicomTag.PatientInsurancePlanCodeSequence,
                            new DicomDataset[]
                        {
                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence
                                new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                {
                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "3_0_.1"),

                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                    new DicomShortString(DicomTag.PatientID,"3_0_.2")
                                }),
                                
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2_0_.1")
                            },

                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence
                                new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                {
                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "3_1_.1"),

                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                    new DicomShortString(DicomTag.PatientID,"3_1_.2")
                                }),
                                
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientInsurancePlanCodeSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2_1_.1")
                            },


                            new DicomDataset
                            {
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence
                                new DicomSequence(DicomTag.ProbeDriveEquipmentSequence,new DicomDataset()
                                {
                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence->SpecimenShortDescription
                                    new DicomShortText(DicomTag.SpecimenShortDescription, "3_2_.1"),

                                    //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->ProbeDriveEquipmentSequence->PatientID
                                    new DicomShortString(DicomTag.PatientID,"3_2_.2")
                                }),
                                
                                //root->PseudoColorPaletteInstanceReferenceSequence->PatientGantryRelationshipCodeSequence->SpecimenShortDescription
                                new DicomShortText(DicomTag.SpecimenShortDescription, "2_2.1")
                            }
                        }),
                        
                        //root->PseudoColorPaletteInstanceReferenceSequence->PatientID
                        new DicomDataset { new DicomShortText(DicomTag.PatientID, "1.2")}
                    }),
            };

            var elevator = new TagElevator(pathway, conditional, conditionalMatch);

            ShowContentSequence(ds, DicomTag.PseudoColorPaletteInstanceReferenceSequence);

            object value = elevator.GetValue(ds);

            Assert.AreEqual(expectedResults, value);

        }

        private void ShowContentSequence(DicomDataset dataset, DicomTag tag = null)
        {
            tag = tag ?? DicomTag.ContentSequence;
            Console.WriteLine(tag.DictionaryEntry.Keyword + " Contains the following:");
            Console.WriteLine("-------------------------------------------------------------");

            var array = (Dictionary<DicomTag, object>[])DicomTypeTranslaterReader.GetCSharpValue(dataset, tag);
            var str = ArrayHelperMethods.AsciiArt(array);


            foreach (var keyword in DicomDictionary.Default)
                str = str.Replace(keyword.Tag.ToString(), keyword.Keyword);

            Console.WriteLine(str);
            Console.WriteLine();



            Console.WriteLine("--------------------End Content Sequence--------------------------");
        }
    }
}
