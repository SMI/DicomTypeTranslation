using FAnsi.Discovery;
using FAnsi.Discovery.TypeTranslation;
using System;
using TypeGuesser;
using YamlDotNet.Serialization;

namespace DicomTypeTranslation.TableCreation
{
    internal class ImageTableTemplateObjectFactory : IObjectFactory
    {
        public object Create(Type type)
        {
            if(type == typeof(DatabaseTypeRequest))
                return new DatabaseTypeRequest(typeof(string),null,null);

            return Activator.CreateInstance(type);
        }
    }
}