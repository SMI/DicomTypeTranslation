using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FellowOakDicom;
using DicomTypeTranslation.Elevation.Exceptions;
using DicomTypeTranslation.Elevation.Serialization;

namespace DicomTypeTranslation.Elevation
{
    /// <summary>
    /// Services <see cref="TagElevationRequest"/>s by walking through <see cref="DicomDataset"/> sequences (<see cref="DicomSequence"/>) to identify matching
    /// tags and return leaf values.
    /// </summary>
    public class TagElevator
    {
        /// <summary>
        /// The symbol used to separate tags in an <see cref="TagElevationRequest.ElevationPathway"/>
        /// </summary>
        public const string Splitter = "->";

        private readonly TagNavigation[] _navigations;
        private readonly TagRelativeConditional _conditional;

        /// <summary>
        /// When ConcatenateMatches is on and multiple leaf nodes are detected (in different subsequences) then this string will be used to seperate them
        /// </summary>
        public string ConcatenateMatchesSplitter { get; set; }

        /// <summary>
        /// Allows multiple final tags to be picked up in different subsets (can happen where a Sequence in the path has multiple subsequences)
        /// </summary>
        public bool ConcatenateMatches { get; private set; }

        /// <summary>
        /// Specifies that 
        /// </summary>
        public bool ConcatenateMultiplicity { get; set; }

        /// <summary>
        /// When ConcatenateMatches is on and the leaf node(s) has multiplicity, the multiplicity array will be turned into a string seperated by this string
        /// </summary>
        public string ConcatenateMultiplicitySplitter { get; set; }


        private readonly bool _conditionalMatchesArrayElementsOfMultiplicity;
        private readonly string _conditionalMatchesArrayElementsOfMultiplicityPattern;

        /// <summary>
        /// Creates a new instance of the resolver ready to start evaluating matches to the given <paramref name="request"/>
        /// </summary>
        /// <param name="request"></param>
        public TagElevator(TagElevationRequest request):this(request.ElevationPathway,request.ConditionalPathway,request.ConditionalRegex)
        {
            
        }

        /// <summary>
        /// Creates a new instance of the resolver ready to start evaluating leaf matches to the given <paramref name="elevationPathway"/> in <see cref="DicomDataset"/>s with
        /// the provided (optional) <paramref name="conditional"/>
        /// </summary>
        public TagElevator(string elevationPathway, string conditional, string conditionalShouldMatch): this(elevationPathway)
        {
            
            if(conditional == null)
                if (conditionalShouldMatch == null)
                    return;
                else
                    throw new ArgumentNullException("conditionalShouldMatch");


            if (conditional.Contains("[]"))
            {
                if(!conditional.Equals("[]"))
                    throw new InvalidTagElevatorPathException(
                        $"Array operator conditional is only valid in isolation (i.e. '[]'), it cannot be part of a pathway (e.g. '{conditional}')");

                _conditionalMatchesArrayElementsOfMultiplicity = true;
                _conditionalMatchesArrayElementsOfMultiplicityPattern = conditionalShouldMatch;
            }
            else
                _conditional = new TagRelativeConditional(conditional,conditionalShouldMatch);
        }

        
        /// <summary>
        /// Creates a new instance of the resolver ready to start evaluating leaf matches to the given <paramref name="elevationPathway"/> in <see cref="DicomDataset"/>s
        /// </summary>
        /// <param name="elevationPathway"></param>
        public TagElevator(string elevationPathway)
        {
            var stop = false;

            while (!stop)
                elevationPathway = StripAndApplyOperators(elevationPathway, out stop);

            _navigations = GetPath(elevationPathway);

            ConcatenateMatchesSplitter = Environment.NewLine;
            ConcatenateMultiplicitySplitter = "\\";
        }

        private TagNavigation[] GetPath(string pathway)
        {
            var entries = pathway.Split(new[] { Splitter }, StringSplitOptions.RemoveEmptyEntries);

            var toReturn = new TagNavigation[entries.Length];

            if (toReturn.Length == 1)
                throw new InvalidTagElevatorPathException("There must be at least 2 entries in a navigation pathway");

            for (var i = 0; i < entries.Length; i++)
                toReturn[i] = new TagNavigation(entries[i], i + 1 == entries.Length);

            return toReturn;
        }

        /// <summary>
        /// Returns leaf elements in the <paramref name="dataset"/> which match the <see cref="TagElevationRequest.ConditionalPathway"/> this class was
        /// constructed with (including any conditionals).
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public object GetValue(DicomDataset dataset)
        {
            var finalObjects = new List<object>();

            //first pathway we turn it into a dictionary 

            foreach (var element in _navigations[0].GetSubsets(dataset))
                finalObjects.AddRange(GetValues(element, 1));
                    
            //none found
            if (!finalObjects.Any())
                return null;

            //found 1 only
            if (finalObjects.Count == 1)
                return finalObjects[0];

            //found multiple
            if (finalObjects.Count > 1)
                if (ConcatenateMatches)
                    return string.Join(ConcatenateMatchesSplitter, finalObjects);
                else
                    throw new TagNavigationException(
                        $"Found {finalObjects.Count} matches among tree branches (ConcatenateMatches mode is off - append a '+' to turn it on)");

            return finalObjects;
        }

        private IEnumerable<object> GetValues(SequenceElement element, int i)
        {
            var toReturn = new List<object>();

            if (_navigations[i].IsLast)
            {
                var o = _navigations[i].GetTags(element, _conditional);

                if (o is Array a)
                {
                    //if we have a conditional which should be applied to the array elements
                    if (_conditionalMatchesArrayElementsOfMultiplicity)
                        a = a.Cast<object>().Where(IsMatch).ToArray();

                    //this last branch matches nothing
                    if (a.Length == 0)
                        return new object[0];

                    if (a.Length == 1)
                        toReturn.Add(a.GetValue(0));
                    else
                    if (!ConcatenateMultiplicity)
                        throw new TagNavigationException(
                            $"Found {a.Length} multiplicity in leaf tag (ConcatenateMultiplicity is off - append a '&' to turn it on)");
                    else
                        toReturn.Add(string.Join(ConcatenateMultiplicitySplitter, a.Cast<object>().Select(s => s.ToString())));
                }
                else
                    if (IsMatch(o))
                    toReturn.Add(o);
            }
            else
                foreach (var subSequence in _navigations[i].GetSubset(element))
                    toReturn.AddRange(GetValues(subSequence, i + 1));

            return toReturn.ToArray();
        }

        private bool IsMatch(object element)
        {
            //if the element is null then don't include it as a matched element
            if (element == null)
                return false;

            //if there is no conditional on returned elements then it's definetly a match (See TagRelativeConditional for relative tag querying)
            if (_conditionalMatchesArrayElementsOfMultiplicityPattern == null)
                return true;

            //there is a conditonal on returned elememnts.  So is the array elementnot null and matches condition?
            return Regex.IsMatch(element.ToString(), _conditionalMatchesArrayElementsOfMultiplicityPattern);
        }


        private string StripAndApplyOperators(string navigationToken, out bool stop)
        {
            if (navigationToken.EndsWith("+"))
            {
                ConcatenateMatches = true;
                stop = false;
                return navigationToken.TrimEnd('+');
            }

            if (navigationToken.EndsWith("&"))
            {
                ConcatenateMultiplicity = true;
                stop = false;
                return navigationToken.TrimEnd('&');
            }


            stop = true;
            return navigationToken;
        }

        
    }
}
