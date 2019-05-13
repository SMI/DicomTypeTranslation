
using System.IO;

namespace DicomTypeTranslation.Helpers
{
    public static class MicroserviceHelpers
    {
        /// <summary>
        /// Check that paths will be handled correctly for the environment at runtime. When running on
        /// mono, Path.GetFileName(...) might not return what you would expect.
        /// </summary>
        /// <returns></returns>
        public static bool CorrectPlatformPathHandling()
        {
            return Path.GetFileName("test\\path\\to\\file.dcm") == "file.dcm";
        }
    }
}
