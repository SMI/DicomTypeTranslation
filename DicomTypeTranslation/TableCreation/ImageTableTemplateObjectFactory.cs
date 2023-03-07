using System;
using TypeGuesser;
using YamlDotNet.Serialization;

namespace DicomTypeTranslation.TableCreation
{
    internal class ImageTableTemplateObjectFactory : IObjectFactory
    {
        public object Create(Type type)
        {
            return type == typeof(DatabaseTypeRequest) ? new DatabaseTypeRequest(typeof(string)) : Activator.CreateInstance(type);
        }
    }
}