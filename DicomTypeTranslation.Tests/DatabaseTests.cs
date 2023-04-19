using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FAnsi;
using FAnsi.Discovery;
using FAnsi.Implementation;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.PostgreSql;
using NUnit.Framework;


namespace DicomTypeTranslation.Tests;

[SingleThreaded]
[NonParallelizable]
public class DatabaseTests
{
    private readonly Dictionary<DatabaseType,string> _testConnectionStrings = new();

    private bool _allowDatabaseCreation;
    private string _testScratchDatabase;

    private const string TestFilename = "TestDatabases.xml";

    [OneTimeSetUp]
    public void CheckFiles()
    {
        ImplementationManager.Load(
            typeof(MicrosoftSQLServerHelper).Assembly,
            typeof(OracleServerHelper).Assembly,
            typeof(MySqlServerHelper).Assembly,
            typeof(PostgreSqlServerHelper).Assembly
        );

        var file = Path.Combine(TestContext.CurrentContext.TestDirectory, TestFilename);
            
        Assert.IsTrue(File.Exists(file),"Could not find {0}", TestFilename);

        var doc = XDocument.Load(file);

        var root = doc.Element("TestDatabases") ?? throw new Exception($"Missing element 'TestDatabases' in {TestFilename}");

        var settings = root.Element("Settings") ?? throw new Exception($"Missing element 'Settings' in {TestFilename}");

        var e = settings.Element("AllowDatabaseCreation") ?? throw new Exception($"Missing element 'AllowDatabaseCreation' in {TestFilename}");

        _allowDatabaseCreation = Convert.ToBoolean(e.Value);

        e = settings.Element("TestScratchDatabase");
        if (e == null)
            throw new Exception($"Missing element 'TestScratchDatabase' in {TestFilename}");

        _testScratchDatabase = e.Value;
            
        foreach (var element in root.Elements("TestDatabase"))
        {
            var type = element.Element("DatabaseType")?.Value;

            if (!Enum.TryParse(type, out DatabaseType databaseType))
                throw new Exception($"Could not parse DatabaseType {type}");

            var constr = element.Element("ConnectionString")?.Value;
                
            _testConnectionStrings.Add(databaseType,constr);
        }
    }

    private DiscoveredServer GetTestServer(DatabaseType type)
    {
        if(!_testConnectionStrings.ContainsKey(type))
            Assert.Inconclusive("No connection string configured for that server");

        return new DiscoveredServer(_testConnectionStrings[type], type);
    }

    protected DiscoveredDatabase GetTestDatabase(DatabaseType type, bool cleanDatabase=true)
    {
        var server = GetTestServer(type);
        var db = server.ExpectDatabase(_testScratchDatabase);

        if(!db.Exists())
            if(_allowDatabaseCreation)
                db.Create();
            else
            {
                Assert.Inconclusive(
                    $"Database {_testScratchDatabase} did not exist on server {server} and AllowDatabaseCreation was false in {TestFilename}");
            }
        else
        {
            if (!cleanDatabase) return db;
            foreach (var t in db.DiscoverTables(true))
                t.Drop();

            foreach (var fn in db.DiscoverTableValuedFunctions())
                fn.Drop();
        }

        return db;
    }
}