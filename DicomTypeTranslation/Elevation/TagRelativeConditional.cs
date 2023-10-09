using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FellowOakDicom;
using DicomTypeTranslation.Elevation.Exceptions;

namespace DicomTypeTranslation.Elevation
{
    class TagRelativeConditional
    {
        public bool IsCurrentNodeMatch { get; private set; }
        private readonly string _conditionalShouldMatch;
        
        private List<TagNavigation> _navigations;

        private string[] validStartersTokens = new[] {".", "[..]", ".."};

        //.. and [..] only
        private List<string> _relativeOperators = new List<string>();

        public TagRelativeConditional(string conditional, string conditionalShouldMatch)
        {
            _conditionalShouldMatch = conditionalShouldMatch;

            if (string.IsNullOrWhiteSpace(conditional))
                throw new InvalidTagElevatorPathException("TagRelativeConditional pathways cannot be blank, use '.' for current node");

            if (conditional.Equals("."))
            {
                IsCurrentNodeMatch = true;
                return;
            }
            
            var path = conditional.Split(new []{TagElevator.Splitter}, StringSplitOptions.RemoveEmptyEntries);

            if (!validStartersTokens.Contains(path[0]))
                throw new InvalidTagElevatorPathException(
                    $"Invalid starter token in TagRelativeConditional '{conditional}'.  Valid starter tokens are '{string.Join("','", validStartersTokens)}'");

            if(path.Length == 1)
                throw new InvalidTagElevatorPathException(
                    $"TagRelativeConditional must have a valid terminating non Sequence Tag e.g. '..->ConceptNameCodeSequence->CodeMeaning'.  Your path was '{conditional}'");

            _navigations = new List<TagNavigation>();
            
            //Process the . and .. elements
            for (var i=0; i < path.Length; i++)
            {
                //if it is a relative positional 

                //if we are going up a level
                if (path[i] == ".." || path[i] == "[..]")
                {
                    if (_navigations.Any())
                        throw new InvalidTagElevatorPathException(
                            $"TagRelativeConditional pathways cannot have '{path[i]}' after the first dicom tag");

                    //go up
                    _relativeOperators.Add(path[i]);
                }
                else if (path[i] == ".")
                {
                    //only valid at the start of the path
                    if (i != 0)
                        throw new InvalidTagElevatorPathException(
                            $"'{path[i]}' is only valid at the start of a TagRelativeConditional");
                }
                else
                    _navigations.Add(new TagNavigation(path[i], i + 1 == path.Length)); //navigational (no more positionals please!)
            }

            if (!_navigations.Any())
                throw new InvalidTagElevatorPathException("TagRelativeConditional pathways must terminate in a DicomTag or be '.' (current tag)");
        }

        

        public bool IsMatch(SequenceElement element, DicomTag currentTag)
        {
            //match is '.' so only care if the current value matches
            if (IsCurrentNodeMatch)
                return IsMatch(element.Dataset[currentTag]);

            //match all elements in the current array of the sequence e.g. [2]
            var toMatchIn = new List<SequenceElement> {element};

            foreach (var relativeOperator in _relativeOperators)
            {
                //[..] - match array siblings
                if(relativeOperator == "[..]")
                    toMatchIn = toMatchIn.SelectMany(s=>s.ArraySiblings).Distinct().ToList();

                //.. - match containing parent of the current Sequence (where not null)
                if (relativeOperator == "..")
                    toMatchIn = toMatchIn.Select(s => s.Parent).Where(p => p != null).Distinct().ToList();

                //distinct it
                toMatchIn = toMatchIn.Distinct().ToList();
            }

            var finalObjects = new List<object>();

            foreach (var navigation in _navigations)
            {
                var newSets = new List<SequenceElement>();

                if (navigation.IsLast)
                    foreach (var sequenceElement in toMatchIn)
                        finalObjects.Add(navigation.GetTags(sequenceElement, null));
                else
                    foreach (var sequenceElement in toMatchIn)
                        newSets.AddRange(navigation.GetSubset(sequenceElement));

                toMatchIn = newSets;
            }
            
            return finalObjects.Any(IsMatch);
        }

        private bool IsMatch(object value)
        {
            if (value == null)
                return false;

            //if it is an array (multiplicity) then conditional matches any sub element
            var a = value as Array;
            if (a != null)
                if (a.Length == 0)
                    return false;
                else
                if (a.Length == 1)
                    value = a.GetValue(0);
                else
                {
                    //return ((Array) value).Cast<object>().Any(o => o != null && Regex.IsMatch(o.ToString(), _conditionalShouldMatch));

                    throw new TagNavigationException($"Conditional matched a leaf node with Multiplicity of {a.Length}");
                }
                

            //its not multiplicity
            return Regex.IsMatch(value.ToString(),_conditionalShouldMatch);
        }
    }
}