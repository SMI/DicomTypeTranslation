using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FellowOakDicom;
using DicomTypeTranslation.Elevation.Exceptions;

namespace DicomTypeTranslation.Elevation;

internal class TagRelativeConditional
{
    public bool IsCurrentNodeMatch { get; }
    private readonly string _conditionalShouldMatch;
        
    private readonly List<TagNavigation> _navigations;

    private static readonly string[] ValidStartersTokens = {".", "[..]", ".."};

    //.. and [..] only
    private readonly List<string> _relativeOperators = new();

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

        if (!ValidStartersTokens.Contains(path[0]))
            throw new InvalidTagElevatorPathException(
                $"Invalid starter token in TagRelativeConditional '{conditional}'.  Valid starter tokens are '{string.Join("','", ValidStartersTokens)}'");

        if(path.Length == 1)
            throw new InvalidTagElevatorPathException(
                $"TagRelativeConditional must have a valid terminating non Sequence Tag e.g. '..->ConceptNameCodeSequence->CodeMeaning'.  Your path was '{conditional}'");

        _navigations = new List<TagNavigation>();
            
        //Process the . and .. elements
        for (var i=0; i < path.Length; i++)
        {
            //if it is a relative positional 

            switch (path[i])
            {
                //if we are going up a level
                case "..":
                case "[..]":
                {
                    if (_navigations.Any())
                        throw new InvalidTagElevatorPathException(
                            $"TagRelativeConditional pathways cannot have '{path[i]}' after the first dicom tag");

                    //go up
                    _relativeOperators.Add(path[i]);
                    break;
                }
                case ".":
                {
                    //only valid at the start of the path
                    if (i != 0)
                        throw new InvalidTagElevatorPathException(
                            $"'{path[i]}' is only valid at the start of a TagRelativeConditional");
                    break;
                }
                default:
                    _navigations.Add(new TagNavigation(path[i], i + 1 == path.Length)); //navigational (no more positionals please!)
                    break;
            }
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
            toMatchIn = relativeOperator switch
            {
                //[..] - match array siblings
                "[..]" => toMatchIn.SelectMany(s => s.ArraySiblings).Distinct().ToList(),
                //.. - match containing parent of the current Sequence (where not null)
                ".." => toMatchIn.Select(s => s.Parent).Where(p => p != null).Distinct().ToList(),
                _ => toMatchIn
            };

            //distinct it
            toMatchIn = toMatchIn.Distinct().ToList();
        }

        var finalObjects = new List<object>();

        foreach (var navigation in _navigations)
        {
            var newSets = new List<SequenceElement>();

            if (navigation.IsLast)
                finalObjects.AddRange(toMatchIn.Select(sequenceElement => navigation.GetTags(sequenceElement, null)));
            else
                foreach (var sequenceElement in toMatchIn)
                    newSets.AddRange(navigation.GetSubset(sequenceElement));

            toMatchIn = newSets;
        }
            
        return finalObjects.Any(IsMatch);
    }

    private bool IsMatch(object value)
    {
        switch (value)
        {
            case null:
            //if it is an array (multiplicity) then conditional matches any sub element
            case Array { Length: 0 }:
                return false;
            case Array { Length: 1 } a:
                value = a.GetValue(0);
                break;
            case Array a:
                //return ((Array) value).Cast<object>().Any(o => o != null && Regex.IsMatch(o.ToString(), _conditionalShouldMatch));

                throw new TagNavigationException($"Conditional matched a leaf node with Multiplicity of {a.Length}");
        }


        //it's not multiplicity
        return Regex.IsMatch(value?.ToString() ?? throw new InvalidOperationException(),_conditionalShouldMatch);
    }
}