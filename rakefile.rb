load 'rakeconfig.rb'
$MSBUILD15CMD = MSBUILD15CMD.gsub(/\\/,"/")

task :continuous, [:config] => [:setup_connection,:assemblyinfo, :build, :tests]

task :release, [:config] => [:setup_connection,:assemblyinfo, :deploy, :pack]

task :restorepackages do
    sh "nuget restore #{SOLUTION}"
end

task :setup_connection do 
    File.open("DicomTypeTranslation.Tests/TestDatabases.xml", "w") do |f|
        f.write("<TestDatabases>
          <Settings>
            <AllowDatabaseCreation>True</AllowDatabaseCreation>
            <TestScratchDatabase>FAnsiTests</TestScratchDatabase>
          </Settings>
          <TestDatabase>
            <DatabaseType>MicrosoftSQLServer</DatabaseType>
            <ConnectionString>server=#{DBSERVER};Trusted_Connection=True;</ConnectionString>
          </TestDatabase>
          <TestDatabase>
            <DatabaseType>MySql</DatabaseType>
            <ConnectionString>Server=#{MYSQLDB};Uid=#{MYSQLUSR};Pwd=#{MYSQLPASS};Ssl-Mode=Required</ConnectionString>
          </TestDatabase>
          <!--<TestDatabase>
            <DatabaseType>Oracle</DatabaseType>
            <ConnectionString>Data Source=localhost:1521/orclpdb.dundee.uni;User Id=ora;Password=zombie;</ConnectionString>
          </TestDatabase>-->
        </TestDatabases>")
    end
end

task :build, [:config] => :restorepackages do |msb, args|
	sh "\"#{$MSBUILD15CMD}\" #{SOLUTION} \/t:Clean;Build \/p:Configuration=#{args.config}"
end

task :tests do 
	sh 'dotnet test --logger:"nunit;LogFilePath=test-result.xml"'
end

task :deploy, [:config] => :restorepackages do |msb, args|
	args.with_defaults(:config => :Release)
	sh "\"#{$MSBUILD15CMD}\" #{SOLUTION} \/t:Clean;Build \/p:Configuration=#{args.config}"
end

desc "Sets the version number from SharedAssemblyInfo file"    
task :assemblyinfo do 
	asminfoversion = File.read("SharedAssemblyInfo.cs").match(/AssemblyInformationalVersion\("(\d+)\.(\d+)\.(\d+)(-.*)?"/)
    
	puts asminfoversion.inspect
	
    major = asminfoversion[1]
	minor = asminfoversion[2]
	patch = asminfoversion[3]
    suffix = asminfoversion[4]
	
	version = "#{major}.#{minor}.#{patch}"
    puts "version: #{version}#{suffix}"
    
	# DO NOT REMOVE! needed by build script!
    f = File.new('version', 'w')
    f.write "#{version}#{suffix}"
    f.close
    # ----
end

desc "Pushes the plugin packages into the specified folder"    
task :pack, [:config] do |t, args|
	args.with_defaults(:config => :Release)
	
		version = File.open('version') {|f| f.readline}
		puts "version: #{version}"
		
    Dir.chdir('DicomTypeTranslation') do
        sh "nuget pack HIC.DicomTypeTranslation.nuspec -Properties Configuration=#{args.config} -IncludeReferencedProjects -Symbols -Version #{version}"
        sh "nuget push HIC.DicomTypeTranslation.#{version}.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey #{NUGETKEY}"
    end
end
