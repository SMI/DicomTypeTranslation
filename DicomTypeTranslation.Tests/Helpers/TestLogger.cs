using NLog;
using NLog.Config;
using NLog.Targets;

namespace DicomTypeTranslation.Tests.Helpers;

public static class TestLogger
{
    private static LoggingConfiguration _logConfig;
    private static ConsoleTarget _consoleTarget;

    public static void Setup()
    {
        _logConfig = new LoggingConfiguration();

        _consoleTarget = new ConsoleTarget("TestConsole")
        {
            Layout = "${level} | ${message} | ${exception:format=toString,Data:maxInnerExceptionLevel=5}"
        };

        _logConfig.AddTarget(_consoleTarget);
        _logConfig.AddRuleForAllLevels(_consoleTarget);

        LogManager.GlobalThreshold = LogLevel.Trace;
        LogManager.Configuration = _logConfig;
        LogManager.GetCurrentClassLogger().Info("TestLogger setup, previous configuration replaced");
    }

    public static void ShutDown()
    {
        LogManager.Configuration = _logConfig = null;
        _consoleTarget.Dispose();
    }
}