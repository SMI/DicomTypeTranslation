﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
    protected Dictionary<DatabaseType,string> TestConnectionStrings = new Dictionary<DatabaseType, string>();

    protected bool AllowDatabaseCreation;
    private string _testScratchDatabase;

    protected const string TestFilename = "TestDatabases.xml";

    [OneTimeSetUp]
    public void CheckFiles()
    {
        ImplementationManager.Load<MicrosoftSQLImplementation>();
        ImplementationManager.Load<MySqlImplementation>();
        ImplementationManager.Load<PostgreSqlImplementation>();
        ImplementationManager.Load<OracleImplementation>();

        var file = Path.Combine(TestContext.CurrentContext.TestDirectory, TestFilename);

        Assert.That(File.Exists(file), Is.True, "Could not find " + TestFilename);

        var doc = XDocument.Load(file);

        var root = doc.Element("TestDatabases") ?? throw new Exception($"Missing element 'TestDatabases' in {TestFilename}");
        var settings = root.Element("Settings") ?? throw new Exception($"Missing element 'Settings' in {TestFilename}");
        var e = settings.Element("AllowDatabaseCreation") ?? throw new Exception($"Missing element 'AllowDatabaseCreation' in {TestFilename}");
        AllowDatabaseCreation = Convert.ToBoolean(e.Value);

        e = settings.Element("TestScratchDatabase");
        if (e == null)
            throw new Exception($"Missing element 'TestScratchDatabase' in {TestFilename}");

        _testScratchDatabase = e.Value;

        foreach (var element in root.Elements("TestDatabase"))
        {
            var type = element.Element("DatabaseType").Value;

            if (!Enum.TryParse(type, out DatabaseType databaseType))
                throw new Exception($"Could not parse DatabaseType {type}");

            var constr = element.Element("ConnectionString").Value;

            TestConnectionStrings.Add(databaseType,constr);
        }
    }

    protected IEnumerable<DiscoveredServer> TestServer()
    {
        return TestConnectionStrings.Select(kvp => new DiscoveredServer(kvp.Value, kvp.Key));
    }
    protected DiscoveredServer GetTestServer(DatabaseType type)
    {
        if(!TestConnectionStrings.ContainsKey(type))
            Assert.Inconclusive("No connection string configured for that server");

        return new DiscoveredServer(TestConnectionStrings[type], type);
    }

    protected DiscoveredDatabase GetTestDatabase(DatabaseType type, bool cleanDatabase=true)
    {
        var server = GetTestServer(type);
        var db = server.ExpectDatabase(_testScratchDatabase);

        if(!db.Exists())
            if(AllowDatabaseCreation)
                db.Create();
            else
            {
                Assert.Inconclusive(
                    $"Database {_testScratchDatabase} did not exist on server {server} and AllowDatabaseCreation was false in {TestFilename}");
            }
        else
        {
            if (cleanDatabase)
            {
                foreach (var t in db.DiscoverTables(true))
                    t.Drop();

                foreach (var func in db.DiscoverTableValuedFunctions())
                    func.Drop();
            }
        }

        return db;
    }

    protected void AssertCanCreateDatabases()
    {
        if(!AllowDatabaseCreation)
            Assert.Inconclusive("Test cannot run when AllowDatabaseCreation is false");
    }

    protected bool AreBasicallyEquals(object o, object o2, bool handleSlashRSlashN = true)
    {
        //if they are legit equals
        if (Equals(o, o2))
            return true;

        //if they are null but basically the same
        var oIsNull = o == null || o == DBNull.Value || o.ToString().Equals("0");
        var o2IsNull = o2 == null || o2 == DBNull.Value || o2.ToString().Equals("0");

        if (oIsNull || o2IsNull)
            return oIsNull == o2IsNull;

        //they are not null so tostring them deals with int vs long etc that DbDataAdapters can be a bit flaky on
        if (handleSlashRSlashN)
            return string.Equals(o.ToString().Replace("\r", "").Replace("\n", ""), o2.ToString().Replace("\r", "").Replace("\n", ""));

        return string.Equals(o.ToString(), o2.ToString());
    }

    protected void AssertAreEqual(DataTable dt1, DataTable dt2)
    {
        Assert.Multiple(() =>
        {
            Assert.That(dt2.Columns, Has.Count.EqualTo(dt1.Columns.Count), "DataTables had a column count mismatch");
            Assert.That(dt2.Rows, Has.Count.EqualTo(dt1.Rows.Count), "DataTables had a row count mismatch");
        });

        foreach (DataRow row1 in dt1.Rows)
        {
            var match = false;

            foreach (DataRow row2 in dt2.Rows)
            {
                var rowMatch = true;
                foreach (DataColumn column in dt1.Columns)
                {
                    if (!AreBasicallyEquals(row1[column.ColumnName], row2[column.ColumnName]))
                        rowMatch = false;
                }

                if (rowMatch)
                    match = true;
            }

            Assert.That(match, Is.True, "Couldn't find match for row:" + string.Join(",", row1.ItemArray));

        }

    }
}