
using Dicom;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Text;

namespace DicomTypeTranslation.Helpers
{
    /// <summary>
    /// Helper methods for evaluating Equality between <see cref="DicomDataset"/> instances
    /// </summary>
    public static class DicomDatasetHelpers
    {
        /// <summary>
        /// Checks  the correct fo-dicom library is present for the platform at runtime
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        public static bool CorrectFoDicomVersion()
        {
            try
            {
                Encoding _ = Dicom.IO.IOManager.BaseEncoding;
                return true;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes group length elements from the DICOM dataset. These have been retired in the DICOM standard.
        /// </summary>
        /// <remarks><see href="http://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.2"/></remarks>
        /// <param name="dataset">DICOM dataset</param>
        public static void RemoveGroupLengths(this DicomDataset dataset)
        {
            if (dataset == null)
                return;

            dataset.Remove(x => x.Tag.Element == 0x0000);

            // Handle sequences
            foreach (DicomSequence sq in dataset.Where(x => x.ValueRepresentation == DicomVR.SQ).Cast<DicomSequence>())
                foreach (DicomDataset item in sq.Items)
                    item.RemoveGroupLengths();
        }
    }
}
