using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DicomTypeTranslation.TableCreation;

internal class SystemTypeTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(Type).IsAssignableFrom(type);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var scalar = parser.Consume<Scalar>();
        return Type.GetType(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        var typeName = ((Type)value).FullName;
        emitter.Emit(new Scalar(typeName));
    }
}