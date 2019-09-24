using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DicomTypeTranslation.TableCreation;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DicomTypeTranslation.Tests.Templates
{
    class TemplateDocumentationTests
    {
        private Regex _modalityRegex = new Regex("^## ([A-Z]+)");
        private Regex _tableNameRegex = new Regex("^### (.*)");
        private Regex _tableHeaderRegex = new Regex(@"\|\s*Field\s*\|\s*Description\s*\|");
        private Regex _tableRowRegex = new Regex(@"\|([^|]*)\|");
        
        [Test]
        public void Test_TemplateDocumentation_MatchesTemplates()
        {
            var content = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory,"Templates","README.md"));

            StringAssert.StartsWith("# Templates", content[0]);
            
            //The template file we have found that matches a header e.g. "## MR" => "MR.it"
            ImageTableTemplateCollection currentFile = null;
            string currentFileName = null;
            ImageTableTemplate currentTemplate = null;

            foreach (string s in content)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                var matchModality = _modalityRegex.Match(s);

                if (matchModality.Success)
                {
                    AssertNoUndocumentedTables(currentFile,currentFileName);
                    currentFile = GetTemplateFile(matchModality.Groups[1].Value.Trim(), out currentFileName);
                }

                var matchTable = _tableNameRegex.Match(s);

                if (matchTable.Success)
                {
                    AssertNoUndocumentedColumns(currentTemplate,currentFileName);
                    Assert.IsNotNull(currentFile,"Found Table Header before a Modality e.g. ### StudyTable before ## MR");
                    currentTemplate = Pop(matchTable.Groups[1].Value.Trim(),currentFile,currentFileName);
                }
                
                if(_tableHeaderRegex.IsMatch(s))
                    continue;

                var matchRow = _tableRowRegex.Match(s);

                if (matchRow.Success)
                {
                    //It's a header row-------------
                    if (matchRow.Groups[1].Value.Trim().All(c => c == '-' || c == '|'))
                        continue;

                    Assert.IsNotNull(currentTemplate,"Found Table Row before a Modality e.g. \"|CHI|A private id|\" before ## MR");
                    Pop(matchRow.Groups[1].Value.Trim(), currentTemplate, currentFileName);
                }
            }

            AssertNoUndocumentedTables(currentFile,currentFileName);
        }

        [Test]
        public void Test_TemplateDocumentation_Generate()
        {
            var files = Directory.EnumerateFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates"), "*.it");

            StringBuilder sb = new StringBuilder();

            foreach (string file in files)
            {
                var collection = ImageTableTemplateCollection.LoadFrom(File.ReadAllText(file));


                sb.AppendLine("## " + Path.GetFileNameWithoutExtension(file));
                sb.AppendLine();

                foreach (ImageTableTemplate table in collection.Tables)
                {
                    sb.AppendLine("### " + table.TableName);
                    sb.AppendLine();
                    sb.AppendLine("| Field | Description |");
                    sb.AppendLine("| ------------- | ------------- |");

                    foreach (ImageColumnTemplate col in table.Columns)
                    {
                        sb.AppendLine("| " + col.ColumnName + " |  |");
                    }

                    sb.AppendLine();
                }
            }

            TestContext.WriteLine("Suggested Documentation:");
            TestContext.Write(sb.ToString());
        }
        

        private ImageColumnTemplate Pop(string columnName, ImageTableTemplate currentTemplate, string currentFileName)
        {
            var col = currentTemplate.Columns.SingleOrDefault(c => c.ColumnName.Equals(columnName));

            Assert.IsNotNull(col,$"Column is described in documentation called {columnName} but did not appear in the current Modality file '{currentFileName}' under expected table '{currentTemplate.TableName}' (or it is documented twice)");

            //currentTemplate.Columns.Remove(col) for arrays!
            currentTemplate.Columns = currentTemplate.Columns.Where(c=>c!=col).ToArray();

            return col;
        }

        private ImageTableTemplate Pop(string tableName, ImageTableTemplateCollection currentFile, string currentFileName)
        {
            var name = currentFile.Tables.SingleOrDefault(t => t.TableName.Equals(tableName));

            Assert.IsNotNull(name,$"Table is described in documentation called {tableName} but did not appear in the current Modality file '{currentFileName}' (or it is documented twice)");

            currentFile.Tables.Remove(name);

            return name;
        }


        private void AssertNoUndocumentedTables(ImageTableTemplateCollection currentFile, string currentFileName)
        {
            if(currentFile != null)
                Assert.IsEmpty(currentFile.Tables,$"There were undocumented tables ({string.Join(",",currentFile.Tables)}) that appear in Modality file '{currentFileName}'");
        }

        private void AssertNoUndocumentedColumns(ImageTableTemplate currentTemplate, string currentFileName)
        {
            if(currentTemplate != null)
                Assert.IsEmpty(currentTemplate.Columns,$"There were undocumented columns ({string.Join(",",currentTemplate.Columns.Select(c=>c.ColumnName))}) that appear in Modality file '{currentFileName}' under table '{currentTemplate.TableName}''");
        }

        private ImageTableTemplateCollection GetTemplateFile(string modality, out string currentFileName)
        {
            currentFileName = Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates", modality + ".it");
            string yaml = File.ReadAllText(currentFileName);
            return ImageTableTemplateCollection.LoadFrom(yaml);

        }
    }
}
