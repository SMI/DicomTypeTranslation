using System.IO;
using System.Text;
using DicomTypeTranslation.TableCreation;
using NUnit.Framework;

namespace DicomTypeTranslation.Tests.Templates;

class TemplateDocumentationTests
{
    [Test]
    public void Test_TemplateDocumentation_Generate()
    {
        var files = Directory.EnumerateFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates"), "*.it");

        var sb = new StringBuilder();

        foreach (var file in files)
        {
            var collection = ImageTableTemplateCollection.LoadFrom(File.ReadAllText(file));


            sb.AppendLine($"## {Path.GetFileNameWithoutExtension(file)}");
            sb.AppendLine();

            foreach (var table in collection.Tables)
            {
                sb.AppendLine($"### {table.TableName}");
                sb.AppendLine();
                sb.AppendLine("| Field | Description |");
                sb.AppendLine("| ------------- | ------------- |");

                foreach (var col in table.Columns)
                {
                    sb.AppendLine($"| {col.ColumnName} |  |");
                }

                sb.AppendLine();
            }
        }

        TestContext.WriteLine("Suggested Documentation:");
        TestContext.Write(sb.ToString());
    }
}