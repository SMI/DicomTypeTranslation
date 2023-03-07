using System;
using TypeGuesser;
using YamlDotNet.Serialization.ObjectFactories;

namespace DicomTypeTranslation.TableCreation;

internal class ImageTableTemplateObjectFactory : ObjectFactoryBase
{
    public override object Create(Type type)
    {
        return type == typeof(DatabaseTypeRequest) ? new DatabaseTypeRequest(typeof(string)) : Activator.CreateInstance(type);
    }
}